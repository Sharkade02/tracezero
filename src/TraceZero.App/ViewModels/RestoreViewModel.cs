using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;
using TraceZero.Application.Protection;
using TraceZero.Domain;
using TraceZero.Domain.Protection;

namespace TraceZero.App.ViewModels;

/// <summary>Ligne d'un élément restaurable.</summary>
public sealed class RestoreRowViewModel
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    public RestoreRowViewModel(RestoreRecord record)
    {
        Record = record;
        DateText = record.TimestampUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Fr);
        Description = record.Description;
        Source = record.Source;
        ReversibilityText = record.Reversibility switch
        {
            Reversibility.Reversible => Localizer.Get("Rev.Reversible"),
            Reversibility.PartiallyReversible => Localizer.Get("Rev.Partial"),
            _ => Localizer.Get("Rev.Irreversible"),
        };
    }

    public RestoreRecord Record { get; }

    public long Id => Record.Id;

    public string DateText { get; }

    public string Description { get; }

    public string Source { get; }

    public string ReversibilityText { get; }

    /// <summary>Seuls les éléments réversibles peuvent être restaurés.</summary>
    public bool CanRestore => Record.Reversibility == Reversibility.Reversible;
}

/// <summary>
/// Page « Restaurer les éléments disponibles » (Phase 7, §17). Liste les sauvegardes créées avant les
/// nettoyages réversibles et permet de les restaurer. N'affiche jamais comme restaurable ce qui ne l'est
/// pas ; ne prétend jamais restaurer un fichier effacé de façon sécurisée.
/// </summary>
public sealed partial class RestoreViewModel : PageViewModelBase
{
    private readonly IProtectionVault _vault;
    private readonly IRegistryBackupService _registryBackup;
    private readonly IDialogService _dialog;
    private readonly IToastService _toasts;

    public RestoreViewModel(
        IProtectionVault vault,
        IRegistryBackupService registryBackup,
        IDialogService dialog,
        IToastService toasts)
    {
        _vault = vault;
        _registryBackup = registryBackup;
        _dialog = dialog;
        _toasts = toasts;
    }

    public override string Title => TraceZero.App.Services.Localizer.Get("Nav.Restore");

    public override string IconGlyph => "\U0001F6E1"; // 🛡

    public override bool IsUnderConstruction => false;

    public ObservableCollection<RestoreRowViewModel> Items { get; } = [];

    [ObservableProperty]
    private bool _hasItems;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    partial void OnStatusMessageChanged(string? value) => OnPropertyChanged(nameof(HasStatus));

    public override void OnActivated() => _ = RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var records = await _vault.GetRestorableAsync(100);
        Items.Clear();
        foreach (var record in records)
        {
            Items.Add(new RestoreRowViewModel(record));
        }

        HasItems = Items.Count > 0;
    }

    private bool CanRestore(RestoreRowViewModel? row) => !IsBusy && row is { CanRestore: true };

    [RelayCommand(CanExecute = nameof(CanRestore))]
    private async Task RestoreAsync(RestoreRowViewModel? row)
    {
        if (row is null || row.Record.Kind != RestoreItemKind.RegistryBackup)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var record = row.Record;
            var restored = await Task.Run(() =>
            {
                var snapshot = RegistrySnapshotCodec.Deserialize(record.Payload);
                return _registryBackup.Restore(record.Target, snapshot);
            });

            await _vault.MarkRestoredAsync(record.Id);
            StatusMessage = Localizer.Format("Restore.Msg.Restored", record.Description, restored);
            _toasts.Show(Localizer.Format("Restore.Toast.Restored", record.Description), ToastKind.Success);
            await RefreshAsync();
        }
        catch (Exception)
        {
            StatusMessage = Localizer.Format("Restore.Msg.Failed", row.Description);
            _toasts.Show(Localizer.Format("Restore.Toast.Failed", row.Description), ToastKind.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        if (!HasItems)
        {
            return;
        }

        var confirmed = await _dialog.ConfirmAsync(
            Localizer.Get("Restore.Confirm.Title"),
            Localizer.Get("Restore.Confirm.Body"),
            confirmText: Localizer.Get("Restore.ClearVault"),
            cancelText: Localizer.Get("Common.Cancel"),
            destructive: true);

        if (!confirmed)
        {
            return;
        }

        await _vault.ClearAsync();
        StatusMessage = Localizer.Get("Restore.Msg.Cleared");
        _toasts.Show(Localizer.Get("Restore.Msg.Cleared"), ToastKind.Info);
        await RefreshAsync();
    }
}

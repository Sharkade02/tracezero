using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TraceZero.App.Services;
using TraceZero.Application.Elevation;
using TraceZero.Application.Exclusions;
using TraceZero.Domain.Common;
using TraceZero.Domain.Elevation;
using TraceZero.Domain.Exclusions;

namespace TraceZero.App.ViewModels;

/// <summary>Ligne d'exclusion affichée.</summary>
public sealed class ExclusionRowViewModel
{
    public ExclusionRowViewModel(ExclusionRule rule)
    {
        Id = rule.Id;
        DisplayName = rule.DisplayName;
        KindText = rule.Kind == ExclusionKind.Folder ? "Dossier" : "Catégorie";
    }

    public Guid Id { get; }

    public string DisplayName { get; }

    public string KindText { get; }
}

/// <summary>Option de langue (endonyme affiché tel quel dans toutes les langues).</summary>
public sealed record LanguageOption(AppLanguage Language, string Display);

/// <summary>Page Paramètres (§40) : thème, langue (§31) et gestion des exclusions.</summary>
public sealed partial class SettingsViewModel : PageViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localization;
    private readonly IExclusionStore _exclusionStore;
    private readonly IElevatedOperationService _elevatedService;

    public SettingsViewModel(
        IThemeService themeService,
        ILocalizationService localization,
        IExclusionStore exclusionStore,
        IElevatedOperationService elevatedService)
    {
        _themeService = themeService;
        _localization = localization;
        _exclusionStore = exclusionStore;
        _elevatedService = elevatedService;
        _themeService.ThemeChanged += (_, _) => OnPropertyChanged(nameof(IsDarkTheme));
        _localization.LanguageChanged += (_, _) => OnPropertyChanged(nameof(SelectedLanguage));
        ReloadExclusions();
    }

    /// <summary>Langues proposées, par endonyme (jamais traduit — convention des sélecteurs de langue).</summary>
    public IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new(AppLanguage.French, "Français"),
        new(AppLanguage.English, "English"),
        new(AppLanguage.German, "Deutsch"),
        new(AppLanguage.Spanish, "Español"),
    ];

    public LanguageOption SelectedLanguage
    {
        get => Languages.First(l => l.Language == _localization.Current);
        set
        {
            if (value is not null && value.Language != _localization.Current)
            {
                _localization.Apply(value.Language);
                OnPropertyChanged();
            }
        }
    }

    public override string Title => TraceZero.App.Services.Localizer.Get("Nav.Settings");

    public override string IconGlyph => "\U0001F527";

    public override bool IsUnderConstruction => false;

    public bool IsDarkTheme
    {
        get => _themeService.Current == AppTheme.Dark;
        set
        {
            _themeService.Apply(value ? AppTheme.Dark : AppTheme.Light);
            OnPropertyChanged();
        }
    }

    public ObservableCollection<ExclusionRowViewModel> Exclusions { get; } = [];

    [ObservableProperty]
    private bool _hasExclusions;

    [RelayCommand]
    private void AddFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choisir un dossier à exclure du nettoyage",
        };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            _exclusionStore.Add(new ExclusionRule
            {
                Id = Guid.NewGuid(),
                Kind = ExclusionKind.Folder,
                Value = dialog.FolderName,
                DisplayName = dialog.FolderName,
                CreatedUtc = DateTimeOffset.UtcNow,
            });
            ReloadExclusions();
        }
    }

    [RelayCommand]
    private void Remove(ExclusionRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        _exclusionStore.Remove(row.Id);
        ReloadExclusions();
    }

    private void ReloadExclusions()
    {
        Exclusions.Clear();
        foreach (var rule in _exclusionStore.GetAll())
        {
            Exclusions.Add(new ExclusionRowViewModel(rule));
        }

        HasExclusions = Exclusions.Count > 0;
    }

    // ── Nettoyage avancé nécessitant l'élévation (Phase 20, §30) ──────────────────────────────

    /// <summary>Message d'état du dernier nettoyage élevé (vide au repos).</summary>
    [ObservableProperty]
    private string _elevatedStatus = string.Empty;

    [ObservableProperty]
    private bool _isElevatedBusy;

    /// <summary>
    /// Nettoie <c>C:\Windows\Temp</c> via le helper élevé. L'app n'est jamais admin : l'appel déclenche
    /// l'invite UAC, puis le helper valide, agit et s'arrête. Un refus UAC est signalé sans planter.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRunElevated))]
    private async Task CleanWindowsTempAsync()
    {
        IsElevatedBusy = true;
        ElevatedStatus = "Élévation en cours (autorisez l'invite Windows)…";
        CleanWindowsTempCommand.NotifyCanExecuteChanged();

        try
        {
            var result = await _elevatedService.RunAsync(new ElevatedRequest
            {
                Operation = ElevatedOperation.CleanWindowsTemp,
            });

            ElevatedStatus = result.Success
                ? $"Nettoyé : {ByteSize.Format(result.BytesFreed)} libérés " +
                  $"({result.ActionsSucceeded} fichiers" +
                  (result.ActionsFailed > 0 ? $", {result.ActionsFailed} ignorés/verrouillés" : string.Empty) + ")."
                : result.ErrorMessage ?? "Échec du nettoyage élevé.";
        }
        finally
        {
            IsElevatedBusy = false;
            CleanWindowsTempCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanRunElevated() => !IsElevatedBusy;
}

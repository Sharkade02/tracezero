using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;
using TraceZero.Application.Apps;
using TraceZero.Domain.Apps;
using TraceZero.Domain.Common;

namespace TraceZero.App.ViewModels;

public sealed class AppRowViewModel(AppInstallation app)
{
    public AppInstallation Model => app;
    public string Name => app.Name;
    public string Publisher => app.Publisher ?? Localizer.Get("Apps.UnknownPublisher");
    public string Version => app.Version is { } v ? $"v{v}" : string.Empty;
    public string SizeText => app.SizeBytes is { } s ? ByteSize.Format(s) : string.Empty;
    public string InstallDateText => app.InstallDate?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("fr-FR")) ?? string.Empty;
    public bool CanUninstall => !string.IsNullOrWhiteSpace(app.UninstallCommand);
    public bool CanOpen => !string.IsNullOrWhiteSpace(app.InstallLocation) && Directory.Exists(app.InstallLocation);
}

public sealed partial class StartupRowViewModel(StartupEntry entry) : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled = entry.IsEnabled;

    public StartupEntry Model => entry;
    public string Name => entry.Name;
    public string Command => entry.Command;
    public bool CanToggle => entry.CanToggle;

    public string LocationText => entry.Location switch
    {
        StartupLocation.RunCurrentUser => "Utilisateur",
        StartupLocation.RunLocalMachine => "Système (lecture seule)",
        StartupLocation.StartupFolder => "Dossier de démarrage",
        _ => string.Empty,
    };
}

/// <summary>Page Applications & Démarrage (§22).</summary>
public sealed partial class ApplicationsViewModel : PageViewModelBase
{
    private readonly IInstalledAppService _appService;
    private readonly IStartupService _startupService;
    private readonly IDialogService _dialog;
    private readonly IToastService _toasts;
    private readonly List<AppRowViewModel> _allApps = [];
    private bool _loaded;

    public ApplicationsViewModel(
        IInstalledAppService appService,
        IStartupService startupService,
        IDialogService dialog,
        IToastService toasts)
    {
        _appService = appService;
        _startupService = startupService;
        _dialog = dialog;
        _toasts = toasts;
    }

    public override string Title => TraceZero.App.Services.Localizer.Get("Nav.Applications");
    public override string IconGlyph => "\U0001F4E6";
    public override bool IsUnderConstruction => false;

    public ObservableCollection<AppRowViewModel> Apps { get; } = [];
    public ObservableCollection<StartupRowViewModel> StartupEntries { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _appCountText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public override void OnActivated()
    {
        if (!_loaded)
        {
            _ = LoadAsync();
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _loaded = false;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var apps = await Task.Run(() => _appService.GetInstalledApps());
            var startup = await Task.Run(() => _startupService.GetStartupEntries());

            _allApps.Clear();
            _allApps.AddRange(apps.Select(a => new AppRowViewModel(a)));
            ApplyFilter();

            StartupEntries.Clear();
            foreach (var entry in startup)
            {
                StartupEntries.Add(new StartupRowViewModel(entry));
            }

            _loaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var query = SearchText?.Trim() ?? string.Empty;
        var filtered = string.IsNullOrEmpty(query)
            ? _allApps
            : _allApps.Where(a => a.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                                  || a.Publisher.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToList();

        Apps.Clear();
        foreach (var app in filtered)
        {
            Apps.Add(app);
        }

        AppCountText = $"{_allApps.Count} application(s) installée(s)";
    }

    [RelayCommand]
    private static void OpenLocation(AppRowViewModel? row)
    {
        var location = row?.Model.InstallLocation;
        if (string.IsNullOrWhiteSpace(location) || !Directory.Exists(location))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{location}\"") { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
        }
    }

    [RelayCommand]
    private async Task UninstallAsync(AppRowViewModel? row)
    {
        if (row is null || !row.CanUninstall)
        {
            return;
        }

        var confirmed = await _dialog.ConfirmAsync(
            "Désinstaller l'application",
            $"Lancer la désinstallation de « {row.Name} » ? TraceZero exécute le désinstallateur fourni par l'éditeur ; il ne supprime jamais un logiciel manuellement.",
            confirmText: "Désinstaller",
            cancelText: "Annuler",
            destructive: true);

        if (!confirmed)
        {
            return;
        }

        if (_appService.LaunchUninstaller(row.Model))
        {
            _toasts.Show($"Désinstallateur de « {row.Name} » lancé.", ToastKind.Info);
        }
        else
        {
            _toasts.Show($"Impossible de lancer le désinstallateur de « {row.Name} ».", ToastKind.Error);
        }
    }

    [RelayCommand]
    private void ToggleStartup(StartupRowViewModel? row)
    {
        if (row is null || !row.CanToggle)
        {
            return;
        }

        var desired = !row.IsEnabled;
        if (_startupService.SetEnabled(row.Model, desired))
        {
            row.IsEnabled = desired;
        }
    }
}

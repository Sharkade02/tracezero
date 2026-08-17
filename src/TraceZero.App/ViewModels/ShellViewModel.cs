using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;
using TraceZero.Application.Update;
using TraceZero.Domain.Update;

namespace TraceZero.App.ViewModels;

/// <summary>
/// ViewModel racine du shell : détient la liste des pages, la page courante, le thème et la bannière de
/// mise à jour (§28, désactivée tant que l'updater n'est pas configuré).
/// </summary>
public partial class ShellViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly IUpdateChecker _updateChecker;
    private readonly IManifestSource _manifestSource;
    private string? _updateDownloadUrl;

    public ShellViewModel(
        IEnumerable<PageViewModelBase> pages,
        IThemeService themeService,
        ILocalizationService localization,
        INavigationService navigation,
        IToastService toasts,
        IDialogService dialog,
        IUpdateChecker updateChecker,
        IManifestSource manifestSource)
    {
        _themeService = themeService;
        _updateChecker = updateChecker;
        _manifestSource = manifestSource;
        Toasts = toasts;
        Dialog = dialog;
        _themeService.ThemeChanged += (_, _) => OnPropertyChanged(nameof(IsDarkTheme));
        navigation.NavigationRequested += Navigate;

        var all = pages.ToList();
        PrimaryPages = all.Where(p => !p.IsFooter).ToList();
        FooterPages = all.Where(p => p.IsFooter).ToList();

        // Au changement de langue, réémettre les titres/chaînes calculées de toutes les pages (§31).
        localization.LanguageChanged += (_, _) =>
        {
            foreach (var page in PrimaryPages.Concat(FooterPages))
            {
                page.RefreshLocalization();
            }
        };

        Navigate(PrimaryPages.Count > 0 ? PrimaryPages[0] : null);

        // Vérification de mise à jour au démarrage (non bloquante). No-op si l'updater n'est pas configuré.
        _ = CheckForUpdatesAsync();
    }

    [ObservableProperty]
    private bool _hasUpdate;

    [ObservableProperty]
    private string _updateBannerText = string.Empty;

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            if (!_manifestSource.IsConfigured)
            {
                return;
            }

            var json = await _manifestSource.FetchAsync();
            if (string.IsNullOrWhiteSpace(json) || !Version.TryParse(AppInfo.Version, out var current))
            {
                return;
            }

            var result = _updateChecker.Check(json, current, UpdaterConfig.Channel);
            if (result.Availability is UpdateAvailability.UpdateAvailable or UpdateAvailability.BelowMinimum
                && result.Manifest is { } manifest)
            {
                _updateDownloadUrl = manifest.Url;
                UpdateBannerText = Localizer.Format("Update.Banner", manifest.Version);
                HasUpdate = true;
            }
        }
        catch (Exception)
        {
            // Une vérification de mise à jour ne doit jamais perturber le démarrage.
        }
    }

    [RelayCommand]
    private void DownloadUpdate()
    {
        if (!string.IsNullOrWhiteSpace(_updateDownloadUrl))
        {
            try
            {
                Process.Start(new ProcessStartInfo(_updateDownloadUrl) { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
            }
        }

        HasUpdate = false;
    }

    [RelayCommand]
    private void DismissUpdate() => HasUpdate = false;

    public IReadOnlyList<PageViewModelBase> PrimaryPages { get; }

    public IReadOnlyList<PageViewModelBase> FooterPages { get; }

    /// <summary>Notifications transitoires (superposition, coin bas-droit).</summary>
    public IToastService Toasts { get; }

    /// <summary>Confirmations modales (superposition centrée).</summary>
    public IDialogService Dialog { get; }

    [ObservableProperty]
    private PageViewModelBase? _current;

    public bool IsDarkTheme => _themeService.Current == AppTheme.Dark;

    [RelayCommand]
    private void Navigate(PageViewModelBase? page)
    {
        if (page is null)
        {
            return;
        }

        if (Current is not null && !ReferenceEquals(Current, page))
        {
            Current.OnDeactivated();
        }

        foreach (var candidate in PrimaryPages.Concat(FooterPages))
        {
            candidate.IsSelected = ReferenceEquals(candidate, page);
        }

        Current = page;
        page.OnActivated();
    }

    [RelayCommand]
    private void ToggleTheme() => _themeService.Toggle();
}

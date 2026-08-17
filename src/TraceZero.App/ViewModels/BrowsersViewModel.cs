using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;
using TraceZero.Application.Browsers;
using TraceZero.Domain.Browsers;

namespace TraceZero.App.ViewModels;

/// <summary>Ligne d'affichage d'un navigateur détecté.</summary>
public sealed class BrowserRowViewModel
{
    public BrowserRowViewModel(DetectedBrowser browser)
    {
        DisplayName = browser.DisplayName;
        IsRunning = browser.IsRunning;
        ProfilesText = browser.Profiles.Count > 1
            ? $"{browser.Profiles.Count} profils"
            : "1 profil";
        Glyph = browser.Kind switch
        {
            BrowserKind.Firefox => "\U0001F98A", // 🦊
            _ => "\U0001F310",                    // 🌐
        };
    }

    public string DisplayName { get; }

    public string ProfilesText { get; }

    public bool IsRunning { get; }

    public string StateText => IsRunning ? "En cours d'exécution" : "Fermé";

    public string Glyph { get; }
}

/// <summary>
/// Page Navigateurs (§14) : liste les navigateurs détectés, leurs profils et leur état d'exécution,
/// et rappelle que seules les données de cache sont nettoyées (connexions préservées).
/// </summary>
public sealed partial class BrowsersViewModel : PageViewModelBase
{
    private readonly IBrowserDetector _detector;
    private readonly INavigationService _navigation;
    private readonly CleanupViewModel _cleanup;

    public BrowsersViewModel(IBrowserDetector detector, INavigationService navigation, CleanupViewModel cleanup)
    {
        _detector = detector;
        _navigation = navigation;
        _cleanup = cleanup;
        Refresh();
    }

    public override string Title => TraceZero.App.Services.Localizer.Get("Nav.Browsers");

    public override string IconGlyph => "\U0001F310";

    public override bool IsUnderConstruction => false;

    public ObservableCollection<BrowserRowViewModel> Browsers { get; } = [];

    public bool HasBrowsers => Browsers.Count > 0;

    [RelayCommand]
    private void Refresh()
    {
        Browsers.Clear();
        foreach (var browser in _detector.DetectInstalledBrowsers())
        {
            Browsers.Add(new BrowserRowViewModel(browser));
        }

        OnPropertyChanged(nameof(HasBrowsers));
    }

    [RelayCommand]
    private void CleanCaches()
    {
        _navigation.RequestNavigate(_cleanup);
        if (_cleanup.ScanCommand.CanExecute(null))
        {
            _cleanup.ScanCommand.Execute(null);
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;

namespace TraceZero.App.ViewModels;

/// <summary>
/// ViewModel racine du shell : détient la liste des pages, la page courante et le thème.
/// </summary>
public partial class ShellViewModel : ObservableObject
{
    private readonly IThemeService _themeService;

    public ShellViewModel(
        IEnumerable<PageViewModelBase> pages,
        IThemeService themeService,
        ILocalizationService localization,
        INavigationService navigation,
        IToastService toasts,
        IDialogService dialog)
    {
        _themeService = themeService;
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
    }

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

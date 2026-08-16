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

    public ShellViewModel(IEnumerable<PageViewModelBase> pages, IThemeService themeService, INavigationService navigation)
    {
        _themeService = themeService;
        _themeService.ThemeChanged += (_, _) => OnPropertyChanged(nameof(IsDarkTheme));
        navigation.NavigationRequested += Navigate;

        var all = pages.ToList();
        PrimaryPages = all.Where(p => !p.IsFooter).ToList();
        FooterPages = all.Where(p => p.IsFooter).ToList();

        Navigate(PrimaryPages.Count > 0 ? PrimaryPages[0] : null);
    }

    public IReadOnlyList<PageViewModelBase> PrimaryPages { get; }

    public IReadOnlyList<PageViewModelBase> FooterPages { get; }

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

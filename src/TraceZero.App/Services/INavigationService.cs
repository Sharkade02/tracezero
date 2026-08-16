using TraceZero.App.ViewModels;

namespace TraceZero.App.Services;

/// <summary>
/// Découple les demandes de navigation du shell. Sans dépendance, il évite un cycle DI entre
/// <see cref="ShellViewModel"/> (qui agrège les pages) et les pages qui veulent naviguer.
/// </summary>
public interface INavigationService
{
    event Action<PageViewModelBase>? NavigationRequested;

    void RequestNavigate(PageViewModelBase page);
}

public sealed class NavigationService : INavigationService
{
    public event Action<PageViewModelBase>? NavigationRequested;

    public void RequestNavigate(PageViewModelBase page) => NavigationRequested?.Invoke(page);
}

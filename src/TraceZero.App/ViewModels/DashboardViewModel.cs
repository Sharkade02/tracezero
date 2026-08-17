using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;

namespace TraceZero.App.ViewModels;

/// <summary>
/// Page d'accueil. En Phase 0/2, aucune donnée de scan n'est affichée comme réelle avant analyse
/// (§0). Le bouton « Analyser mon PC » ouvre la page Nettoyage et lance un vrai scan.
/// </summary>
public sealed partial class DashboardViewModel : PageViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly CleanupViewModel _cleanup;

    public DashboardViewModel(INavigationService navigation, CleanupViewModel cleanup)
    {
        _navigation = navigation;
        _cleanup = cleanup;
    }

    public override string Title => TraceZero.App.Services.Localizer.Get("Nav.Home");

    public override string IconGlyph => "\U0001F3E0";

    public override bool IsUnderConstruction => false;

    [RelayCommand]
    private void Analyze()
    {
        _navigation.RequestNavigate(_cleanup);
        if (_cleanup.ScanCommand.CanExecute(null))
        {
            _cleanup.ScanCommand.Execute(null);
        }
    }
}

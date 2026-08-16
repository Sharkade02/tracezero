using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.Application.Automation;
using TraceZero.Domain.Automation;

namespace TraceZero.App.ViewModels;

/// <summary>Page Automatisation (§15) : profil, déclencheur et activation d'un nettoyage planifié.</summary>
public sealed partial class AutomationViewModel : PageViewModelBase
{
    private readonly IAutomationService _automationService;

    public AutomationViewModel(IAutomationService automationService)
    {
        _automationService = automationService;
        var config = _automationService.GetConfig();
        _isEnabled = config.Enabled;
        _selectedProfile = config.Profile;
        _selectedTrigger = config.Trigger;
        _statusMessage = config.Enabled
            ? "Nettoyage automatique activé."
            : "Le nettoyage automatique est désactivé.";
    }

    public override string Title => "Automatisation";
    public override string IconGlyph => "⚙";
    public override bool IsUnderConstruction => false;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private CleaningProfile _selectedProfile;

    [ObservableProperty]
    private AutomationTrigger _selectedTrigger;

    [ObservableProperty]
    private string _statusMessage;

    [RelayCommand]
    private void Apply()
    {
        var config = new AutomationConfig
        {
            Enabled = IsEnabled,
            Profile = SelectedProfile,
            Trigger = SelectedTrigger,
        };

        var ok = _automationService.Apply(config);

        if (!ok)
        {
            StatusMessage = "Impossible d'appliquer la planification (le Planificateur de tâches a refusé l'opération).";
            return;
        }

        StatusMessage = IsEnabled
            ? "Nettoyage automatique planifié. Il s'exécutera sans fenêtre selon le déclencheur choisi."
            : "Nettoyage automatique désactivé (tâche planifiée supprimée).";
    }
}

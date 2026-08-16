using TraceZero.Domain;
using TraceZero.Domain.Automation;

namespace TraceZero.Application.Automation;

/// <summary>
/// Planifie le nettoyage automatique via le Planificateur de tâches Windows (§15). Pas de service
/// lourd permanent.
/// </summary>
public interface IAutomationService
{
    AutomationConfig GetConfig();

    /// <summary>Applique la configuration : crée/met à jour ou supprime la tâche planifiée. Retourne vrai en cas de succès.</summary>
    bool Apply(AutomationConfig config);
}

/// <summary>Sélectionne les éléments à nettoyer selon le profil (§15).</summary>
public static class CleaningProfiles
{
    public static bool Includes(CleaningProfile profile, RiskLevel risk) => profile switch
    {
        // Jamais d'élément REVIEW en automatique.
        CleaningProfile.Safe => risk == RiskLevel.Safe,
        CleaningProfile.Privacy => risk is RiskLevel.Safe or RiskLevel.Privacy,
        _ => false,
    };
}

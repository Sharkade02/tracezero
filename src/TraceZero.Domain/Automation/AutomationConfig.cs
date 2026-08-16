namespace TraceZero.Domain.Automation;

/// <summary>Profil de nettoyage automatique (§15).</summary>
public enum CleaningProfile
{
    /// <summary>Caches et temporaires uniquement (SAFE).</summary>
    Safe = 0,

    /// <summary>Ajoute les traces de confidentialité autorisées (SAFE + PRIVACY).</summary>
    Privacy = 1,
}

/// <summary>Déclencheur de l'automatisation (§15).</summary>
public enum AutomationTrigger
{
    Weekly = 0,
    Monthly = 1,
    AtLogon = 2,
}

/// <summary>Configuration de l'automatisation, persistée localement.</summary>
public sealed record AutomationConfig
{
    public bool Enabled { get; init; }

    public CleaningProfile Profile { get; init; } = CleaningProfile.Safe;

    public AutomationTrigger Trigger { get; init; } = AutomationTrigger.Weekly;

    public static AutomationConfig Default { get; } = new();
}

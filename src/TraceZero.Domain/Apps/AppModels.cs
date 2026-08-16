namespace TraceZero.Domain.Apps;

/// <summary>Une application installée (§22).</summary>
public sealed record AppInstallation
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? Publisher { get; init; }

    public string? Version { get; init; }

    public DateOnly? InstallDate { get; init; }

    /// <summary>Taille estimée en octets (si connue).</summary>
    public long? SizeBytes { get; init; }

    public string? InstallLocation { get; init; }

    /// <summary>Commande de désinstallation déclarée par l'application.</summary>
    public string? UninstallCommand { get; init; }
}

/// <summary>Emplacement d'une entrée de démarrage.</summary>
public enum StartupLocation
{
    RunCurrentUser = 0,
    RunLocalMachine = 1,
    StartupFolder = 2,
}

/// <summary>Une entrée de démarrage automatique (§22).</summary>
public sealed record StartupEntry
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Command { get; init; }

    public required StartupLocation Location { get; init; }

    public bool IsEnabled { get; init; }

    /// <summary>Vrai si TraceZero peut activer/désactiver cette entrée sans élévation.</summary>
    public bool CanToggle { get; init; }
}

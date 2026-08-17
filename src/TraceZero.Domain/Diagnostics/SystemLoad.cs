namespace TraceZero.Domain.Diagnostics;

/// <summary>
/// Utilisation mémoire (RAM) en direct, rapportée par Windows (read-only, sans admin). Valeurs réelles
/// issues de <c>Win32_OperatingSystem</c> ; aucune estimation.
/// </summary>
public sealed record LiveMemoryUsage
{
    public long TotalBytes { get; init; }

    public long UsedBytes { get; init; }

    public long FreeBytes => Math.Max(0, TotalBytes - UsedBytes);

    /// <summary>Pourcentage utilisé (0–100) ; 0 si la capacité totale est inconnue.</summary>
    public double UsedPercent => TotalBytes > 0 ? Math.Clamp(UsedBytes * 100.0 / TotalBytes, 0, 100) : 0;

    public static LiveMemoryUsage Empty { get; } = new() { TotalBytes = 0, UsedBytes = 0 };
}

/// <summary>
/// Instantané de charge système (RAM + CPU) mesuré par Windows. <see cref="Available"/> est faux si
/// la lecture WMI a échoué — l'UI l'explique honnêtement plutôt que d'afficher un zéro trompeur.
/// </summary>
public sealed record SystemLoadSnapshot
{
    public required LiveMemoryUsage Memory { get; init; }

    /// <summary>Charge CPU globale (0–100), telle que mesurée par Windows.</summary>
    public double CpuPercent { get; init; }

    public required bool Available { get; init; }

    public static SystemLoadSnapshot Unavailable { get; } =
        new() { Memory = LiveMemoryUsage.Empty, CpuPercent = 0, Available = false };
}

/// <summary>
/// Consommation d'un programme (agrégée par nom, car un même logiciel peut avoir plusieurs process —
/// ex. un navigateur). Mesure réelle du jeu de travail (working set) rapporté par Windows.
/// </summary>
public sealed record ProcessUsage
{
    public required string Name { get; init; }

    /// <summary>Nombre de process regroupés sous ce nom.</summary>
    public int ProcessCount { get; init; }

    /// <summary>Mémoire physique utilisée (working set), en octets.</summary>
    public long WorkingSetBytes { get; init; }
}

/// <summary>
/// Indice de performance Windows (WinSAT / « Windows Experience Index »). Ce sont les scores calculés
/// par Windows lui-même, pas un score inventé par TraceZero. <see cref="Assessed"/> est faux si aucune
/// évaluation valide n'est en cache (l'utilisateur peut la relancer via <c>winsat formal</c>).
/// </summary>
public sealed record PerformanceIndex
{
    /// <summary>Score de base = le plus faible des composants (convention Windows).</summary>
    public double BaseScore { get; init; }

    public double CpuScore { get; init; }

    public double MemoryScore { get; init; }

    public double DiskScore { get; init; }

    /// <summary>Graphismes bureau (Aero).</summary>
    public double GraphicsScore { get; init; }

    /// <summary>Graphismes 3D / jeu.</summary>
    public double GamingGraphicsScore { get; init; }

    public required bool Assessed { get; init; }

    public static PerformanceIndex Unavailable { get; } = new() { Assessed = false };
}

namespace TraceZero.Domain.Diagnostics;

/// <summary>Type de mémoire (Phase RAM). Déduit de SMBIOSMemoryType.</summary>
public enum MemoryType
{
    Unknown = 0,
    Ddr3 = 3,
    Ddr4 = 4,
    Ddr5 = 5,
}

/// <summary>
/// Un module de mémoire physique (barrette), lu via WMI (read-only, sans admin). Toutes les valeurs
/// proviennent de Windows/SMBIOS. Les timings (CL, tRCD…) ne sont **pas** disponibles sans accès
/// matériel bas niveau (SMBus/SPD via driver kernel) — hors périmètre d'un outil en mode utilisateur.
/// </summary>
public sealed record MemoryModule
{
    /// <summary>Emplacement physique (ex. « DIMM A1 »).</summary>
    public required string Slot { get; init; }

    public string? Manufacturer { get; init; }

    /// <summary>Référence exacte du module (utile pour racheter les mêmes).</summary>
    public string? PartNumber { get; init; }

    public long CapacityBytes { get; init; }

    /// <summary>Fréquence nominale/étiquetée (MHz).</summary>
    public int RatedSpeedMhz { get; init; }

    /// <summary>Fréquence réelle de fonctionnement (MHz).</summary>
    public int ConfiguredSpeedMhz { get; init; }

    /// <summary>Tension configurée (millivolts) ; 0 si non rapportée.</summary>
    public int VoltageMillivolts { get; init; }

    public MemoryType Type { get; init; }
}

/// <summary>Vue d'ensemble de la mémoire installée (Phase RAM).</summary>
public sealed record MemoryReport
{
    public required IReadOnlyList<MemoryModule> Modules { get; init; }

    /// <summary>Nombre total d'emplacements (slots) de la carte mère.</summary>
    public int TotalSlots { get; init; }

    /// <summary>Capacité maximale supportée (octets), telle que rapportée par le firmware.</summary>
    public long MaxCapacityBytes { get; init; }

    public int UsedSlots => Modules.Count;

    public int FreeSlots => Math.Max(0, TotalSlots - UsedSlots);

    public long TotalCapacityBytes => Modules.Sum(m => m.CapacityBytes);

    public static MemoryReport Empty { get; } =
        new() { Modules = [], TotalSlots = 0, MaxCapacityBytes = 0 };
}

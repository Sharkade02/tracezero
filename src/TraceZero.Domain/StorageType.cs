namespace TraceZero.Domain;

/// <summary>
/// Type de stockage physique. Détermine la stratégie d'effacement sécurisé (§9) : le multi-pass
/// n'est jamais présenté comme garanti sur SSD/NVMe (wear leveling, TRIM).
/// </summary>
public enum StorageType
{
    Unknown = 0,
    Hdd = 1,
    Ssd = 2,
    Nvme = 3,
    Removable = 4,
    Network = 5,
}

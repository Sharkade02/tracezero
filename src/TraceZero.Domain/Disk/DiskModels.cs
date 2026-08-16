namespace TraceZero.Domain.Disk;

/// <summary>Informations sur un volume/lecteur (§20).</summary>
public sealed record DriveInfoModel
{
    public required string Name { get; init; }

    public string? Label { get; init; }

    public required string Format { get; init; }

    public long TotalBytes { get; init; }

    public long FreeBytes { get; init; }

    public long UsedBytes => Math.Max(0, TotalBytes - FreeBytes);

    public double UsedFraction => TotalBytes <= 0 ? 0 : Math.Clamp((double)UsedBytes / TotalBytes, 0, 1);
}

/// <summary>Un gros fichier trouvé lors de l'analyse d'espace (§20).</summary>
public sealed record LargeFileEntry
{
    public required string Path { get; init; }

    public required string FileName { get; init; }

    public long SizeBytes { get; init; }

    public DateTime LastWriteUtc { get; init; }
}

namespace TraceZero.Domain.Licensing;

public enum LicenseTier
{
    Free = 0,
    Supporter = 1,
}

/// <summary>État de licence courant (§27). Le nettoyage et la sécurité restent complets en Free.</summary>
public sealed record LicenseStatus
{
    public required LicenseTier Tier { get; init; }

    /// <summary>Nom du soutien (issu du jeton signé), le cas échéant.</summary>
    public string? SupporterName { get; init; }

    public bool IsSupporter => Tier == LicenseTier.Supporter;

    public static LicenseStatus Free { get; } = new() { Tier = LicenseTier.Free };
}

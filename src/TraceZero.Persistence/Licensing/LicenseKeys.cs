namespace TraceZero.Persistence.Licensing;

/// <summary>
/// Clé publique de vérification des jetons de soutien (§27). La clé privée correspondante n'est
/// JAMAIS embarquée : elle sert uniquement, hors ligne, à émettre les jetons côté projet.
/// </summary>
public static class LicenseKeys
{
    public const string PublicKeyPem =
        """
        -----BEGIN PUBLIC KEY-----
        MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAoNsyoWxfsN1ClFioAlG9
        hfx6ggrQd10hCePaRWxakGimdgeMpRqaec0wtHIh8l61v4Sp0L8V+tvXapBPagXK
        MEo+vcN69jfyLIUJ+KPfKZuXmYLGhLXcCr1GmknabNYe8zrK3x/YA9XauMOEEmci
        f3GIxYzhfAXfjFG/FxtdbsJ2M3m+oxoCpP3O3CTQCGzDEYPdL0bBcLe3j9ql/Gb7
        ZWF2l2KnKoUwp1oMJRwelR7mbzqQaV3ldUHKgUfDZSIRcQ/XDsCKT3uH4lUSKW8X
        AFoVoFnXvZly80Akw1IcUCQrOoWzbm4nJmJtAeTel8hg4ix0o15eVCF39/AdBw1K
        pQIDAQAB
        -----END PUBLIC KEY-----
        """;
}

namespace TraceZero.Domain.Safety;

/// <summary>
/// Résultat d'une validation de chemin avant suppression (§9). Immuable.
/// </summary>
public sealed record PathValidationResult
{
    private PathValidationResult(bool isAllowed, PathRejectionReason reason, string? canonicalPath, string? detail)
    {
        IsAllowed = isAllowed;
        Reason = reason;
        CanonicalPath = canonicalPath;
        Detail = detail;
    }

    public bool IsAllowed { get; }

    public PathRejectionReason Reason { get; }

    /// <summary>Chemin canonisé (chemin complet) lorsqu'il a pu être calculé.</summary>
    public string? CanonicalPath { get; }

    /// <summary>Détail lisible (mode Expert) expliquant la décision.</summary>
    public string? Detail { get; }

    public static PathValidationResult Allow(string canonicalPath) =>
        new(true, PathRejectionReason.None, canonicalPath, null);

    public static PathValidationResult Reject(PathRejectionReason reason, string detail, string? canonicalPath = null) =>
        new(false, reason, canonicalPath, detail);
}

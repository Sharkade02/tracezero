using TraceZero.Domain.Safety;

namespace TraceZero.Engine.Safety;

/// <summary>
/// Validateur de sécurité dédié aux opérations élevées (Phase 20, §30).
///
/// Il ne partage pas la logique de <see cref="SafePathValidator"/> — qui refuse par principe tout ce qui
/// se trouve sous <c>C:\Windows</c> — car l'élévation sert précisément à toucher un sous-ensemble
/// <b>très restreint</b> de ces emplacements (ex. <c>C:\Windows\Temp</c>).
///
/// Le contrat reste « refus par défaut » et strictement plus sévère que nécessaire :
/// <list type="bullet">
///   <item>seule une racine explicitement autorisée (et ses <b>descendants stricts</b>) est acceptée ;</item>
///   <item>la racine autorisée elle-même est refusée (on ne supprime jamais le dossier, seulement son contenu) ;</item>
///   <item>vide, caractères génériques, remontée « .. », UNC, racine de volume : refusés ;</item>
///   <item>point d'analyse / lien symbolique sur la cible ou l'un de ses parents (jusqu'à la racine) : refusé.</item>
/// </list>
///
/// Cette classe est la source d'autorité côté helper : elle ne fait jamais confiance au client UI.
/// </summary>
public sealed class ElevatedSafePathValidator
{
    private static readonly char[] WildcardChars = ['*', '?'];

    private readonly IReadOnlyList<string> _allowedRoots;

    /// <param name="allowedRoots">Racines élevées autorisées (ex. <c>C:\Windows\Temp</c>). Vide ⇒ tout est refusé.</param>
    public ElevatedSafePathValidator(IReadOnlyCollection<string> allowedRoots)
    {
        ArgumentNullException.ThrowIfNull(allowedRoots);

        var normalized = new List<string>(allowedRoots.Count);
        foreach (var root in allowedRoots)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            try
            {
                normalized.Add(Normalize(Path.GetFullPath(root)));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException
                                           or PathTooLongException or System.Security.SecurityException)
            {
                // Racine autorisée illisible : ignorée (une racine invalide n'autorise rien).
            }
        }

        _allowedRoots = normalized;
    }

    public PathValidationResult Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return PathValidationResult.Reject(PathRejectionReason.EmptyPath, "Chemin vide.");
        }

        if (path.IndexOfAny(WildcardChars) >= 0)
        {
            return PathValidationResult.Reject(
                PathRejectionReason.WildcardNotAllowed,
                "Le chemin contient un caractère générique (* ou ?).");
        }

        if (HasTraversalSegment(path))
        {
            return PathValidationResult.Reject(
                PathRejectionReason.PathTraversal,
                "Le chemin contient une remontée de répertoire (« .. »).");
        }

        if (path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal))
        {
            return PathValidationResult.Reject(
                PathRejectionReason.UncPathNotAllowed,
                "Les chemins réseau (UNC) ne sont pas autorisés.");
        }

        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException
                                       or PathTooLongException or System.Security.SecurityException)
        {
            return PathValidationResult.Reject(PathRejectionReason.InvalidPath, ex.Message);
        }

        var pathRoot = Path.GetPathRoot(full);
        if (!string.IsNullOrEmpty(pathRoot) && PathsEqual(full, pathRoot))
        {
            return PathValidationResult.Reject(
                PathRejectionReason.DriveRoot, "Le chemin est la racine d'un volume.", full);
        }

        var canonical = Normalize(full);

        // Racine autorisée : la cible doit être un descendant STRICT (jamais la racine elle-même).
        string? boundary = null;
        foreach (var root in _allowedRoots)
        {
            if (IsStrictDescendant(canonical, root))
            {
                boundary = root;
                break;
            }
        }

        if (boundary is null)
        {
            return PathValidationResult.Reject(
                PathRejectionReason.OutsideAllowedRoot,
                "Le chemin n'est pas un descendant d'une racine élevée autorisée.", canonical);
        }

        // Point d'analyse sur la cible ou l'un de ses parents jusqu'à la racine autorisée (exclue).
        if (TryFindReparsePoint(canonical, boundary, out var reparse))
        {
            return PathValidationResult.Reject(
                PathRejectionReason.ReparsePoint,
                $"Point d'analyse ou lien détecté : {reparse}.", canonical);
        }

        return PathValidationResult.Allow(canonical);
    }

    private static bool HasTraversalSegment(string path)
    {
        foreach (var segment in path.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == "..")
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
        {
            return fullPath;
        }

        var trimmed = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return trimmed.Length == 0 ? fullPath : trimmed;
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static bool IsStrictDescendant(string candidate, string ancestor)
    {
        if (string.IsNullOrEmpty(ancestor) || PathsEqual(candidate, ancestor))
        {
            return false;
        }

        var prefix = ancestor.EndsWith(Path.DirectorySeparatorChar)
            ? ancestor
            : ancestor + Path.DirectorySeparatorChar;

        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryFindReparsePoint(string canonical, string boundary, out string found)
    {
        found = string.Empty;
        var current = canonical;

        while (!string.IsNullOrEmpty(current) && !PathsEqual(current, boundary))
        {
            if (IsReparsePoint(current))
            {
                found = current;
                return true;
            }

            var parent = Path.GetDirectoryName(current);
            if (parent is null || PathsEqual(parent, current))
            {
                break;
            }

            current = parent;
        }

        return false;
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException
                                       or UnauthorizedAccessException or IOException or ArgumentException)
        {
            return false;
        }
    }
}

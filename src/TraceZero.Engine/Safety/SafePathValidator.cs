using System.Runtime.CompilerServices;
using TraceZero.Application.Safety;
using TraceZero.Domain.Safety;

namespace TraceZero.Engine.Safety;

/// <summary>
/// Implémentation de référence de <see cref="ISafePathValidator"/> (§9).
///
/// Approche « refus par défaut » : un chemin doit franchir toutes les barrières pour être autorisé.
/// L'ordre de vérification va du moins coûteux (syntaxe) au plus coûteux (accès disque pour les
/// points d'analyse).
/// </summary>
public sealed class SafePathValidator : ISafePathValidator
{
    private static readonly char[] WildcardChars = ['*', '?'];

    private readonly IKnownFolders _knownFolders;
    private readonly bool _followReparsePoints;

    public SafePathValidator(IKnownFolders knownFolders, bool followReparsePoints = false)
    {
        _knownFolders = knownFolders ?? throw new ArgumentNullException(nameof(knownFolders));
        _followReparsePoints = followReparsePoints;
    }

    public PathValidationResult Validate(string path) =>
        Validate(path, Array.Empty<string>());

    public PathValidationResult Validate(string path, IReadOnlyCollection<string> allowedRoots)
    {
        ArgumentNullException.ThrowIfNull(allowedRoots);

        // 1. Vide / espaces.
        if (string.IsNullOrWhiteSpace(path))
        {
            return PathValidationResult.Reject(PathRejectionReason.EmptyPath, "Chemin vide.");
        }

        // 2. Caractères génériques : une cible concrète n'en contient jamais.
        if (path.IndexOfAny(WildcardChars) >= 0)
        {
            return PathValidationResult.Reject(
                PathRejectionReason.WildcardNotAllowed,
                "Le chemin contient un caractère générique (* ou ?).");
        }

        // 3. Remontée de répertoire dans le chemin brut.
        if (HasTraversalSegment(path))
        {
            return PathValidationResult.Reject(
                PathRejectionReason.PathTraversal,
                "Le chemin contient une remontée de répertoire (« .. »).");
        }

        // 4. UNC (réseau) refusé par défaut.
        if (IsUncPath(path))
        {
            return PathValidationResult.Reject(
                PathRejectionReason.UncPathNotAllowed,
                "Les chemins réseau (UNC) ne sont pas autorisés par défaut.");
        }

        // 5. Canonisation.
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

        // 6. Racine de volume (ex. C:\) : comparer sur le chemin complet, avant normalisation
        // (sinon « C:\ » deviendrait « C: », un chemin relatif au lecteur).
        var pathRoot = Path.GetPathRoot(full);
        if (!string.IsNullOrEmpty(pathRoot) && PathsEqual(full, pathRoot))
        {
            return PathValidationResult.Reject(
                PathRejectionReason.DriveRoot, "Le chemin est la racine d'un volume.", full);
        }

        var canonical = Normalize(full);

        // 7. Conteneurs système protégés : lui-même ou un descendant est interdit.
        foreach (var container in _knownFolders.ForbiddenSystemContainers)
        {
            if (IsEqualOrDescendant(canonical, Normalize(container)))
            {
                return PathValidationResult.Reject(
                    PathRejectionReason.ForbiddenSystemPath,
                    $"Chemin dans un dossier système protégé : {container}.", canonical);
            }
        }

        // 8. Profil utilisateur / conteneur des profils : le chemin ne peut être égal ou parent.
        foreach (var profileRoot in new[] { _knownFolders.UserProfile, _knownFolders.UsersContainer })
        {
            if (string.IsNullOrEmpty(profileRoot))
            {
                continue;
            }

            var normalizedProfile = Normalize(profileRoot);
            if (PathsEqual(canonical, normalizedProfile) || IsStrictAncestor(canonical, normalizedProfile))
            {
                return PathValidationResult.Reject(
                    PathRejectionReason.ForbiddenUserProfile,
                    $"Le chemin est la racine du profil utilisateur ou son parent : {profileRoot}.", canonical);
            }
        }

        // 9. Dossiers personnels : ni eux, ni parent, ni descendant.
        foreach (var personal in _knownFolders.ProtectedPersonalFolders)
        {
            var normalizedPersonal = Normalize(personal);
            if (IsEqualOrDescendant(canonical, normalizedPersonal) || IsStrictAncestor(canonical, normalizedPersonal))
            {
                return PathValidationResult.Reject(
                    PathRejectionReason.ForbiddenUserFolder,
                    $"Le chemin touche un dossier personnel protégé : {personal}.", canonical);
            }
        }

        // 10. Racine autorisée (si l'opération en impose).
        string? boundary = null;
        if (allowedRoots.Count > 0)
        {
            foreach (var allowed in allowedRoots)
            {
                if (string.IsNullOrWhiteSpace(allowed))
                {
                    continue;
                }

                string normalizedRoot;
                try
                {
                    normalizedRoot = Normalize(Path.GetFullPath(allowed));
                }
                catch (Exception ex) when (ex is ArgumentException or NotSupportedException
                                               or PathTooLongException or System.Security.SecurityException)
                {
                    continue;
                }

                if (IsEqualOrDescendant(canonical, normalizedRoot))
                {
                    boundary = normalizedRoot;
                    break;
                }
            }

            if (boundary is null)
            {
                return PathValidationResult.Reject(
                    PathRejectionReason.OutsideAllowedRoot,
                    "Le chemin n'est contenu dans aucune racine autorisée pour l'opération.", canonical);
            }
        }

        // 11. Points d'analyse / liens symboliques.
        if (!_followReparsePoints && TryFindReparsePoint(canonical, boundary, out var reparse))
        {
            return PathValidationResult.Reject(
                PathRejectionReason.ReparsePoint,
                $"Point d'analyse ou lien détecté : {reparse}.", canonical);
        }

        return PathValidationResult.Allow(canonical);
    }

    private static bool HasTraversalSegment(string path)
    {
        var segments = path.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment == "..")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsUncPath(string path) =>
        path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal);

    /// <summary>Chemin complet sans séparateur final (sauf racine de volume).</summary>
    private static string Normalize(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
        {
            return fullPath;
        }

        var trimmed = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Conserver le séparateur pour une racine de volume type « C:\ ».
        return trimmed.Length == 0 ? fullPath : trimmed;
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    /// <summary><paramref name="candidate"/> est égal à <paramref name="ancestor"/> ou situé dessous.</summary>
    private static bool IsEqualOrDescendant(string candidate, string ancestor)
    {
        if (string.IsNullOrEmpty(ancestor))
        {
            return false;
        }

        if (PathsEqual(candidate, ancestor))
        {
            return true;
        }

        var prefix = ancestor.EndsWith(Path.DirectorySeparatorChar)
            ? ancestor
            : ancestor + Path.DirectorySeparatorChar;

        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary><paramref name="candidate"/> contient strictement <paramref name="descendant"/> (est au-dessus).</summary>
    private static bool IsStrictAncestor(string candidate, string descendant) =>
        !PathsEqual(candidate, descendant) && IsEqualOrDescendant(descendant, candidate);

    /// <summary>
    /// Recherche un point d'analyse sur la cible et ses parents jusqu'à la racine autorisée (exclue).
    /// Sans racine autorisée, seule la cible elle-même est inspectée.
    /// </summary>
    private static bool TryFindReparsePoint(string canonical, string? boundary, out string found)
    {
        found = string.Empty;
        var current = canonical;

        while (!string.IsNullOrEmpty(current))
        {
            if (boundary is not null && PathsEqual(current, boundary))
            {
                break;
            }

            if (IsReparsePoint(current))
            {
                found = current;
                return true;
            }

            if (boundary is null)
            {
                // Pas de frontière : on n'inspecte que la cible elle-même.
                break;
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsReparsePoint(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException
                                       or UnauthorizedAccessException or IOException or ArgumentException)
        {
            // Chemin inexistant ou illisible : rien à traverser ici.
            return false;
        }
    }
}

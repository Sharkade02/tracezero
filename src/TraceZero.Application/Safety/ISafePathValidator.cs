using TraceZero.Domain.Safety;

namespace TraceZero.Application.Safety;

/// <summary>
/// Validation centrale et obligatoire de tout chemin avant une action destructive (§9).
/// Aucune suppression ne doit contourner ce composant.
/// </summary>
public interface ISafePathValidator
{
    /// <summary>
    /// Valide un chemin en n'appliquant que les règles globales (interdictions absolues), sans
    /// restriction de racine autorisée.
    /// </summary>
    PathValidationResult Validate(string path);

    /// <summary>
    /// Valide un chemin en exigeant qu'il soit contenu dans au moins une des racines autorisées
    /// pour l'opération courante, en plus des interdictions absolues.
    /// </summary>
    /// <param name="path">Chemin cible concret (sans caractère générique).</param>
    /// <param name="allowedRoots">Racines dans lesquelles l'opération a le droit d'agir.</param>
    PathValidationResult Validate(string path, IReadOnlyCollection<string> allowedRoots);
}

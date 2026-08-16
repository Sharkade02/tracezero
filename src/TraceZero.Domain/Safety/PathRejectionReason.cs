namespace TraceZero.Domain.Safety;

/// <summary>
/// Raison précise pour laquelle un chemin a été refusé par la validation de sécurité (§9).
/// </summary>
public enum PathRejectionReason
{
    /// <summary>Aucun refus : le chemin est autorisé.</summary>
    None = 0,

    /// <summary>Chemin nul, vide ou uniquement composé d'espaces.</summary>
    EmptyPath = 1,

    /// <summary>Chemin syntaxiquement invalide ou non canonisable.</summary>
    InvalidPath = 2,

    /// <summary>Le chemin contient un caractère générique (* ou ?). Une cible concrète ne peut en contenir.</summary>
    WildcardNotAllowed = 3,

    /// <summary>Le chemin contient une remontée de répertoire (« .. »).</summary>
    PathTraversal = 4,

    /// <summary>Le chemin est la racine d'un volume (ex. C:\).</summary>
    DriveRoot = 5,

    /// <summary>Dossier système protégé (Windows, Program Files…), lui-même ou un de ses descendants.</summary>
    ForbiddenSystemPath = 6,

    /// <summary>Racine du profil utilisateur ou de C:\Users (le chemin est égal ou parent).</summary>
    ForbiddenUserProfile = 7,

    /// <summary>Dossier personnel protégé (Documents, Bureau, Téléchargements, Images, Vidéos, Musique).</summary>
    ForbiddenUserFolder = 8,

    /// <summary>Le chemin n'est pas contenu dans une racine autorisée pour l'opération.</summary>
    OutsideAllowedRoot = 9,

    /// <summary>Le chemin (ou un de ses parents jusqu'à la racine autorisée) est un point d'analyse / lien symbolique.</summary>
    ReparsePoint = 10,

    /// <summary>Chemin UNC (réseau) non autorisé par défaut.</summary>
    UncPathNotAllowed = 11,
}

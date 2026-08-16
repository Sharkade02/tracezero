namespace TraceZero.Domain;

/// <summary>
/// Nature de l'opération de suppression appliquée à une cible du système de fichiers.
/// </summary>
public enum FileActionKind
{
    /// <summary>Supprimer un fichier unique.</summary>
    DeleteFile = 0,

    /// <summary>Supprimer le contenu d'un dossier en conservant le dossier racine.</summary>
    DeleteDirectoryContents = 1,

    /// <summary>Supprimer un dossier et tout son contenu.</summary>
    DeleteDirectory = 2,

    /// <summary>Vider la Corbeille (mécanisme Windows dédié, pas une suppression de chemin).</summary>
    EmptyRecycleBin = 3,

    /// <summary>Effacer les valeurs et sous-clés d'une clé de registre (trace de confidentialité).</summary>
    ClearRegistryKey = 4,
}

namespace TraceZero.Application.Safety;

/// <summary>
/// Fournit les emplacements protégés du système. Abstrait pour permettre l'injection de faux
/// emplacements dans les tests et l'isolation vis-à-vis de l'OS.
/// </summary>
public interface IKnownFolders
{
    /// <summary>Racine du profil utilisateur (ex. C:\Users\Alice).</summary>
    string UserProfile { get; }

    /// <summary>Conteneur des profils (ex. C:\Users).</summary>
    string UsersContainer { get; }

    /// <summary>Répertoire Windows (ex. C:\Windows).</summary>
    string Windows { get; }

    /// <summary>
    /// Dossiers système protégés : leur suppression, ou celle d'un de leurs descendants, est
    /// interdite par défaut (Windows, Program Files, Program Files (x86)).
    /// </summary>
    IReadOnlyList<string> ForbiddenSystemContainers { get; }

    /// <summary>
    /// Dossiers personnels protégés : Documents, Bureau, Téléchargements, Images, Vidéos, Musique.
    /// Ni eux, ni leurs parents, ni leurs descendants ne sont supprimables par défaut.
    /// </summary>
    IReadOnlyList<string> ProtectedPersonalFolders { get; }
}

using TraceZero.Domain.Licensing;

namespace TraceZero.Application.Licensing;

/// <summary>
/// Validation de licence locale et cryptographique (§27). Aucun secret serveur dans le client,
/// aucun compte obligatoire, fonctionne hors ligne. Un jeton de soutien est signé par la clé privée
/// du projet et vérifié ici avec la clé publique embarquée.
/// </summary>
public interface ILicenseService
{
    LicenseStatus Status { get; }

    /// <summary>Vérifie et active un jeton de soutien signé. Retourne vrai s'il est valide.</summary>
    bool TryActivate(string licenseToken);

    /// <summary>Revient à l'état gratuit (supprime la licence stockée localement).</summary>
    void Deactivate();
}

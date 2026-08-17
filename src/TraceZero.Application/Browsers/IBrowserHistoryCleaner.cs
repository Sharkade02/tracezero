namespace TraceZero.Application.Browsers;

/// <summary>
/// Suppression ciblée de l'historique de navigation d'un navigateur dont l'historique et les favoris
/// cohabitent dans une même base (Firefox : <c>places.sqlite</c>). Contrairement à une suppression de
/// fichier entier, cette opération efface l'historique tout en préservant les favoris — et n'agit que si
/// l'intégrité des favoris est prouvée après coup (sinon annulation). Jamais d'action si la base est
/// verrouillée (navigateur ouvert) : rien n'est forcé (§14).
/// </summary>
public interface IBrowserHistoryCleaner
{
    /// <summary>
    /// Efface l'historique Firefox de la base indiquée en conservant les favoris. Renvoie le nombre
    /// d'octets libérés (réduction de taille du fichier), ou 0 si rien n'a pu être fait sans risque.
    /// </summary>
    long ClearFirefoxHistory(string placesDbPath);
}

namespace TraceZero.Application.Cleaning;

/// <summary>
/// Accès à la Corbeille Windows via l'API dédiée (jamais une suppression de chemin brute).
/// Abstrait pour garder le moteur portable et testable.
/// </summary>
public interface IRecycleBinService
{
    /// <summary>Taille totale occupée par la Corbeille, en octets.</summary>
    long GetUsedBytes();

    /// <summary>Nombre d'éléments dans la Corbeille.</summary>
    long GetItemCount();

    /// <summary>Vide la Corbeille. Retourne les octets libérés (mesurés avant vidage).</summary>
    long Empty();
}

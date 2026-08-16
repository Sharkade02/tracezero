using TraceZero.Application.Scanning;
using TraceZero.Domain.Duplicates;

namespace TraceZero.Application.Duplicates;

/// <summary>
/// Détecte les fichiers en double via un pipeline sûr (§21) :
/// regroupement par taille → hachage partiel rapide → hachage complet de confirmation.
/// </summary>
public interface IDuplicateFinder
{
    Task<IReadOnlyList<DuplicateGroup>> FindAsync(
        string root,
        long minimumBytes,
        IScanProgressReporter reporter,
        CancellationToken cancellationToken);
}

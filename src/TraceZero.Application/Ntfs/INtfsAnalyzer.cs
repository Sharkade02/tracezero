using TraceZero.Domain.Ntfs;

namespace TraceZero.Application.Ntfs;

/// <summary>
/// Analyse avancée des traces NTFS (Phase 8, §18), en lecture seule et Mode Expert. Ne modifie jamais le
/// système de fichiers : elle explique chaque artefact et affiche un statut honnête (détecté / géré par
/// Windows / atténuable par effacement d'espace libre).
/// </summary>
public interface INtfsAnalyzer
{
    IReadOnlyList<NtfsArtifact> Analyze();
}

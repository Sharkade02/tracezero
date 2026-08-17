using TraceZero.Application.Ntfs;
using TraceZero.Domain.Common;
using TraceZero.Domain.Ntfs;

namespace TraceZero.Engine.Ntfs;

/// <summary>
/// Analyse NTFS en lecture seule (Phase 8, §18). Décrit les artefacts de confidentialité du système de
/// fichiers et leur statut honnête. N'ouvre jamais le volume en écriture, ne lit ni ne modifie de
/// structures NTFS brutes : les artefacts nécessitant un accès privilégié sont marqués « détectés »
/// (jamais « nettoyables », jamais simulés). L'espace libre — seul artefact réellement atténuable en
/// sécurité — renvoie vers l'effacement d'espace libre (Phase 9).
/// </summary>
public sealed class NtfsAnalyzer : INtfsAnalyzer
{
    public IReadOnlyList<NtfsArtifact> Analyze()
    {
        var artifacts = new List<NtfsArtifact>
        {
            new()
            {
                Id = "ntfs.usn",
                Name = "Journal USN",
                Explanation = "Windows tient un journal des modifications de fichiers (créations, renommages, suppressions) sur chaque volume NTFS.",
                Why = "Utilisé par la recherche, la sauvegarde et l'antivirus. Il peut révéler des noms de fichiers récemment manipulés.",
                Status = NtfsArtifactStatus.ManagedByWindows,
                Detail = "Lecture/purge réservées à Windows et aux outils élevés. TraceZero ne supprime jamais le journal (cela dégraderait le système).",
            },
            new()
            {
                Id = "ntfs.mft",
                Name = "Table de fichiers maître (MFT)",
                Explanation = "La MFT décrit chaque fichier du volume. Après suppression, des références résiduelles peuvent y subsister jusqu'à réutilisation.",
                Why = "Structure fondamentale de NTFS. La modifier directement corromprait le système de fichiers.",
                Status = NtfsArtifactStatus.DetectedOnly,
                Detail = "Détectée : aucune API sûre ne permet d'effacer sélectivement ces références sans risque. TraceZero n'écrit jamais dans la MFT.",
            },
            new()
            {
                Id = "ntfs.logfile",
                Name = "$LogFile",
                Explanation = "Journal transactionnel de NTFS garantissant la cohérence en cas de coupure.",
                Why = "Nécessaire à l'intégrité du volume. Peut contenir des fragments de métadonnées récentes.",
                Status = NtfsArtifactStatus.ManagedByWindows,
                Detail = "Géré par Windows. Le retirer manuellement compromettrait la fiabilité — détecté uniquement.",
            },
            new()
            {
                Id = "ntfs.filenames",
                Name = "Résidus de noms de fichiers",
                Explanation = "Des noms de fichiers supprimés peuvent subsister dans les métadonnées jusqu'à ce que l'espace soit réutilisé.",
                Why = "Effet de bord normal de la suppression logique (le contenu et les entrées ne sont pas effacés physiquement immédiatement).",
                Status = NtfsArtifactStatus.DetectedOnly,
                Detail = "Détectée : non supprimable sélectivement de façon fiable. L'effacement de l'espace libre réduit ce risque au fil du temps.",
            },
        };

        // Espace libre : seul artefact réellement atténuable en sécurité (renvoi Phase 9).
        foreach (var drive in EnumerateFixedDrives())
        {
            artifacts.Add(new NtfsArtifact
            {
                Id = $"ntfs.freespace.{drive.RootLetter}",
                Name = $"Contenu récupérable dans l'espace libre ({drive.Name})",
                Explanation = "Les fichiers supprimés restent physiquement présents dans l'espace libre tant qu'il n'est pas réécrit, et peuvent être récupérés.",
                Why = "La suppression ne fait que marquer l'espace comme réutilisable ; le contenu demeure jusqu'à réécriture.",
                Status = NtfsArtifactStatus.MitigableByFreeSpaceWipe,
                Detail = $"{ByteSize.Format(drive.FreeBytes)} d'espace libre. Atténuation sûre : effacer l'espace libre (module « Effacement sécurisé »).",
            });
        }

        return artifacts;
    }

    private static IEnumerable<(string Name, char RootLetter, long FreeBytes)> EnumerateFixedDrives()
    {
        DriveInfo[] drives;
        try
        {
            drives = DriveInfo.GetDrives();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var drive in drives)
        {
            string name;
            char letter;
            long free;
            try
            {
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady || drive.DriveFormat != "NTFS")
                {
                    continue;
                }

                name = drive.Name;
                letter = char.ToUpperInvariant(drive.Name[0]);
                free = drive.AvailableFreeSpace;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                continue;
            }

            yield return (name, letter, free);
        }
    }
}

using System.IO.Enumeration;

namespace TraceZero.Engine.IO;

/// <summary>Métadonnées d'un fichier lues directement depuis l'énumération (sans syscall supplémentaire).</summary>
public readonly record struct FileEntry(string FullPath, string FileName, long Length, DateTime LastWriteUtc);

/// <summary>
/// Énumération de fichiers rapide et sûre (§9, §23) :
/// <list type="bullet">
///   <item>lit taille et date depuis les données de recherche Win32 (pas de <c>stat</c> par fichier) ;</item>
///   <item>ne suit jamais un point d'analyse (jonction / lien symbolique) ;</item>
///   <item>ignore silencieusement l'inaccessible.</item>
/// </list>
/// </summary>
public static class SafeFileEnumerator
{
    public static IEnumerable<FileEntry> EnumerateEntries(
        string root,
        bool recursive,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            yield break;
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = recursive,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            ReturnSpecialDirectories = false,
        };

        var enumerable = new FileSystemEnumerable<FileEntry>(
            root,
            (ref FileSystemEntry entry) => new FileEntry(
                entry.ToFullPath(),
                entry.FileName.ToString(),
                entry.Length,
                entry.LastWriteTimeUtc.UtcDateTime),
            options)
        {
            // On ne renvoie que des fichiers.
            ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory,

            // Barrière de sécurité : ne jamais descendre dans une jonction / un lien.
            ShouldRecursePredicate = (ref FileSystemEntry entry) =>
                (entry.Attributes & FileAttributes.ReparsePoint) == 0,
        };

        foreach (var entry in enumerable)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return entry;
        }
    }

    /// <summary>Sous-dossiers directs non-reparse de <paramref name="directory"/> (pour le nettoyage des dossiers vides).</summary>
    public static IEnumerable<string> EnumerateSafeChildDirectories(string directory)
    {
        IEnumerable<string> subs;
        try
        {
            subs = Directory.EnumerateDirectories(directory);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
        {
            yield break;
        }

        foreach (var sub in subs)
        {
            bool isReparse;
            try
            {
                isReparse = (File.GetAttributes(sub) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or FileNotFoundException or ArgumentException)
            {
                continue;
            }

            if (!isReparse)
            {
                yield return sub;
            }
        }
    }
}

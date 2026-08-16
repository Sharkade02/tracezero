using System.Diagnostics;

namespace TraceZero.Engine.Tests;

/// <summary>Arborescence temporaire jetable pour les tests de scan / nettoyage.</summary>
public sealed class TempTree : IDisposable
{
    public TempTree()
    {
        Root = Path.Combine(Path.GetTempPath(), "tz-engine-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string Dir(params string[] segments)
    {
        var path = Path.Combine([Root, .. segments]);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Crée un fichier de <paramref name="sizeBytes"/> octets et retourne son chemin.</summary>
    public string File(string relativePath, int sizeBytes, DateTime? lastWriteUtc = null)
    {
        var full = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        System.IO.File.WriteAllBytes(full, new byte[sizeBytes]);
        if (lastWriteUtc is { } stamp)
        {
            System.IO.File.SetLastWriteTimeUtc(full, stamp);
        }

        return full;
    }

    /// <summary>Crée une jonction (reparse point) via mklink /J, sans privilège admin.</summary>
    public static void CreateJunction(string link, string target)
    {
        var psi = new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{target}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(psi)!;
        process.WaitForExit(10_000);
    }

    public void Dispose()
    {
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(Root, "*", SearchOption.AllDirectories))
            {
                try
                {
                    if ((System.IO.File.GetAttributes(dir) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                    {
                        Directory.Delete(dir); // supprime la jonction, pas la cible
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }

            Directory.Delete(Root, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

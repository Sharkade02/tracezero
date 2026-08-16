using System.Diagnostics;
using TraceZero.Domain.Safety;
using TraceZero.Engine.Safety;

namespace TraceZero.SafetyTests;

/// <summary>
/// Vérifie sur un vrai système de fichiers qu'une jonction (reparse point) à l'intérieur d'une
/// racine autorisée est refusée : le moteur ne doit jamais sortir de la racine en suivant un lien (§9).
/// </summary>
public sealed class ReparsePointSafetyTests : IDisposable
{
    private readonly string _sandbox;
    private readonly string _outside;

    public ReparsePointSafetyTests()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "tz-safety-" + Guid.NewGuid().ToString("N"));
        _sandbox = Path.Combine(baseDir, "allowed");
        _outside = Path.Combine(baseDir, "outside");
        Directory.CreateDirectory(_sandbox);
        Directory.CreateDirectory(_outside);
    }

    [Fact]
    public void Rejects_junction_inside_allowed_root()
    {
        var link = Path.Combine(_sandbox, "escape");
        CreateJunction(link, _outside);

        var validator = new SafePathValidator(new FakeKnownFolders());
        var result = validator.Validate(link, new[] { _sandbox });

        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.ReparsePoint, result.Reason);
    }

    [Fact]
    public void Rejects_path_whose_ancestor_is_a_junction()
    {
        var link = Path.Combine(_sandbox, "escape2");
        CreateJunction(link, _outside);
        var underLink = Path.Combine(link, "some", "file.tmp");

        var validator = new SafePathValidator(new FakeKnownFolders());
        var result = validator.Validate(underLink, new[] { _sandbox });

        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.ReparsePoint, result.Reason);
    }

    [Fact]
    public void Allows_real_directory_inside_allowed_root()
    {
        var real = Path.Combine(_sandbox, "cache", "gpu");
        Directory.CreateDirectory(real);

        var validator = new SafePathValidator(new FakeKnownFolders());
        var result = validator.Validate(real, new[] { _sandbox });

        Assert.True(result.IsAllowed);
    }

    private static void CreateJunction(string link, string target)
    {
        // mklink /J crée une jonction de répertoire sans nécessiter de privilèges administrateur.
        var psi = new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{target}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(psi)
                            ?? throw new InvalidOperationException("Impossible de démarrer cmd.exe.");
        process.WaitForExit(10_000);

        Assert.True(
            Directory.Exists(link),
            $"La jonction n'a pas pu être créée (code {process.ExitCode}): {process.StandardError.ReadToEnd()}");
    }

    public void Dispose()
    {
        try
        {
            var baseDir = Path.GetDirectoryName(_sandbox);
            if (baseDir is not null && Directory.Exists(baseDir))
            {
                // Supprimer les jonctions sans suivre la cible : on retire d'abord les points d'analyse.
                foreach (var dir in Directory.EnumerateDirectories(_sandbox))
                {
                    var attrs = File.GetAttributes(dir);
                    if ((attrs & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                    {
                        Directory.Delete(dir); // supprime la jonction, pas la cible
                    }
                }

                Directory.Delete(baseDir, recursive: true);
            }
        }
        catch (IOException)
        {
            // Nettoyage best-effort.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

using System.Diagnostics;
using TraceZero.Application.Scanning;
using TraceZero.Engine.Disk;
using TraceZero.Engine.Duplicates;
using TraceZero.Engine.IO;

namespace TraceZero.PerformanceTests;

/// <summary>
/// Tests de performance/robustesse (§23) : énumération streaming, annulation prompte, filtrage par
/// seuil, hachage de doublons correct à l'échelle. Datasets synthétiques modérés pour rester rapides et
/// déterministes ; un benchmark large est disponible en opt-in (variable TZ_BIGBENCH=1).
/// </summary>
public sealed class ScanPerformanceTests : IDisposable
{
    private readonly string _root;

    public ScanPerformanceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "tz-perf-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    private static void CreateFlatFiles(string dir, int count, int contentBytes)
    {
        Directory.CreateDirectory(dir);
        var content = new string('x', contentBytes);
        for (var i = 0; i < count; i++)
        {
            File.WriteAllText(Path.Combine(dir, $"f{i}.txt"), content);
        }
    }

    [Fact]
    public void Enumerator_streams_many_files_and_counts_correctly()
    {
        // Arborescence : 3 sous-dossiers × 2000 fichiers = 6000.
        for (var d = 0; d < 3; d++)
        {
            CreateFlatFiles(Path.Combine(_root, $"d{d}"), 2000, 8);
        }

        var sw = Stopwatch.StartNew();
        var count = SafeFileEnumerator.EnumerateEntries(_root, recursive: true).Count();
        sw.Stop();

        Assert.Equal(6000, count);
        // Budget très large : on prouve surtout que l'énumération streame sans exploser.
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30), $"Énumération trop lente : {sw.Elapsed}.");
    }

    [Fact]
    public void Enumerator_cancellation_is_prompt()
    {
        CreateFlatFiles(_root, 5000, 8);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Un jeton déjà annulé doit interrompre l'énumération dès le premier élément.
        Assert.Throws<OperationCanceledException>(() =>
        {
            foreach (var _ in SafeFileEnumerator.EnumerateEntries(_root, recursive: true, cts.Token))
            {
            }
        });
    }

    [Fact]
    public async Task LargeFileScanner_filters_by_threshold()
    {
        CreateFlatFiles(Path.Combine(_root, "small"), 500, 16);        // < seuil
        CreateFlatFiles(Path.Combine(_root, "big"), 20, 200 * 1024);   // >= seuil (200 Ko)

        var scanner = new LargeFileScanner();
        var results = new List<Domain.Disk.LargeFileEntry>();
        await foreach (var entry in scanner.ScanAsync(_root, 100 * 1024, NullScanProgressReporter.Instance, CancellationToken.None))
        {
            results.Add(entry);
        }

        Assert.Equal(20, results.Count);
        Assert.All(results, r => Assert.True(r.SizeBytes >= 100 * 1024));
    }

    [Fact]
    public async Task DuplicateFinder_groups_identical_content_only()
    {
        // 30 groupes de 3 copies identiques + 100 fichiers uniques (même taille, contenu différent).
        var dupDir = Path.Combine(_root, "dups");
        Directory.CreateDirectory(dupDir);
        for (var g = 0; g < 30; g++)
        {
            var content = $"contenu-groupe-{g}-{new string((char)('a' + g % 26), 4096)}";
            for (var c = 0; c < 3; c++)
            {
                await File.WriteAllTextAsync(Path.Combine(dupDir, $"g{g}c{c}.bin"), content);
            }
        }

        for (var u = 0; u < 100; u++)
        {
            // Même longueur mais contenu distinct → ne doit jamais être groupé.
            await File.WriteAllTextAsync(Path.Combine(dupDir, $"u{u}.bin"), $"unique-{u:D4}-" + new string((char)('A' + u % 26), 4080));
        }

        var finder = new DuplicateFinder();
        var sw = Stopwatch.StartNew();
        var groups = await finder.FindAsync(_root, minimumBytes: 1024, NullScanProgressReporter.Instance, CancellationToken.None);
        sw.Stop();

        Assert.Equal(30, groups.Count);
        Assert.All(groups, grp => Assert.Equal(3, grp.Files.Count));
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30), $"Détection de doublons trop lente : {sw.Elapsed}.");
    }

    [Fact]
    public void Big_benchmark_opt_in_only()
    {
        // Benchmark 100k fichiers : coûteux, exécuté uniquement si TZ_BIGBENCH=1 (honnête, jamais en CI par défaut).
        if (Environment.GetEnvironmentVariable("TZ_BIGBENCH") != "1")
        {
            return;
        }

        for (var d = 0; d < 10; d++)
        {
            CreateFlatFiles(Path.Combine(_root, $"b{d}"), 10_000, 8);
        }

        var sw = Stopwatch.StartNew();
        var count = SafeFileEnumerator.EnumerateEntries(_root, recursive: true).Count();
        sw.Stop();

        Assert.Equal(100_000, count);
        Assert.True(sw.Elapsed < TimeSpan.FromMinutes(2), $"Énumération 100k trop lente : {sw.Elapsed}.");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

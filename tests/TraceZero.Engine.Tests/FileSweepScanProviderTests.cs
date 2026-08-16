using TraceZero.Application.Rules;
using TraceZero.Application.Scanning;
using TraceZero.Domain;
using TraceZero.Engine.Scanning;

namespace TraceZero.Engine.Tests;

public sealed class FileSweepScanProviderTests
{
    private static FileSweepRule Rule(string root, IReadOnlyList<string>? globs = null, TimeSpan? minAge = null, bool recursive = true) => new()
    {
        Id = "test.sweep",
        DisplayName = "Test",
        Category = Category.WindowsTemp,
        Risk = RiskLevel.Safe,
        Roots = [root],
        Recursive = recursive,
        IncludeGlobs = globs ?? [],
        MinimumAge = minAge,
        SelectedByDefault = true,
    };

    private static async Task<List<ScanItem>> Collect(FileSweepScanProvider provider)
    {
        var list = new List<ScanItem>();
        await foreach (var item in provider.ScanAsync(NullScanProgressReporter.Instance, CancellationToken.None))
        {
            list.Add(item);
        }

        return list;
    }

    [Fact]
    public async Task Measures_real_total_size_and_count()
    {
        using var tree = new TempTree();
        tree.File("a.dat", 1000);
        tree.File("sub/b.dat", 2500);

        var items = await Collect(new FileSweepScanProvider(Rule(tree.Root)));

        var item = Assert.Single(items);
        Assert.Equal(3500, item.SizeBytes);
        Assert.Equal(2, item.ItemCount);
        Assert.True(item.SelectedByDefault);
    }

    [Fact]
    public async Task Respects_minimum_age()
    {
        using var tree = new TempTree();
        tree.File("old.dat", 500, DateTime.UtcNow.AddDays(-3));
        tree.File("fresh.dat", 999, DateTime.UtcNow);

        var items = await Collect(new FileSweepScanProvider(Rule(tree.Root, minAge: TimeSpan.FromDays(1))));

        var item = Assert.Single(items);
        Assert.Equal(500, item.SizeBytes);
        Assert.Equal(1, item.ItemCount);
    }

    [Fact]
    public async Task Respects_include_globs()
    {
        using var tree = new TempTree();
        tree.File("keep.tmp", 100);
        tree.File("ignore.log", 100);

        var items = await Collect(new FileSweepScanProvider(Rule(tree.Root, globs: ["*.tmp"])));

        var item = Assert.Single(items);
        Assert.Equal(100, item.SizeBytes);
        Assert.Equal(1, item.ItemCount);
    }

    [Fact]
    public async Task Does_not_follow_junctions()
    {
        using var tree = new TempTree();
        tree.File("inside/real.dat", 700);

        // Cible externe contenant un fichier ; une jonction pointe dessus depuis l'arborescence scannée.
        using var outside = new TempTree();
        outside.File("secret.dat", 999999);
        TempTree.CreateJunction(Path.Combine(tree.Root, "link"), outside.Root);

        var items = await Collect(new FileSweepScanProvider(Rule(tree.Root)));

        var item = Assert.Single(items);
        // Seul real.dat est compté ; le fichier atteint via la jonction est ignoré.
        Assert.Equal(700, item.SizeBytes);
        Assert.Equal(1, item.ItemCount);
    }

    [Fact]
    public async Task Returns_nothing_for_missing_root()
    {
        var provider = new FileSweepScanProvider(Rule(Path.Combine(Path.GetTempPath(), "tz-does-not-exist-" + Guid.NewGuid().ToString("N"))));
        var items = await Collect(provider);
        Assert.Empty(items);
    }
}

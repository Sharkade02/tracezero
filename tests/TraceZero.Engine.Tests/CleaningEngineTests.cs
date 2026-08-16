using TraceZero.Domain;
using TraceZero.Domain.Cleaning;
using TraceZero.Engine.Cleaning;
using TraceZero.Engine.Safety;

namespace TraceZero.Engine.Tests;

public sealed class CleaningEngineTests
{
    private static CleaningEngine CreateEngine(FakeRecycleBin? recycleBin = null) =>
        new(new SafePathValidator(new WindowsKnownFolders()), recycleBin);

    private static ScanItem DirItem(string path, long size, IReadOnlyList<string> allowedRoots, FileActionKind kind = FileActionKind.DeleteDirectoryContents, RiskLevel risk = RiskLevel.Safe) => new()
    {
        Id = "item::" + path,
        RuleId = "rule",
        Category = Category.WindowsTemp,
        DisplayName = "Cache de test",
        PathOrIdentifier = path,
        SizeBytes = size,
        Risk = risk,
        ActionKind = kind,
        AllowedRoots = allowedRoots,
    };

    [Fact]
    public async Task Deletes_contents_frees_bytes_and_preserves_root()
    {
        using var tree = new TempTree();
        var cache = tree.Dir("cache");
        System.IO.File.WriteAllBytes(Path.Combine(cache, "a.dat"), new byte[1000]);
        var sub = Path.Combine(cache, "sub");
        Directory.CreateDirectory(sub);
        System.IO.File.WriteAllBytes(Path.Combine(sub, "b.dat"), new byte[500]);

        var engine = CreateEngine();
        var plan = engine.BuildPlan([DirItem(cache, 1500, [cache])]);
        var result = await engine.CleanAsync(plan, progress: null, CancellationToken.None);

        Assert.Equal(1500, result.BytesFreed);
        Assert.Equal(1, result.ActionsSucceeded);
        Assert.False(result.HasFailures);
        Assert.True(Directory.Exists(cache), "La racine doit être conservée (PreserveRoot).");
        Assert.Empty(Directory.EnumerateFileSystemEntries(cache));
    }

    [Fact]
    public async Task Refuses_target_outside_allowed_root()
    {
        using var allowed = new TempTree();
        using var other = new TempTree();
        var victim = Path.Combine(other.Root, "keepme.dat");
        System.IO.File.WriteAllBytes(victim, new byte[2000]);

        var engine = CreateEngine();
        // La cible est en dehors de la racine autorisée.
        var plan = engine.BuildPlan([DirItem(other.Root, 2000, [allowed.Root])]);
        var result = await engine.CleanAsync(plan, progress: null, CancellationToken.None);

        Assert.Equal(0, result.BytesFreed);
        Assert.True(result.HasFailures);
        Assert.True(System.IO.File.Exists(victim), "Aucun fichier hors racine ne doit être supprimé.");
    }

    [Fact]
    public async Task Refuses_protected_personal_folder()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var target = Path.Combine(documents, "tz-should-never-be-touched");

        var engine = CreateEngine();
        var plan = engine.BuildPlan([DirItem(target, 999, [documents])]);
        var result = await engine.CleanAsync(plan, progress: null, CancellationToken.None);

        Assert.Equal(0, result.BytesFreed);
        Assert.True(result.HasFailures);
    }

    [Fact]
    public async Task Records_failure_for_locked_file_but_completes()
    {
        using var tree = new TempTree();
        var cache = tree.Dir("cache");
        System.IO.File.WriteAllBytes(Path.Combine(cache, "free.dat"), new byte[300]);
        var lockedPath = Path.Combine(cache, "locked.dat");
        System.IO.File.WriteAllBytes(lockedPath, new byte[400]);

        var engine = CreateEngine();
        var plan = engine.BuildPlan([DirItem(cache, 700, [cache])]);

        // Verrouille un fichier de façon exclusive (pas de partage suppression).
        using (var stream = new FileStream(lockedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var result = await engine.CleanAsync(plan, progress: null, CancellationToken.None);

            Assert.Equal(300, result.BytesFreed); // seul free.dat supprimé
            Assert.True(result.HasFailures);
            Assert.Contains(result.Failures, f => f.Path.EndsWith("locked.dat", StringComparison.OrdinalIgnoreCase));
        }

        Assert.False(System.IO.File.Exists(Path.Combine(cache, "free.dat")));
        Assert.True(System.IO.File.Exists(lockedPath));
    }

    [Fact]
    public async Task Sweeps_multiple_roots_in_one_action()
    {
        using var tree = new TempTree();
        var cacheA = tree.Dir("browser", "Cache");
        var cacheB = tree.Dir("browser", "Code Cache");
        System.IO.File.WriteAllBytes(Path.Combine(cacheA, "a.bin"), new byte[1200]);
        System.IO.File.WriteAllBytes(Path.Combine(cacheB, "b.bin"), new byte[800]);

        var roots = new[] { cacheA, cacheB };
        var item = new ScanItem
        {
            Id = "browser::chrome",
            RuleId = "browser",
            Category = Category.BrowserCache,
            DisplayName = "Chrome — cache",
            PathOrIdentifier = cacheA,
            SizeBytes = 2000,
            Risk = RiskLevel.Safe,
            ActionKind = FileActionKind.DeleteDirectoryContents,
            AllowedRoots = roots,
            SweepRoots = roots,
        };

        var engine = CreateEngine();
        var result = await engine.CleanAsync(engine.BuildPlan([item]), progress: null, CancellationToken.None);

        Assert.Equal(2000, result.BytesFreed);
        Assert.False(result.HasFailures);
        Assert.Empty(Directory.EnumerateFileSystemEntries(cacheA));
        Assert.Empty(Directory.EnumerateFileSystemEntries(cacheB));
        Assert.True(Directory.Exists(cacheA));
    }

    [Fact]
    public async Task Empties_recycle_bin_via_service()
    {
        var fake = new FakeRecycleBin(bytes: 4242, count: 3);
        var engine = CreateEngine(fake);
        var item = new ScanItem
        {
            Id = "windows.recycle-bin",
            RuleId = "windows.recycle-bin",
            Category = Category.RecycleBin,
            DisplayName = "Corbeille",
            PathOrIdentifier = "shell:RecycleBinFolder",
            SizeBytes = 4242,
            Risk = RiskLevel.Review,
            ActionKind = FileActionKind.EmptyRecycleBin,
            AllowedRoots = [],
        };

        var plan = engine.BuildPlan([item]);
        var result = await engine.CleanAsync(plan, progress: null, CancellationToken.None);

        Assert.True(fake.Emptied);
        Assert.Equal(4242, result.BytesFreed);
        Assert.False(result.HasFailures);
    }
}

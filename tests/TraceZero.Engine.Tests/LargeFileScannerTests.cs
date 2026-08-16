using TraceZero.Application.Scanning;
using TraceZero.Domain.Disk;
using TraceZero.Engine.Disk;

namespace TraceZero.Engine.Tests;

public sealed class LargeFileScannerTests
{
    private static async Task<List<LargeFileEntry>> Scan(string root, long minBytes)
    {
        var results = new List<LargeFileEntry>();
        await foreach (var entry in new LargeFileScanner().ScanAsync(root, minBytes, NullScanProgressReporter.Instance, CancellationToken.None))
        {
            results.Add(entry);
        }

        return results;
    }

    [Fact]
    public async Task Returns_only_files_at_or_above_threshold()
    {
        using var tree = new TempTree();
        tree.File("big.bin", 5000);
        tree.File("sub/medium.bin", 2000);
        tree.File("small.bin", 100);

        var results = await Scan(tree.Root, minBytes: 2000);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.SizeBytes >= 2000));
        Assert.Contains(results, r => r.FileName == "big.bin");
    }

    [Fact]
    public async Task Does_not_follow_junctions()
    {
        using var tree = new TempTree();
        tree.File("inside/keep.bin", 3000);

        using var outside = new TempTree();
        outside.File("huge.bin", 9_000_000);
        TempTree.CreateJunction(Path.Combine(tree.Root, "link"), outside.Root);

        var results = await Scan(tree.Root, minBytes: 2000);

        var entry = Assert.Single(results);
        Assert.Equal("keep.bin", entry.FileName);
    }
}

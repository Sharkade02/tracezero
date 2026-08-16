using System.Text;
using TraceZero.Application.Scanning;
using TraceZero.Domain.Duplicates;
using TraceZero.Engine.Duplicates;

namespace TraceZero.Engine.Tests;

public sealed class DuplicateFinderTests
{
    private static async Task<IReadOnlyList<DuplicateGroup>> Find(string root, long minBytes = 1) =>
        await new DuplicateFinder().FindAsync(root, minBytes, NullScanProgressReporter.Instance, CancellationToken.None);

    private static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, Encoding.UTF8);
    }

    [Fact]
    public async Task Finds_identical_content_across_folders()
    {
        using var tree = new TempTree();
        Write(Path.Combine(tree.Root, "a.txt"), "le même contenu exact");
        Write(Path.Combine(tree.Root, "sub", "b.txt"), "le même contenu exact");
        Write(Path.Combine(tree.Root, "unique.txt"), "contenu différent unique");

        var groups = await Find(tree.Root);

        var group = Assert.Single(groups);
        Assert.Equal(2, group.Files.Count);
    }

    [Fact]
    public async Task Does_not_group_same_size_different_content()
    {
        using var tree = new TempTree();
        // Même longueur (10 octets), contenu différent → PAS un doublon.
        Write(Path.Combine(tree.Root, "x.bin"), "AAAAAAAAAA");
        Write(Path.Combine(tree.Root, "y.bin"), "BBBBBBBBBB");

        var groups = await Find(tree.Root);

        Assert.Empty(groups);
    }

    [Fact]
    public async Task Reports_reclaimable_bytes_for_three_copies()
    {
        using var tree = new TempTree();
        var content = new string('z', 500);
        Write(Path.Combine(tree.Root, "1.dat"), content);
        Write(Path.Combine(tree.Root, "2.dat"), content);
        Write(Path.Combine(tree.Root, "3.dat"), content);

        var group = Assert.Single(await Find(tree.Root));

        Assert.Equal(3, group.Files.Count);
        Assert.Equal(group.SizeBytes * 2, group.ReclaimableBytes);
    }
}

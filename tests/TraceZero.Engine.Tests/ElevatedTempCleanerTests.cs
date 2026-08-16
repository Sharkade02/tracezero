using TraceZero.Engine.Elevation;

namespace TraceZero.Engine.Tests;

/// <summary>
/// Le nettoyeur élevé (Phase 20) supprime uniquement le contenu éligible d'une racine autorisée,
/// respecte l'âge minimum, ne suit jamais une jonction, ne force jamais un fichier verrouillé,
/// et préserve toujours la racine elle-même.
/// </summary>
public sealed class ElevatedTempCleanerTests
{
    [Fact]
    public void Deletes_OldFiles_KeepsRecentFiles_AndReportsBytes()
    {
        using var tree = new TempTree();
        var oldFile = tree.File("old.tmp", 4096, DateTime.UtcNow.AddHours(-2));
        var recentFile = tree.File("recent.tmp", 2048, DateTime.UtcNow);

        var result = new ElevatedTempCleaner().Clean(tree.Root, minimumAgeMinutes: 60);

        Assert.True(result.Success);
        Assert.False(File.Exists(oldFile));
        Assert.True(File.Exists(recentFile));
        Assert.Equal(4096, result.BytesFreed);
        Assert.Equal(1, result.ActionsSucceeded);
        Assert.Equal(0, result.ActionsFailed);
    }

    [Fact]
    public void PreservesRoot_DeletesNestedOldFiles()
    {
        using var tree = new TempTree();
        var nested = tree.File(Path.Combine("sub", "deep.tmp"), 1024, DateTime.UtcNow.AddDays(-1));

        var result = new ElevatedTempCleaner().Clean(tree.Root, minimumAgeMinutes: 60);

        Assert.True(result.Success);
        Assert.False(File.Exists(nested));
        Assert.True(Directory.Exists(tree.Root)); // La racine n'est jamais supprimée.
        Assert.Equal(1, result.ActionsSucceeded);
    }

    [Fact]
    public void DoesNotFollowJunction()
    {
        using var outside = new TempTree();
        var protectedFile = outside.File("keep.tmp", 512, DateTime.UtcNow.AddDays(-1));

        using var tree = new TempTree();
        TempTree.CreateJunction(Path.Combine(tree.Root, "link"), outside.Root);

        var result = new ElevatedTempCleaner().Clean(tree.Root, minimumAgeMinutes: 60);

        Assert.True(result.Success);
        Assert.True(File.Exists(protectedFile)); // Le contenu de la cible de la jonction est intact.
        Assert.Equal(0, result.ActionsSucceeded);
    }

    [Fact]
    public void LockedFile_IsCountedAsFailed_NotForced()
    {
        using var tree = new TempTree();
        var locked = tree.File("locked.tmp", 256, DateTime.UtcNow.AddHours(-2));

        using (new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var result = new ElevatedTempCleaner().Clean(tree.Root, minimumAgeMinutes: 60);

            Assert.True(result.Success);
            Assert.True(File.Exists(locked)); // Jamais forcé.
            Assert.Equal(0, result.ActionsSucceeded);
            Assert.Equal(1, result.ActionsFailed);
        }
    }

    [Fact]
    public void NonexistentRoot_IsNeutralSuccess()
    {
        var result = new ElevatedTempCleaner().Clean(
            Path.Combine(Path.GetTempPath(), "tz-does-not-exist-" + Guid.NewGuid().ToString("N")),
            minimumAgeMinutes: 60);

        Assert.True(result.Success);
        Assert.Equal(0, result.BytesFreed);
        Assert.Equal(0, result.ActionsSucceeded);
    }
}

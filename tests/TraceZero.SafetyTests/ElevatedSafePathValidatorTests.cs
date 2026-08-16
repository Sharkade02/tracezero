using TraceZero.Domain.Safety;
using TraceZero.Engine.Safety;

namespace TraceZero.SafetyTests;

/// <summary>
/// Le validateur dédié à l'élévation (Phase 20, §30) n'autorise QUE les descendants stricts d'une racine
/// élevée explicitement listée, et refuse tout le reste — y compris les chemins Windows arbitraires.
/// </summary>
public sealed class ElevatedSafePathValidatorTests
{
    private static readonly string[] AllowedRoots = [@"C:\Windows\Temp"];

    private static ElevatedSafePathValidator Validator() => new(AllowedRoots);

    [Fact]
    public void Allows_StrictDescendant_OfAllowedRoot()
    {
        var result = Validator().Validate(@"C:\Windows\Temp\some-cache-file.tmp");

        Assert.True(result.IsAllowed);
        Assert.Equal(PathRejectionReason.None, result.Reason);
    }

    [Fact]
    public void Rejects_AllowedRoot_Itself()
    {
        // On ne supprime jamais le dossier lui-même, seulement son contenu.
        var result = Validator().Validate(@"C:\Windows\Temp");

        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.OutsideAllowedRoot, result.Reason);
    }

    [Theory]
    [InlineData(@"C:\Windows\System32\kernel32.dll")]   // Windows mais hors racine autorisée
    [InlineData(@"C:\Windows\notepad.exe")]
    [InlineData(@"C:\Users\Tester\Documents\cv.docx")]
    [InlineData(@"C:\Program Files\App\app.exe")]
    public void Rejects_ArbitraryPaths_OutsideAllowedRoot(string path)
    {
        var result = Validator().Validate(path);

        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.OutsideAllowedRoot, result.Reason);
    }

    [Fact]
    public void Rejects_Traversal_EvenWhenLandingInsideRoot()
    {
        var result = Validator().Validate(@"C:\Windows\Temp\..\System32\evil.dll");

        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.PathTraversal, result.Reason);
    }

    [Theory]
    [InlineData(@"C:\Windows\Temp\*.tmp", PathRejectionReason.WildcardNotAllowed)]
    [InlineData(@"C:\Windows\Temp\file?.log", PathRejectionReason.WildcardNotAllowed)]
    [InlineData(@"\\server\share\Temp\x", PathRejectionReason.UncPathNotAllowed)]
    [InlineData("", PathRejectionReason.EmptyPath)]
    [InlineData("   ", PathRejectionReason.EmptyPath)]
    public void Rejects_MalformedOrUnsafe(string path, PathRejectionReason expected)
    {
        var result = Validator().Validate(path);

        Assert.False(result.IsAllowed);
        Assert.Equal(expected, result.Reason);
    }

    [Fact]
    public void Rejects_DriveRoot()
    {
        var result = Validator().Validate(@"C:\");

        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.DriveRoot, result.Reason);
    }

    [Fact]
    public void EmptyAllowlist_RejectsEverything()
    {
        var validator = new ElevatedSafePathValidator([]);

        var result = validator.Validate(@"C:\Windows\Temp\file.tmp");

        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.OutsideAllowedRoot, result.Reason);
    }
}

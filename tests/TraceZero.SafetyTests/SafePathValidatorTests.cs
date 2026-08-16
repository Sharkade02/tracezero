using TraceZero.Domain.Safety;
using TraceZero.Engine.Safety;

namespace TraceZero.SafetyTests;

/// <summary>
/// Prouve que le moteur REFUSE toute suppression hors des règles (§9, §34).
/// Le refus par défaut est la propriété de sécurité la plus importante du produit.
/// </summary>
public sealed class SafePathValidatorTests
{
    private static SafePathValidator CreateValidator(bool followReparse = false) =>
        new(new FakeKnownFolders(), followReparse);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_paths(string? path)
    {
        var result = CreateValidator().Validate(path!);
        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.EmptyPath, result.Reason);
    }

    [Theory]
    [InlineData(@"C:\Temp\*")]
    [InlineData(@"C:\Temp\file?.tmp")]
    [InlineData(@"C:\*")]
    public void Rejects_wildcards(string path)
    {
        var result = CreateValidator().Validate(path);
        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.WildcardNotAllowed, result.Reason);
    }

    [Theory]
    [InlineData(@"C:\Temp\..\Windows\System32")]
    [InlineData(@"C:\Users\Tester\AppData\..\..\..\Windows")]
    public void Rejects_path_traversal(string path)
    {
        var result = CreateValidator().Validate(path);
        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.PathTraversal, result.Reason);
    }

    [Theory]
    [InlineData(@"\\server\share\folder")]
    [InlineData(@"//server/share")]
    public void Rejects_unc_paths(string path)
    {
        var result = CreateValidator().Validate(path);
        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.UncPathNotAllowed, result.Reason);
    }

    [Theory]
    [InlineData(@"C:\")]
    [InlineData(@"D:\")]
    public void Rejects_drive_roots(string path)
    {
        var result = CreateValidator().Validate(path);
        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.DriveRoot, result.Reason);
    }

    [Theory]
    [InlineData(@"C:\Windows")]
    [InlineData(@"C:\Windows\System32\config")]
    [InlineData(@"C:\Program Files")]
    [InlineData(@"C:\Program Files (x86)\Something")]
    public void Rejects_system_containers(string path)
    {
        var result = CreateValidator().Validate(path);
        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.ForbiddenSystemPath, result.Reason);
    }

    [Theory]
    [InlineData(@"C:\Users")]
    [InlineData(@"C:\Users\Tester")]
    public void Rejects_user_profile_and_its_parent(string path)
    {
        var result = CreateValidator().Validate(path);
        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.ForbiddenUserProfile, result.Reason);
    }

    [Theory]
    [InlineData(@"C:\Users\Tester\Documents")]
    [InlineData(@"C:\Users\Tester\Documents\budget.xlsx")]
    [InlineData(@"C:\Users\Tester\Desktop")]
    [InlineData(@"C:\Users\Tester\Downloads\installer.exe")]
    [InlineData(@"C:\Users\Tester\Pictures\photo.jpg")]
    public void Rejects_personal_folders_and_their_content(string path)
    {
        var result = CreateValidator().Validate(path);
        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.ForbiddenUserFolder, result.Reason);
    }

    [Fact]
    public void Rejects_path_outside_allowed_roots()
    {
        var allowed = new[] { @"C:\Users\Tester\AppData\Local\Temp" };
        var result = CreateValidator().Validate(@"C:\Users\Tester\AppData\Local\Google\Chrome", allowed);
        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.OutsideAllowedRoot, result.Reason);
    }

    [Fact]
    public void Allows_cache_under_allowed_root_inside_profile()
    {
        // Un cache légitime sous AppData\Local (dans le profil) doit être autorisé :
        // le profil n'est protégé qu'en tant que racine, pas ses sous-dossiers non personnels.
        var allowed = new[] { @"C:\Users\Tester\AppData\Local" };
        var target = @"C:\Users\Tester\AppData\Local\Google\Chrome\User Data\Default\GPUCache";
        var result = CreateValidator().Validate(target, allowed);
        Assert.True(result.IsAllowed);
        Assert.Equal(PathRejectionReason.None, result.Reason);
    }

    [Fact]
    public void Allows_target_equal_to_allowed_root()
    {
        var allowed = new[] { @"C:\Users\Tester\AppData\Local\Temp\tz" };
        var result = CreateValidator().Validate(@"C:\Users\Tester\AppData\Local\Temp\tz", allowed);
        Assert.True(result.IsAllowed);
    }
}

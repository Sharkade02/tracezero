using TraceZero.Domain;
using TraceZero.Domain.Safety;
using TraceZero.Engine.Cleaning;
using TraceZero.Engine.Safety;

namespace TraceZero.SafetyTests;

/// <summary>
/// Tests de sécurité additionnels (§34) : re-validation du moteur avant suppression (le plan seul
/// n'autorise rien — protection TOCTOU / fichier remplacé entre scan et clean), chemins longs et
/// jetons d'environnement non expansés. Complète <see cref="SafePathValidatorTests"/>.
/// </summary>
public sealed class Phase24SafetyTests : IDisposable
{
    private readonly string _dir;

    public Phase24SafetyTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "tz-safety-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    private static ScanItem FileItem(string path, IReadOnlyList<string> allowedRoots) => new()
    {
        Id = "test::" + path,
        RuleId = "test",
        Category = Category.WindowsTemp,
        DisplayName = "Test",
        PathOrIdentifier = path,
        Risk = RiskLevel.Safe,
        ActionKind = FileActionKind.DeleteFile,
        AllowedRoots = allowedRoots,
    };

    [Fact]
    public async Task Engine_refuses_target_outside_allowed_roots_and_keeps_the_file()
    {
        // Le fichier existe et est réel, mais la racine autorisée du plan ne le couvre pas :
        // la re-validation avant suppression doit refuser (le plan seul n'autorise jamais).
        var allowedRoot = Path.Combine(_dir, "allowed");
        var victimDir = Path.Combine(_dir, "elsewhere");
        Directory.CreateDirectory(allowedRoot);
        Directory.CreateDirectory(victimDir);
        var victim = Path.Combine(victimDir, "keep.txt");
        await File.WriteAllTextAsync(victim, "précieux");

        var engine = new CleaningEngine(new SafePathValidator(new WindowsKnownFolders()));
        var plan = engine.BuildPlan([FileItem(victim, [allowedRoot])]);

        var result = await engine.CleanAsync(plan, progress: null, CancellationToken.None);

        Assert.True(File.Exists(victim), "Le fichier hors racine autorisée ne doit jamais être supprimé.");
        Assert.Single(result.Failures);
        Assert.Equal(0, result.BytesFreed);
    }

    [Fact]
    public async Task Engine_deletes_a_file_genuinely_inside_the_allowed_root()
    {
        // Contrôle positif : sous la racine autorisée, la suppression fonctionne (le refus n'est pas systématique).
        var target = Path.Combine(_dir, "temp.txt");
        await File.WriteAllTextAsync(target, "jetable");

        var engine = new CleaningEngine(new SafePathValidator(new WindowsKnownFolders()));
        var plan = engine.BuildPlan([FileItem(target, [_dir])]);

        var result = await engine.CleanAsync(plan, progress: null, CancellationToken.None);

        Assert.False(File.Exists(target));
        Assert.Empty(result.Failures);
    }

    [Fact]
    public void Validator_long_path_under_personal_folder_is_still_rejected()
    {
        var validator = new SafePathValidator(new FakeKnownFolders());
        var deep = @"C:\Users\Tester\Documents\" + string.Join("\\", Enumerable.Repeat("sous-dossier-tres-long", 20)) + @"\fichier.txt";

        var result = validator.Validate(deep);

        Assert.False(result.IsAllowed);
        Assert.Equal(PathRejectionReason.ForbiddenUserFolder, result.Reason);
    }

    [Fact]
    public void Validator_does_not_silently_allow_unexpanded_environment_token()
    {
        var validator = new SafePathValidator(new FakeKnownFolders());
        var allowed = new[] { @"C:\Users\Tester\AppData\Local\Temp" };

        // Un jeton %TEMP% non expansé ne doit jamais être considéré comme dans la racine autorisée.
        var result = validator.Validate(@"%TEMP%\payload.exe", allowed);

        Assert.False(result.IsAllowed);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, recursive: true);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

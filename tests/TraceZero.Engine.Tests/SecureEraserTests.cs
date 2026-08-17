using TraceZero.Domain.Erasure;
using TraceZero.Engine.Erasure;
using TraceZero.Engine.Safety;

namespace TraceZero.Engine.Tests;

public sealed class SecureEraserTests : IDisposable
{
    private readonly string _dir;
    private readonly SecureEraser _eraser = new(new WindowsKnownFolders());

    public SecureEraserTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "tz-erase-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    private string SeedFile(string name = "secret.txt")
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, "données sensibles à détruire");
        return path;
    }

    [Fact]
    public async Task Single_overwrite_erases_and_deletes_the_file()
    {
        var path = SeedFile();

        var result = await _eraser.EraseFileAsync(path, SecureEraseMethod.SingleOverwrite);

        Assert.True(result.Success);
        Assert.Equal(1, result.PassesApplied);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task Reinforced_applies_three_passes()
    {
        var path = SeedFile();

        var result = await _eraser.EraseFileAsync(path, SecureEraseMethod.ReinforcedOverwrite);

        Assert.True(result.Success);
        Assert.Equal(3, result.PassesApplied);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Rejects_missing_file()
    {
        Assert.NotNull(_eraser.ValidateTarget(Path.Combine(_dir, "nope.txt")));
    }

    [Fact]
    public void Rejects_directory()
    {
        Assert.NotNull(_eraser.ValidateTarget(_dir));
    }

    [Fact]
    public void Rejects_drive_root()
    {
        var root = Path.GetPathRoot(_dir)!;
        Assert.NotNull(_eraser.ValidateTarget(root));
    }

    [Fact]
    public void Rejects_system_file_under_windows()
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var kernel = Path.Combine(windows, "System32", "kernel32.dll");
        if (!File.Exists(kernel))
        {
            return; // environnement sans ce fichier : on n'invente pas le test.
        }

        Assert.NotNull(_eraser.ValidateTarget(kernel));
    }

    [Fact]
    public void Allows_a_plain_user_file()
    {
        var path = SeedFile("ok.txt");
        Assert.Null(_eraser.ValidateTarget(path));
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

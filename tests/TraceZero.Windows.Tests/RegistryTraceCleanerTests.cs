using Microsoft.Win32;
using TraceZero.Windows.Privacy;

namespace TraceZero.Windows.Tests;

public sealed class RegistryTraceCleanerTests : IDisposable
{
    private readonly string _testKey;

    public RegistryTraceCleanerTests()
    {
        _testKey = @"Software\TraceZeroTest_" + Guid.NewGuid().ToString("N");
    }

    private void SeedTestKey()
    {
        using var key = Registry.CurrentUser.CreateSubKey(_testKey);
        key!.SetValue("a", "1");
        key.SetValue("b", "2");
        using var sub = key.CreateSubKey("child");
        sub!.SetValue("x", "y");
    }

    [Fact]
    public void Clears_allowlisted_key_but_keeps_the_key_itself()
    {
        SeedTestKey();
        var cleaner = new RegistryTraceCleaner([_testKey]);

        Assert.Equal(3, cleaner.CountEntries(_testKey)); // a, b, child
        var removed = cleaner.ClearKey(_testKey);
        Assert.Equal(3, removed);
        Assert.Equal(0, cleaner.CountEntries(_testKey));

        using var key = Registry.CurrentUser.OpenSubKey(_testKey);
        Assert.NotNull(key); // la clé elle-même est conservée
    }

    [Fact]
    public void Refuses_key_not_in_allowlist()
    {
        SeedTestKey();
        // Liste d'autorisation vide : toute clé est refusée.
        var cleaner = new RegistryTraceCleaner([]);

        Assert.False(cleaner.IsAllowed(_testKey));
        Assert.Equal(0, cleaner.CountEntries(_testKey));
        Assert.Equal(0, cleaner.ClearKey(_testKey));

        // Rien n'a été touché.
        var directCleaner = new RegistryTraceCleaner([_testKey]);
        Assert.Equal(3, directCleaner.CountEntries(_testKey));
    }

    public void Dispose()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(_testKey, throwOnMissingSubKey: false);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
        }
    }
}

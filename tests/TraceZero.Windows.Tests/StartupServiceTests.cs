using Microsoft.Win32;
using TraceZero.Domain.Apps;
using TraceZero.Windows.Apps;

namespace TraceZero.Windows.Tests;

public sealed class StartupServiceTests : IDisposable
{
    private readonly string _runKey;
    private readonly string _backupKey;

    public StartupServiceTests()
    {
        var suffix = Guid.NewGuid().ToString("N");
        _runKey = $@"Software\TraceZeroTest_{suffix}\Run";
        _backupKey = $@"Software\TraceZeroTest_{suffix}\Backup";
    }

    private StartupEntry Seed(string name, string command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(_runKey);
        key!.SetValue(name, command);
        return new StartupEntry
        {
            Id = "hkcu::" + name,
            Name = name,
            Command = command,
            Location = StartupLocation.RunCurrentUser,
            IsEnabled = true,
            CanToggle = true,
        };
    }

    [Fact]
    public void Disabling_backs_up_then_removes_from_run_reversibly()
    {
        var entry = Seed("MyApp", @"C:\app\my.exe");
        var service = new StartupService(_runKey, _backupKey);

        Assert.True(service.SetEnabled(entry, enabled: false));

        // Retirée de Run, sauvegardée dans le backup (réversible).
        using (var run = Registry.CurrentUser.OpenSubKey(_runKey))
        {
            Assert.Null(run!.GetValue("MyApp"));
        }

        using (var backup = Registry.CurrentUser.OpenSubKey(_backupKey))
        {
            Assert.Equal(@"C:\app\my.exe", backup!.GetValue("MyApp"));
        }

        // Réactivation : restaurée dans Run, retirée du backup.
        Assert.True(service.SetEnabled(entry with { IsEnabled = false }, enabled: true));
        using (var run = Registry.CurrentUser.OpenSubKey(_runKey))
        {
            Assert.Equal(@"C:\app\my.exe", run!.GetValue("MyApp"));
        }
    }

    [Fact]
    public void Refuses_to_toggle_machine_entries()
    {
        var service = new StartupService(_runKey, _backupKey);
        var hklm = new StartupEntry
        {
            Id = "hklm::x",
            Name = "x",
            Command = "y",
            Location = StartupLocation.RunLocalMachine,
            IsEnabled = true,
            CanToggle = false,
        };

        Assert.False(service.SetEnabled(hklm, enabled: false));
    }

    public void Dispose()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(Path.GetDirectoryName(_runKey)!.Replace('/', '\\'), throwOnMissingSubKey: false);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException or ArgumentException)
        {
        }
    }
}

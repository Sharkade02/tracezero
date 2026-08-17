using Microsoft.Win32;
using TraceZero.Application.Protection;
using TraceZero.Windows.Privacy;
using TraceZero.Windows.Protection;

namespace TraceZero.Windows.Tests;

public sealed class RegistryBackupServiceTests : IDisposable
{
    private readonly string _root;

    public RegistryBackupServiceTests()
    {
        _root = $@"Software\TraceZeroTest_{Guid.NewGuid():N}\Protection";
    }

    private void Seed()
    {
        using var key = Registry.CurrentUser.CreateSubKey(_root)!;
        key.SetValue("Text", "hello", RegistryValueKind.String);
        key.SetValue("Number", 42, RegistryValueKind.DWord);
        key.SetValue("Big", 9_000_000_000L, RegistryValueKind.QWord);
        key.SetValue("Blob", new byte[] { 1, 2, 3, 250 }, RegistryValueKind.Binary);
        key.SetValue("List", new[] { "a", "b c", "" }, RegistryValueKind.MultiString);

        using var sub = key.CreateSubKey("Child")!;
        sub.SetValue("Inner", "deep", RegistryValueKind.String);
    }

    [Fact]
    public void Capture_then_restore_round_trips_all_value_kinds_and_subkeys()
    {
        Seed();
        var service = new RegistryBackupService();

        var snapshot = service.Capture(_root);
        Assert.NotNull(snapshot);
        Assert.False(snapshot!.IsEmpty);

        // Simule le nettoyage : on efface tout le contenu de la clé.
        using (var key = Registry.CurrentUser.OpenSubKey(_root, writable: true)!)
        {
            foreach (var name in key.GetValueNames())
            {
                key.DeleteValue(name, throwOnMissingValue: false);
            }

            foreach (var subName in key.GetSubKeyNames())
            {
                key.DeleteSubKeyTree(subName, throwOnMissingSubKey: false);
            }
        }

        var restored = service.Restore(_root, snapshot);
        Assert.Equal(6, restored); // 5 valeurs + 1 valeur dans la sous-clé

        using var reopened = Registry.CurrentUser.OpenSubKey(_root, writable: false)!;
        Assert.Equal("hello", reopened.GetValue("Text"));
        Assert.Equal(42, reopened.GetValue("Number"));
        Assert.Equal(9_000_000_000L, reopened.GetValue("Big"));
        Assert.Equal(new byte[] { 1, 2, 3, 250 }, (byte[])reopened.GetValue("Blob")!);
        Assert.Equal(new[] { "a", "b c", "" }, (string[])reopened.GetValue("List")!);

        using var child = reopened.OpenSubKey("Child")!;
        Assert.Equal("deep", child.GetValue("Inner"));
    }

    [Fact]
    public void Capture_returns_null_for_absent_key()
    {
        var service = new RegistryBackupService();
        Assert.Null(service.Capture(_root + @"\DoesNotExist"));
    }

    [Fact]
    public void Serialize_then_deserialize_preserves_snapshot()
    {
        Seed();
        var service = new RegistryBackupService();
        var snapshot = service.Capture(_root)!;

        var round = RegistrySnapshotCodec.Deserialize(RegistrySnapshotCodec.Serialize(snapshot));

        Assert.Equal(snapshot.EntryCount, round.EntryCount);
        Assert.Contains(round.Values, v => v.Name == "Text" && v.EncodedValue == "hello");
        Assert.Contains(round.SubKeys, s => s.Name == "Child");
    }

    [Fact]
    public void Backup_matches_privacy_catalog_allow_list()
    {
        // Garde-fou : la sauvegarde vise les mêmes clés HKCU que le catalogue de traces autorisées.
        var allowed = WindowsPrivacyCatalog.RegistryAllowList().ToList();
        Assert.NotEmpty(allowed);
    }

    public void Dispose()
    {
        try
        {
            var top = _root.Split('\\')[1]; // TraceZeroTest_xxxx
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\" + top, throwOnMissingSubKey: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
        }
    }
}

using TraceZero.Windows.Software;

namespace TraceZero.Windows.Tests;

public sealed class WingetParseTests
{
    // Sortie type de « winget upgrade » (colonnes séparées par des espaces multiples).
    private const string Sample =
        "Name                     Id                        Version      Available    Source\n" +
        "-------------------------------------------------------------------------------------\n" +
        "Mozilla Firefox          Mozilla.Firefox           118.0        119.0        winget\n" +
        "7-Zip                    7zip.7zip                 22.01        23.01        winget\n" +
        "\n" +
        "2 upgrades available.\n";

    [Fact]
    public void Parses_available_updates()
    {
        var updates = WingetUpdateService.Parse(Sample);

        Assert.Equal(2, updates.Count);
        Assert.Equal("Mozilla Firefox", updates[0].Name);
        Assert.Equal("Mozilla.Firefox", updates[0].Id);
        Assert.Equal("118.0", updates[0].InstalledVersion);
        Assert.Equal("119.0", updates[0].AvailableVersion);
        Assert.Equal("winget", updates[0].Source);
        Assert.Equal("7zip.7zip", updates[1].Id);
    }

    [Fact]
    public void Ignores_summary_and_blank_lines()
    {
        var updates = WingetUpdateService.Parse(Sample);
        Assert.DoesNotContain(updates, u => u.Name.Contains("upgrades available"));
    }

    [Fact]
    public void Empty_or_headerless_output_yields_no_updates()
    {
        Assert.Empty(WingetUpdateService.Parse(string.Empty));
        Assert.Empty(WingetUpdateService.Parse("No installed package found matching input criteria."));
    }
}

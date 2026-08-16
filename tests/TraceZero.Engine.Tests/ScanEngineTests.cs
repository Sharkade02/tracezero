using TraceZero.Domain;
using TraceZero.Domain.Scanning;
using TraceZero.Engine.Scanning;

namespace TraceZero.Engine.Tests;

public sealed class ScanEngineTests
{
    private static ScanItem Item(string id, long size) => new()
    {
        Id = id,
        RuleId = id,
        Category = Category.WindowsTemp,
        DisplayName = id,
        PathOrIdentifier = @"C:\whatever\" + id,
        SizeBytes = size,
        Risk = RiskLevel.Safe,
    };

    [Fact]
    public async Task Aggregates_items_from_all_providers()
    {
        var engine = new ScanEngine(
        [
            new StubProvider("p1", Item("a", 100), Item("b", 200)),
            new StubProvider("p2", Item("c", 300)),
        ]);

        var report = await engine.ScanAsync(progress: null, CancellationToken.None);

        Assert.Equal(3, report.TotalItems);
        Assert.Equal(600, report.TotalBytes);
        Assert.Empty(report.Errors);
    }

    [Fact]
    public async Task Isolates_a_failing_provider()
    {
        var engine = new ScanEngine(
        [
            new StubProvider("ok", Item("a", 100)),
            new ThrowingProvider("bad"),
        ]);

        var report = await engine.ScanAsync(progress: null, CancellationToken.None);

        Assert.Single(report.Items);
        var error = Assert.Single(report.Errors);
        Assert.Equal("bad", error.ProviderId);
    }

    [Fact]
    public async Task Reports_progress()
    {
        var updates = new List<ScanProgress>();
        var progress = new Progress<ScanProgress>(updates.Add);
        var engine = new ScanEngine([new StubProvider("p1", Item("a", 100))]);

        await engine.ScanAsync(progress, CancellationToken.None);

        // Laisse le SynchronizationContext du Progress<> écouler ses callbacks.
        await Task.Delay(50);
        Assert.NotEmpty(updates);
        Assert.Equal(1, updates[^1].TotalProviders);
    }

    [Fact]
    public async Task Honors_cancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var engine = new ScanEngine([new StubProvider("p1", Item("a", 100))]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => engine.ScanAsync(progress: null, cts.Token));
    }
}

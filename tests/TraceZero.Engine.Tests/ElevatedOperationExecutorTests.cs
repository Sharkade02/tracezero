using TraceZero.Domain.Elevation;
using TraceZero.Engine.Elevation;

namespace TraceZero.Engine.Tests;

/// <summary>
/// L'exécuteur côté helper (Phase 20, §30) refuse tout protocole/opération inconnu et résout lui-même
/// la liste d'autorisation dédiée (le client ne fournit jamais de chemin).
/// </summary>
public sealed class ElevatedOperationExecutorTests
{
    [Fact]
    public void Rejects_UnknownProtocolVersion()
    {
        var executor = new ElevatedOperationExecutor();

        var result = executor.Execute(new ElevatedRequest
        {
            ProtocolVersion = 999,
            Operation = ElevatedOperation.CleanWindowsTemp,
        });

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Rejects_UnknownOperation()
    {
        var executor = new ElevatedOperationExecutor();

        var result = executor.Execute(new ElevatedRequest
        {
            Operation = (ElevatedOperation)987,
        });

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void CleanWindowsTemp_UsesHelperResolvedTempFolder()
    {
        using var tree = new TempTree();
        // Simule %SystemRoot% : le helper compose lui-même « <windows>\Temp ».
        var windowsDir = tree.Root;
        var tempDir = tree.Dir("Temp");
        var old = tree.File(Path.Combine("Temp", "old.tmp"), 1024, DateTime.UtcNow.AddDays(-1));

        var executor = new ElevatedOperationExecutor(windowsDirectoryProvider: () => windowsDir);

        var result = executor.Execute(new ElevatedRequest { Operation = ElevatedOperation.CleanWindowsTemp });

        Assert.True(result.Success);
        Assert.False(File.Exists(old));
        Assert.Equal(1024, result.BytesFreed);
    }
}

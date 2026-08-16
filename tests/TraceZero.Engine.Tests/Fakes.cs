using System.Runtime.CompilerServices;
using TraceZero.Application.Cleaning;
using TraceZero.Application.Scanning;
using TraceZero.Domain;

namespace TraceZero.Engine.Tests;

/// <summary>Fournisseur de scan qui renvoie des éléments fixes.</summary>
internal sealed class StubProvider(string id, params ScanItem[] items) : IScanProvider
{
    public string Id => id;
    public string DisplayName => id;
    public Category Category => Category.Unknown;

    public async IAsyncEnumerable<ScanItem> ScanAsync(
        IScanProgressReporter reporter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return item;
        }
    }
}

/// <summary>Fournisseur qui échoue : sert à vérifier l'isolation des erreurs (§12).</summary>
internal sealed class ThrowingProvider(string id) : IScanProvider
{
    public string Id => id;
    public string DisplayName => id;
    public Category Category => Category.Unknown;

#pragma warning disable CS1998, CS0162 // itérateur async volontairement sans await ; yield inaccessible voulu
    public async IAsyncEnumerable<ScanItem> ScanAsync(
        IScanProgressReporter reporter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("boom");
        yield break;
    }
#pragma warning restore CS1998, CS0162
}

internal sealed class FakeRecycleBin(long bytes, long count) : IRecycleBinService
{
    public bool Emptied { get; private set; }
    public long GetUsedBytes() => bytes;
    public long GetItemCount() => count;

    public long Empty()
    {
        Emptied = true;
        return bytes;
    }
}

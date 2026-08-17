using TraceZero.Engine.Erasure;

namespace TraceZero.Engine.Tests;

public sealed class FreeSpaceWiperTests : IDisposable
{
    private readonly string _dir;
    private readonly FreeSpaceWiper _wiper = new();

    public FreeSpaceWiperTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "tz-wipe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    [Fact]
    public async Task Wipes_up_to_cap_then_removes_fill_file_and_leaves_existing_files_untouched()
    {
        // Fichier existant témoin : ne doit jamais être touché.
        var sentinel = Path.Combine(_dir, "keepme.txt");
        await File.WriteAllTextAsync(sentinel, "intact");

        // Cap volontairement petit (2 Mo) pour ne jamais remplir un vrai disque.
        var result = await _wiper.WipeAsync(_dir, maxBytes: 2 * 1024 * 1024, progress: null);

        Assert.True(result.Success);
        Assert.True(result.BytesWritten >= 2 * 1024 * 1024);

        // Le fichier de remplissage est supprimé ; le témoin est intact.
        Assert.Empty(Directory.GetFiles(_dir, "tracezero-freespace-*.tmp"));
        Assert.Equal("intact", await File.ReadAllTextAsync(sentinel));
    }

    [Fact]
    public async Task Cancellation_is_reported_and_no_fill_file_remains()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _wiper.WipeAsync(_dir, maxBytes: 0, progress: null, cts.Token);

        Assert.True(result.Canceled);
        Assert.False(result.Success);
        Assert.Empty(Directory.GetFiles(_dir, "tracezero-freespace-*.tmp"));
    }

    [Fact]
    public async Task Missing_working_directory_fails_honestly()
    {
        var result = await _wiper.WipeAsync(Path.Combine(_dir, "does-not-exist"), maxBytes: 1024, progress: null);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
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

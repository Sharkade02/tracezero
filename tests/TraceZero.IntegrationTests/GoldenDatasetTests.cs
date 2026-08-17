using TraceZero.Application.Rules;
using TraceZero.Application.Scanning;
using TraceZero.Domain;
using TraceZero.Domain.Exclusions;
using TraceZero.Engine.Cleaning;
using TraceZero.Engine.Safety;
using TraceZero.Engine.Scanning;
using TraceZero.Persistence;

namespace TraceZero.IntegrationTests;

/// <summary>
/// Tests de non-régression par « golden dataset » (§35). Un faux profil contient des fichiers
/// dangereux (caches) et des fichiers à préserver (documents, sessions, favoris, exclusions). Après
/// nettoyage réel, les caches doivent disparaître et tout le reste rester intact.
/// </summary>
public sealed class GoldenDatasetTests : IDisposable
{
    private readonly string _profile;
    private readonly string _tempCache;
    private readonly string _chromeCache;
    private readonly string _documents;
    private readonly string _bookmark;
    private readonly string _session;

    public GoldenDatasetTests()
    {
        _profile = Path.Combine(Path.GetTempPath(), "tz-golden-" + Guid.NewGuid().ToString("N"));

        _tempCache = Path.Combine(_profile, "Temp");
        _chromeCache = Path.Combine(_profile, "Chrome", "Cache");
        _documents = Path.Combine(_profile, "Documents");
        var chrome = Path.Combine(_profile, "Chrome");
        var protectedDir = Path.Combine(_profile, "Protected");

        foreach (var d in new[] { _tempCache, _chromeCache, _documents, chrome, protectedDir })
        {
            Directory.CreateDirectory(d);
        }

        // Fichiers dangereux (doivent disparaître).
        File.WriteAllText(Path.Combine(_tempCache, "junk1.tmp"), "temp");
        File.WriteAllText(Path.Combine(_tempCache, "junk2.tmp"), "temp");
        File.WriteAllText(Path.Combine(_chromeCache, "data_0"), "cache");
        File.WriteAllText(Path.Combine(_chromeCache, "data_1"), "cache");

        // Fichiers à préserver.
        _documents = Path.Combine(_documents, "budget.xlsx");
        File.WriteAllText(_documents, "important");
        _bookmark = Path.Combine(chrome, "Bookmarks");
        File.WriteAllText(_bookmark, "mes favoris");
        _session = Path.Combine(protectedDir, "session.dat");
        File.WriteAllText(_session, "connexion active");
    }

    private static FileSweepRule Rule(string id, Category category, string root) => new()
    {
        Id = id,
        DisplayName = id,
        Category = category,
        Risk = RiskLevel.Safe,
        Roots = [root],
        Recursive = true,
        PreserveRoot = true,
        SelectedByDefault = true,
    };

    private async Task<List<ScanItem>> CollectItemsAsync()
    {
        var providers = new IScanProvider[]
        {
            new FileSweepScanProvider(Rule("temp", Category.WindowsTemp, _tempCache)),
            new FileSweepScanProvider(Rule("chrome-cache", Category.WindowsCache, _chromeCache)),
        };

        var items = new List<ScanItem>();
        foreach (var provider in providers)
        {
            await foreach (var item in provider.ScanAsync(NullScanProgressReporter.Instance, CancellationToken.None))
            {
                items.Add(item);
            }
        }

        return items;
    }

    private void AssertProtectedFilesIntact()
    {
        Assert.True(File.Exists(_documents), "Les documents doivent rester présents.");
        Assert.True(File.Exists(_bookmark), "Les favoris doivent rester présents.");
        Assert.True(File.Exists(_session), "Les sessions protégées doivent rester présentes.");
    }

    [Fact]
    public async Task Cleaning_removes_caches_but_keeps_documents_bookmarks_and_sessions()
    {
        var engine = new CleaningEngine(new SafePathValidator(new WindowsKnownFolders()));
        var plan = engine.BuildPlan(await CollectItemsAsync());

        var result = await engine.CleanAsync(plan, progress: null, CancellationToken.None);

        // Caches prévus : absents.
        Assert.Empty(Directory.GetFiles(_tempCache));
        Assert.Empty(Directory.GetFiles(_chromeCache));
        Assert.Empty(result.Failures);

        AssertProtectedFilesIntact();
    }

    [Fact]
    public async Task Excluded_cache_is_preserved()
    {
        var exclusionsFile = Path.Combine(_profile, "exclusions.json");
        var store = new JsonExclusionStore(exclusionsFile);
        store.Add(new ExclusionRule
        {
            Id = Guid.NewGuid(),
            Kind = ExclusionKind.Folder,
            Value = _chromeCache,
            DisplayName = "Cache Chrome protégé",
            CreatedUtc = DateTimeOffset.UtcNow,
        });

        // Filtrage identique à l'automatisation (§15) : on écarte les éléments exclus.
        var selected = (await CollectItemsAsync()).Where(i => !store.IsExcluded(i)).ToList();

        var engine = new CleaningEngine(new SafePathValidator(new WindowsKnownFolders()));
        await engine.CleanAsync(engine.BuildPlan(selected), progress: null, CancellationToken.None);

        // L'exclusion est respectée : le cache Chrome reste présent, le Temp non exclu est nettoyé.
        Assert.NotEmpty(Directory.GetFiles(_chromeCache));
        Assert.Empty(Directory.GetFiles(_tempCache));
        AssertProtectedFilesIntact();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_profile))
            {
                Directory.Delete(_profile, recursive: true);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

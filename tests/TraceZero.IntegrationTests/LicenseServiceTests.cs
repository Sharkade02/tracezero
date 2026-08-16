using System.Security.Cryptography;
using TraceZero.Persistence.Licensing;

namespace TraceZero.IntegrationTests;

public sealed class LicenseServiceTests : IDisposable
{
    private readonly string _file;
    private readonly RSA _rsa;
    private readonly string _publicPem;

    public LicenseServiceTests()
    {
        _file = Path.Combine(Path.GetTempPath(), "tz-lic-" + Guid.NewGuid().ToString("N"), "license.token");
        _rsa = RSA.Create(2048);
        _publicPem = _rsa.ExportSubjectPublicKeyInfoPem();
    }

    [Fact]
    public void Activates_a_validly_signed_token()
    {
        var token = LicenseService.CreateToken(_rsa, "Alice");
        var service = new LicenseService(_publicPem, _file);

        Assert.True(service.TryActivate(token));
        Assert.True(service.Status.IsSupporter);
        Assert.Equal("Alice", service.Status.SupporterName);
    }

    [Fact]
    public void Rejects_a_token_signed_by_a_different_key()
    {
        using var otherKey = RSA.Create(2048);
        var forged = LicenseService.CreateToken(otherKey, "Mallory");
        var service = new LicenseService(_publicPem, _file);

        Assert.False(service.TryActivate(forged));
        Assert.False(service.Status.IsSupporter);
    }

    [Fact]
    public void Rejects_a_tampered_token()
    {
        var token = LicenseService.CreateToken(_rsa, "Alice");
        var tampered = token[..^3] + "AAA";
        var service = new LicenseService(_publicPem, _file);

        Assert.False(service.TryActivate(tampered));
    }

    [Fact]
    public void Persists_activation_across_instances()
    {
        var token = LicenseService.CreateToken(_rsa, "Bob");
        Assert.True(new LicenseService(_publicPem, _file).TryActivate(token));

        var reopened = new LicenseService(_publicPem, _file);
        Assert.True(reopened.Status.IsSupporter);
        Assert.Equal("Bob", reopened.Status.SupporterName);
    }

    public void Dispose()
    {
        _rsa.Dispose();
        try
        {
            var dir = Path.GetDirectoryName(_file);
            if (dir is not null && Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

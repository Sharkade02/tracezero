using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TraceZero.Domain.Update;
using TraceZero.Updater;

namespace TraceZero.IntegrationTests;

public sealed class UpdateCheckerTests : IDisposable
{
    private readonly RSA _rsa = RSA.Create(2048);

    private string PublicKeyPem => _rsa.ExportSubjectPublicKeyInfoPem();

    private string BuildSignedManifestJson(
        string version, string channel = "stable", string? minimum = null, bool tamper = false)
    {
        var manifest = new UpdateManifest
        {
            Version = version,
            Channel = channel,
            Url = "https://tracezero.app/download/setup.exe",
            Sha256 = "abc123",
            Signature = string.Empty,
            MinimumSupportedVersion = minimum,
        };

        var payload = Encoding.UTF8.GetBytes(UpdateChecker.SignedPayload(manifest));
        var signature = Convert.ToBase64String(_rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

        var signed = manifest with { Signature = signature };
        if (tamper)
        {
            // Modifier un champ APRÈS la signature : la vérification doit échouer.
            signed = signed with { Url = "https://malveillant.example/evil.exe" };
        }

        return JsonSerializer.Serialize(signed);
    }

    [Fact]
    public void Newer_signed_version_is_available()
    {
        var checker = new UpdateChecker(PublicKeyPem);
        var result = checker.Check(BuildSignedManifestJson("1.2.3"), new Version(1, 0, 0), UpdateChannel.Stable);

        Assert.Equal(UpdateAvailability.UpdateAvailable, result.Availability);
        Assert.Equal("1.2.3", result.Manifest!.Version);
    }

    [Fact]
    public void Same_or_older_version_is_up_to_date()
    {
        var checker = new UpdateChecker(PublicKeyPem);
        var result = checker.Check(BuildSignedManifestJson("1.0.0"), new Version(1, 0, 0), UpdateChannel.Stable);

        Assert.Equal(UpdateAvailability.UpToDate, result.Availability);
    }

    [Fact]
    public void Tampered_manifest_is_rejected()
    {
        var checker = new UpdateChecker(PublicKeyPem);
        var result = checker.Check(BuildSignedManifestJson("2.0.0", tamper: true), new Version(1, 0, 0), UpdateChannel.Stable);

        Assert.Equal(UpdateAvailability.ManifestInvalid, result.Availability);
    }

    [Fact]
    public void Wrong_public_key_rejects_signature()
    {
        using var other = RSA.Create(2048);
        var checker = new UpdateChecker(other.ExportSubjectPublicKeyInfoPem());
        var result = checker.Check(BuildSignedManifestJson("2.0.0"), new Version(1, 0, 0), UpdateChannel.Stable);

        Assert.Equal(UpdateAvailability.ManifestInvalid, result.Availability);
    }

    [Fact]
    public void Below_minimum_supported_version_forces_update()
    {
        var checker = new UpdateChecker(PublicKeyPem);
        var result = checker.Check(
            BuildSignedManifestJson("2.0.0", minimum: "1.5.0"), new Version(1, 0, 0), UpdateChannel.Stable);

        Assert.Equal(UpdateAvailability.BelowMinimum, result.Availability);
    }

    [Fact]
    public void Beta_manifest_is_not_offered_on_stable_channel()
    {
        var checker = new UpdateChecker(PublicKeyPem);
        var result = checker.Check(BuildSignedManifestJson("2.0.0", channel: "beta"), new Version(1, 0, 0), UpdateChannel.Stable);

        Assert.Equal(UpdateAvailability.ChannelMismatch, result.Availability);
    }

    [Fact]
    public void Beta_channel_accepts_beta_manifest()
    {
        var checker = new UpdateChecker(PublicKeyPem);
        var result = checker.Check(BuildSignedManifestJson("2.0.0", channel: "beta"), new Version(1, 0, 0), UpdateChannel.Beta);

        Assert.Equal(UpdateAvailability.UpdateAvailable, result.Availability);
    }

    public void Dispose() => _rsa.Dispose();
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TraceZero.Application.Licensing;
using TraceZero.Domain.Licensing;

namespace TraceZero.Persistence.Licensing;

/// <summary>
/// Vérifie les jetons de soutien signés (RSA-SHA256) avec une clé publique embarquée, et mémorise
/// localement l'activation (§27). Format du jeton : base64url(payload).base64url(signature),
/// où payload est un JSON { name, tier }.
/// </summary>
public sealed class LicenseService : ILicenseService
{
    private readonly RSA? _rsa;
    private readonly string _licenseFilePath;

    public LicenseService(string publicKeyPem, string licenseFilePath)
    {
        _licenseFilePath = licenseFilePath;

        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            _rsa = rsa;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            _rsa = null; // licence non configurée : mode gratuit uniquement
        }

        Status = LoadStoredStatus();
    }

    public LicenseStatus Status { get; private set; }

    public bool TryActivate(string licenseToken)
    {
        var status = Validate(licenseToken);
        if (status is null)
        {
            return false;
        }

        try
        {
            var directory = Path.GetDirectoryName(_licenseFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_licenseFilePath, licenseToken.Trim());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Même si l'écriture échoue, la licence est valide pour la session.
        }

        Status = status;
        return true;
    }

    public void Deactivate()
    {
        try
        {
            if (File.Exists(_licenseFilePath))
            {
                File.Delete(_licenseFilePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }

        Status = LicenseStatus.Free;
    }

    private LicenseStatus LoadStoredStatus()
    {
        try
        {
            if (File.Exists(_licenseFilePath))
            {
                var token = File.ReadAllText(_licenseFilePath);
                return Validate(token) ?? LicenseStatus.Free;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }

        return LicenseStatus.Free;
    }

    private LicenseStatus? Validate(string? token)
    {
        if (_rsa is null || string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var parts = token.Trim().Split('.');
        if (parts.Length != 2)
        {
            return null;
        }

        byte[] payloadBytes;
        byte[] signature;
        try
        {
            payloadBytes = FromBase64Url(parts[0]);
            signature = FromBase64Url(parts[1]);
        }
        catch (FormatException)
        {
            return null;
        }

        if (!_rsa.VerifyData(payloadBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<LicensePayload>(payloadBytes);
            if (payload is null || !string.Equals(payload.Tier, "Supporter", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new LicenseStatus { Tier = LicenseTier.Supporter, SupporterName = payload.Name };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static byte[] FromBase64Url(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }

    private sealed record LicensePayload
    {
        public string? Name { get; init; }
        public string? Tier { get; init; }
    }

    /// <summary>Encode un jeton (utilisé côté émission / tests). Nécessite la clé privée.</summary>
    public static string CreateToken(RSA privateKey, string name)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new LicensePayload { Name = name, Tier = "Supporter" });
        var signature = privateKey.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return ToBase64Url(payload) + "." + ToBase64Url(signature);
    }

    private static string ToBase64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

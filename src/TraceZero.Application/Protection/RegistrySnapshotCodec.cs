using System.Text.Json;
using TraceZero.Domain.Protection;

namespace TraceZero.Application.Protection;

/// <summary>
/// Sérialisation portable des instantanés de registre (§17). Isolée ici pour rester testable sans
/// dépendance Windows et pour centraliser le format persisté dans le coffre de protection.
/// </summary>
public static class RegistrySnapshotCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
    };

    public static string Serialize(RegistryKeySnapshot snapshot) =>
        JsonSerializer.Serialize(snapshot, Options);

    public static RegistryKeySnapshot Deserialize(string payload) =>
        JsonSerializer.Deserialize<RegistryKeySnapshot>(payload, Options)
        ?? throw new FormatException("Instantané de registre invalide.");
}

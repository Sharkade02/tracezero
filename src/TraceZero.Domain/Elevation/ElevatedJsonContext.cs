using System.Text.Json;
using System.Text.Json.Serialization;

namespace TraceZero.Domain.Elevation;

/// <summary>
/// Contexte de sérialisation source-generated pour l'IPC du helper élevé (Phase 20).
/// Partagé par le client (UI) et le helper afin de garantir un format identique des deux côtés,
/// sans réflexion à l'exécution.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(ElevatedRequest))]
[JsonSerializable(typeof(ElevatedResult))]
public sealed partial class ElevatedJsonContext : JsonSerializerContext
{
}

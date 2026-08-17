using TraceZero.Domain.Update;

namespace TraceZero.Application.Update;

/// <summary>
/// Évalue un manifeste de mise à jour (§28) : vérifie sa signature cryptographique et décide s'il faut
/// mettre à jour. Ne télécharge ni n'exécute rien — un manifeste invalide n'est jamais accepté.
/// </summary>
public interface IUpdateChecker
{
    UpdateCheckResult Check(string manifestJson, Version currentVersion, UpdateChannel channel);
}

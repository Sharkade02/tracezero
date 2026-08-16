using TraceZero.Application.Safety;

namespace TraceZero.SafetyTests;

/// <summary>
/// Emplacements protégés déterministes (indépendants de la machine) pour les tests de logique.
/// </summary>
internal sealed class FakeKnownFolders : IKnownFolders
{
    public string UserProfile { get; init; } = @"C:\Users\Tester";

    public string UsersContainer { get; init; } = @"C:\Users";

    public string Windows { get; init; } = @"C:\Windows";

    public IReadOnlyList<string> ForbiddenSystemContainers { get; init; } =
        [@"C:\Windows", @"C:\Program Files", @"C:\Program Files (x86)"];

    public IReadOnlyList<string> ProtectedPersonalFolders { get; init; } =
    [
        @"C:\Users\Tester\Documents",
        @"C:\Users\Tester\Desktop",
        @"C:\Users\Tester\Downloads",
        @"C:\Users\Tester\Pictures",
        @"C:\Users\Tester\Videos",
        @"C:\Users\Tester\Music",
    ];
}

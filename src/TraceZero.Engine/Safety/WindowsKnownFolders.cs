using TraceZero.Application.Safety;

namespace TraceZero.Engine.Safety;

/// <summary>
/// Implémentation par défaut de <see cref="IKnownFolders"/> basée sur les dossiers spéciaux Windows.
/// </summary>
public sealed class WindowsKnownFolders : IKnownFolders
{
    public WindowsKnownFolders()
    {
        UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        // C:\Users est le parent du profil. Fallback prudent si le profil est vide.
        UsersContainer = string.IsNullOrEmpty(UserProfile)
            ? string.Empty
            : Path.GetDirectoryName(UserProfile.TrimEnd(Path.DirectorySeparatorChar)) ?? string.Empty;

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        ForbiddenSystemContainers = NonEmpty(Windows, programFiles, programFilesX86);

        ProtectedPersonalFolders = NonEmpty(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            string.IsNullOrEmpty(UserProfile) ? string.Empty : Path.Combine(UserProfile, "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
    }

    public string UserProfile { get; }

    public string UsersContainer { get; }

    public string Windows { get; }

    public IReadOnlyList<string> ForbiddenSystemContainers { get; }

    public IReadOnlyList<string> ProtectedPersonalFolders { get; }

    private static string[] NonEmpty(params string[] values) =>
        values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}

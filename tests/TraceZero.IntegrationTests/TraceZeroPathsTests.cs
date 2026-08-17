using TraceZero.Persistence;

namespace TraceZero.IntegrationTests;

public sealed class TraceZeroPathsTests
{
    [Fact]
    public void Installed_mode_uses_localappdata()
    {
        var dir = TraceZeroPaths.ResolveDataDirectory(@"C:\Program Files\TraceZero", @"C:\Users\me\AppData\Local", portable: false);
        Assert.Equal(@"C:\Users\me\AppData\Local\TraceZero", dir);
    }

    [Fact]
    public void Portable_mode_uses_folder_next_to_exe()
    {
        var dir = TraceZeroPaths.ResolveDataDirectory(@"D:\TraceZeroPortable", @"C:\Users\me\AppData\Local", portable: true);
        Assert.Equal(@"D:\TraceZeroPortable\Data", dir);
    }
}

using TraceZero.Domain.Diagnostics;
using TraceZero.Storage;

namespace TraceZero.IntegrationTests;

public sealed class DiskHealthServiceTests
{
    [Fact]
    public void GetDiskHealth_never_throws_and_returns_a_list()
    {
        var service = new DiskHealthService();

        // Le service isole toute erreur WMI et renvoie une liste (vide au pire), jamais d'exception.
        var disks = service.GetDiskHealth();

        Assert.NotNull(disks);

        // Sur une vraie machine Windows, chaque disque a un modèle et un état exploitable.
        foreach (var disk in disks)
        {
            Assert.False(string.IsNullOrWhiteSpace(disk.Model));
            Assert.True(Enum.IsDefined(disk.Status));
            Assert.True(Enum.IsDefined(disk.Media));
        }
    }
}

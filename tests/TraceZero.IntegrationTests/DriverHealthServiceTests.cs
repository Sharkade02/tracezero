using TraceZero.Windows.Diagnostics;

namespace TraceZero.IntegrationTests;

public sealed class DriverHealthServiceTests
{
    [Fact]
    public void GetDrivers_never_throws_and_returns_a_list()
    {
        var service = new DriverHealthService();

        // Toute erreur WMI est isolée : liste (vide au pire), jamais d'exception.
        var drivers = service.GetDrivers();

        Assert.NotNull(drivers);

        foreach (var driver in drivers)
        {
            Assert.False(string.IsNullOrWhiteSpace(driver.DeviceName));
            Assert.True(driver.ProblemCode >= 0);
        }
    }
}

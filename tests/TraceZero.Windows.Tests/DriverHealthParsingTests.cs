using TraceZero.Windows.Diagnostics;

namespace TraceZero.Windows.Tests;

public sealed class DriverHealthParsingTests
{
    [Fact]
    public void Parses_cim_datetime_to_date()
    {
        var date = DriverHealthService.ParseCimDate("20230115000000.000000-000");

        Assert.NotNull(date);
        Assert.Equal(new DateOnly(2023, 1, 15), date);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]        // trop court / non numérique
    [InlineData("20231301000000.000000-000")] // mois 13 invalide
    [InlineData("20230230000000.000000-000")] // 30 février invalide
    public void Returns_null_for_invalid_input(string? input)
    {
        Assert.Null(DriverHealthService.ParseCimDate(input));
    }
}

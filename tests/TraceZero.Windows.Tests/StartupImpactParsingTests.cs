using TraceZero.Windows.Diagnostics;

namespace TraceZero.Windows.Tests;

public sealed class StartupImpactParsingTests
{
    // Événement 101 réel simplifié (Diagnostics-Performance/Operational).
    private const string SampleEvent =
        """
        <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
          <System>
            <Provider Name="Microsoft-Windows-Diagnostics-Performance" />
            <EventID>101</EventID>
          </System>
          <EventData>
            <Data Name="Name">MonApp.exe</Data>
            <Data Name="FriendlyName">Mon Application</Data>
            <Data Name="Path">C:\Apps\MonApp.exe</Data>
            <Data Name="TotalTime">820</Data>
            <Data Name="DegradationTime">610</Data>
          </EventData>
        </Event>
        """;

    [Fact]
    public void Parses_name_and_total_time()
    {
        var ok = StartupImpactService.TryParseEventXml(SampleEvent, out var name, out var ms);

        Assert.True(ok);
        Assert.Equal("MonApp.exe", name);
        Assert.Equal(820, ms);
    }

    [Fact]
    public void Rejects_event_without_total_time()
    {
        const string xml =
            """
            <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
              <EventData><Data Name="Name">X.exe</Data></EventData>
            </Event>
            """;

        Assert.False(StartupImpactService.TryParseEventXml(xml, out _, out _));
    }

    [Fact]
    public void Rejects_malformed_xml()
    {
        Assert.False(StartupImpactService.TryParseEventXml("not xml <", out _, out _));
    }
}

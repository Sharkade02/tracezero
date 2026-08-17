using System.Diagnostics.Eventing.Reader;
using System.Xml.Linq;
using TraceZero.Application.Diagnostics;
using TraceZero.Domain.Diagnostics;

namespace TraceZero.Windows.Diagnostics;

/// <summary>
/// Impact des programmes au démarrage, mesuré par Windows (Phase 28). Lit le journal
/// « Microsoft-Windows-Diagnostics-Performance/Operational » (événement 101 : application ayant
/// rallongé le démarrage) en lecture seule, et agrège la pénalité moyenne par programme.
/// Ce n'est pas une estimation : chaque valeur vient d'un démarrage réellement mesuré par Windows.
/// La lecture de ce journal peut exiger des droits élevés ; le cas échéant, le rapport est marqué
/// indisponible plutôt que faussé.
/// </summary>
public sealed class StartupImpactService : IStartupImpactService
{
    private const string LogName = "Microsoft-Windows-Diagnostics-Performance/Operational";
    private const string EventNamespace = "http://schemas.microsoft.com/win/2004/08/events/event";

    public StartupImpactReport GetRecentImpacts(int maxBoots = 10)
    {
        // On borne la lecture pour rester léger : quelques centaines d'événements couvrent largement
        // les derniers démarrages.
        var maxEvents = Math.Clamp(maxBoots, 1, 50) * 30;

        var totals = new Dictionary<string, (double Sum, int Count)>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var query = new EventLogQuery(LogName, PathType.LogName, "*[System[(EventID=101)]]")
            {
                ReverseDirection = true, // les plus récents d'abord
            };

            using var reader = new EventLogReader(query);

            var read = 0;
            for (EventRecord? record = reader.ReadEvent();
                 record is not null && read < maxEvents;
                 record = reader.ReadEvent())
            {
                using (record)
                {
                    read++;
                    if (TryParse(record, out var name, out var ms))
                    {
                        var entry = totals.TryGetValue(name, out var existing) ? existing : (0d, 0);
                        totals[name] = (entry.Item1 + ms, entry.Item2 + 1);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or EventLogException or EventLogNotFoundException)
        {
            // Journal inaccessible (droits requis) ou absent : honnêtement indisponible.
            return StartupImpactReport.Unavailable;
        }

        var impacts = totals
            .Select(kv => new StartupImpact
            {
                Name = kv.Key,
                AverageMs = kv.Value.Sum / kv.Value.Count,
                SampleCount = kv.Value.Count,
            })
            .OrderByDescending(i => i.AverageMs)
            .ToList();

        return new StartupImpactReport { Impacts = impacts, DataAvailable = true };
    }

    private static bool TryParse(EventRecord record, out string name, out double ms)
    {
        try
        {
            return TryParseEventXml(record.ToXml(), out name, out ms);
        }
        catch (EventLogException)
        {
            name = string.Empty;
            ms = 0;
            return false;
        }
    }

    /// <summary>
    /// Extrait le nom du programme et la pénalité (ms) d'un événement 101 de Diagnostics-Performance.
    /// Public et pur pour être testable sans journal Windows.
    /// </summary>
    public static bool TryParseEventXml(string eventXml, out string name, out double ms)
    {
        name = string.Empty;
        ms = 0;

        try
        {
            var doc = XDocument.Parse(eventXml);
            XNamespace ns = EventNamespace;

            string? foundName = null;
            double foundMs = 0;

            foreach (var data in doc.Descendants(ns + "Data"))
            {
                var key = data.Attribute("Name")?.Value;
                if (key == "Name")
                {
                    foundName = data.Value;
                }
                else if (key == "TotalTime" && double.TryParse(
                             data.Value, System.Globalization.NumberStyles.Integer,
                             System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                {
                    foundMs = parsed;
                }
            }

            if (string.IsNullOrWhiteSpace(foundName) || foundMs <= 0)
            {
                return false;
            }

            name = foundName.Trim();
            ms = foundMs;
            return true;
        }
        catch (System.Xml.XmlException)
        {
            return false;
        }
    }
}

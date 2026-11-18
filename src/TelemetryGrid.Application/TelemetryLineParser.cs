using System.Globalization;
using TelemetryGrid.Domain;

namespace TelemetryGrid.Application;

public static class TelemetryLineParser
{
    // Wire format: sensor|timestamp|value|unit|quality
    // The parser deliberately avoids Split(), Regex and LINQ in the hot path.
    public static bool TryParse(ReadOnlySpan<char> line, out TelemetryReading? reading)
    {
        reading = null;
        Span<Range> fields = stackalloc Range[5];
        var fieldCount = 0;
        var start = 0;
        while (fieldCount < fields.Length - 1)
        {
            var separator = line[start..].IndexOf('|');
            if (separator < 0) break;
            separator += start;
            fields[fieldCount++] = start..separator;
            start = separator + 1;
        }
        fields[fieldCount++] = start..line.Length;
        if (fieldCount != 5) return false;

        var sensor = line[fields[0]];
        var timestampText = line[fields[1]];
        var valueText = line[fields[2]];
        var unit = line[fields[3]];
        var qualityText = line[fields[4]];
        if (sensor.IsEmpty || unit.IsEmpty ||
            !DateTimeOffset.TryParse(timestampText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp) ||
            !double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
            !int.TryParse(qualityText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quality))
            return false;

        reading = new TelemetryReading(sensor.ToString(), timestamp, value, unit.ToString(), quality);
        return reading.IsValid;
    }
}

using System.Diagnostics;
using TelemetryGrid.Application;
using Xunit;

namespace TelemetryGrid.Tests;

public sealed class TelemetryLineParserTests
{
    [Fact]
    public void Parses_wire_format_without_split_or_regex()
    {
        var line = "S-01|2026-01-01T00:00:00.0000000+00:00|42.5|°C|100";

        Assert.True(TelemetryLineParser.TryParse(line.AsSpan(), out var reading));
        Assert.NotNull(reading);
        Assert.Equal("S-01", reading!.SensorId);
        Assert.Equal(42.5, reading.Value);
    }

    [Fact]
    public void Rejects_malformed_or_invalid_quality_lines()
    {
        Assert.False(TelemetryLineParser.TryParse("S-01|bad|42.5|°C|100".AsSpan(), out _));
        Assert.False(TelemetryLineParser.TryParse("S-01|2026-01-01T00:00:00Z|42.5|°C|101".AsSpan(), out _));
    }

    [Fact]
    public void Parses_fifteen_hundred_messages_in_the_hot_path()
    {
        var line = "S-01|2026-01-01T00:00:00.0000000+00:00|42.5|°C|100";
        var stopwatch = Stopwatch.StartNew();
        for (var index = 0; index < 1_500; index++)
            Assert.True(TelemetryLineParser.TryParse(line.AsSpan(), out _));
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2));
    }
}

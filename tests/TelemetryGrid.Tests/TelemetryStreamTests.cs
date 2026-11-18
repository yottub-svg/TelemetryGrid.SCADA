using TelemetryGrid.Application;
using TelemetryGrid.Domain;
using Xunit;

namespace TelemetryGrid.Tests;

public sealed class TelemetryStreamTests
{
    [Fact]
    public async Task Invalid_reading_is_rejected()
    {
        await using var stream = new TelemetryStream();
        var reading = new TelemetryReading("", DateTimeOffset.UtcNow, 1, "kPa", 100);
        Assert.False(stream.TryPublish(reading));
    }

    [Fact]
    public async Task Published_readings_are_delivered_as_a_batch()
    {
        await using var stream = new TelemetryStream();
        var delivered = new TaskCompletionSource<IReadOnlyList<TelemetryReading>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        stream.BatchReady += (_, batch) => delivered.TrySetResult(batch);

        Assert.True(stream.TryPublish(new TelemetryReading("S-01", DateTimeOffset.UtcNow, 42, "°C", 100)));
        var batch = await delivered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Single(batch);
        Assert.Equal("S-01", batch[0].SensorId);
    }
}

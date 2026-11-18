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

    [Fact]
    public async Task Disposing_the_stream_completes_without_deadlock()
    {
        var stream = new TelemetryStream();
        await stream.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Bounded_channel_accepts_a_high_frequency_burst_without_blocking_the_producer()
    {
        await using var stream = new TelemetryStream(capacity: 8);
        var accepted = 0;

        for (var index = 0; index < 2_000; index++)
        {
            if (stream.TryPublish(new TelemetryReading($"S-{index % 8}", DateTimeOffset.UtcNow, index, "°C", 100)))
                accepted++;
        }

        Assert.Equal(2_000, accepted);
    }

    [Fact]
    public async Task Deterministic_validation_scenario_passes_one_hundred_times()
    {
        for (var run = 0; run < 100; run++)
        {
            await using var stream = new TelemetryStream();
            var delivered = new TaskCompletionSource<IReadOnlyList<TelemetryReading>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            stream.BatchReady += (_, batch) => delivered.TrySetResult(batch);
            Assert.True(stream.TryPublish(new TelemetryReading($"S-{run}", DateTimeOffset.UtcNow, run, "°C", 100)));

            var batch = await delivered.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.Single(batch);
        }
    }
}

using TelemetryGrid.Application;
using TelemetryGrid.Domain;
using Xunit;

namespace TelemetryGrid.Tests;

public sealed class TelemetryIngestionWorkerTests
{
    [Fact]
    public async Task Graceful_shutdown_flushes_all_confirmed_readings()
    {
        var queue = new TelemetryIngestionQueue(capacity: 32);
        var sink = new RecordingSink();
        var worker = new TelemetryIngestionWorker(queue, sink, TimeSpan.FromMilliseconds(5));
        await worker.StartAsync(CancellationToken.None);

        var expected = Enumerable.Range(0, 20)
            .Select(index => new TelemetryReading($"S-{index}", DateTimeOffset.UtcNow, index, "°C", 100))
            .ToArray();
        foreach (var reading in expected) await queue.EnqueueAsync(reading);

        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(expected.Length, sink.Readings.Count);
        Assert.Equal(expected.Select(x => x.SensorId), sink.Readings.Select(x => x.SensorId));
    }

    [Fact]
    public async Task Bounded_queue_applies_backpressure_without_dropping_confirmed_items()
    {
        var queue = new TelemetryIngestionQueue(capacity: 2);
        var sink = new RecordingSink();
        var worker = new TelemetryIngestionWorker(queue, sink, TimeSpan.FromMilliseconds(1));
        await worker.StartAsync(CancellationToken.None);

        for (var index = 0; index < 100; index++)
            await queue.EnqueueAsync(new TelemetryReading($"S-{index}", DateTimeOffset.UtcNow, index, "°C", 100));
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(100, sink.Readings.Count);
    }

    private sealed class RecordingSink : ITelemetryBatchSink
    {
        public List<TelemetryReading> Readings { get; } = [];

        public Task SaveBatchAsync(IReadOnlyList<TelemetryReading> batch, CancellationToken cancellationToken)
        {
            Readings.AddRange(batch);
            return Task.CompletedTask;
        }
    }
}

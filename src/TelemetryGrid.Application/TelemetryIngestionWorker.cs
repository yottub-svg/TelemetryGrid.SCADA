using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using TelemetryGrid.Domain;

namespace TelemetryGrid.Application;

public interface ITelemetryBatchSink
{
    Task SaveBatchAsync(IReadOnlyList<TelemetryReading> batch, CancellationToken cancellationToken);
}

public sealed class TelemetryIngestionQueue(int capacity = 10_000)
{
    private readonly Channel<TelemetryReading> _channel = Channel.CreateBounded<TelemetryReading>(new BoundedChannelOptions(capacity)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });

    public ChannelReader<TelemetryReading> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(TelemetryReading reading, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(reading, cancellationToken);

    public bool TryEnqueue(TelemetryReading reading) => _channel.Writer.TryWrite(reading);

    public void Complete() => _channel.Writer.TryComplete();
}

public sealed class TelemetryIngestionWorker(
    TelemetryIngestionQueue queue,
    ITelemetryBatchSink sink,
    TimeSpan? flushInterval = null) : BackgroundService
{
    private readonly TimeSpan _flushInterval = flushInterval ?? TimeSpan.FromMilliseconds(100);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pending = new List<TelemetryReading>(256);
        try
        {
            while (await queue.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
            {
                while (queue.Reader.TryRead(out var item)) pending.Add(item);
                await FlushWhenReadyAsync(pending, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // StopAsync requests a graceful drain in the finally block.
        }
        finally
        {
            while (queue.Reader.TryRead(out var item)) pending.Add(item);
            if (pending.Count > 0)
                await sink.SaveBatchAsync(pending.ToArray(), CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task FlushWhenReadyAsync(List<TelemetryReading> pending, CancellationToken cancellationToken)
    {
        if (pending.Count == 0) return;
        await Task.Delay(_flushInterval, cancellationToken).ConfigureAwait(false);
        var batch = pending.ToArray();
        pending.Clear();
        await sink.SaveBatchAsync(batch, cancellationToken).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        queue.Complete();
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}

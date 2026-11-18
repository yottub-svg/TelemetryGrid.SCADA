using System.Threading.Channels;
using TelemetryGrid.Domain;

namespace TelemetryGrid.Application;

public sealed class TelemetryStream : IAsyncDisposable
{
    private readonly Channel<TelemetryReading> _channel;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task _consumerTask;

    public event EventHandler<IReadOnlyList<TelemetryReading>>? BatchReady;

    public TelemetryStream(int capacity = 5_000)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

        _channel = Channel.CreateBounded<TelemetryReading>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _consumerTask = ConsumeAsync(_shutdown.Token);
    }

    public bool TryPublish(TelemetryReading reading)
        => reading.IsValid && _channel.Writer.TryWrite(reading);

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        var batch = new List<TelemetryReading>(256);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var item)) batch.Add(item);
                if (batch.Count == 0) continue;

                var snapshot = batch.ToArray();
                batch.Clear();
                BatchReady?.Invoke(this, snapshot);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown path.
        }
        finally
        {
            timer.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        await _shutdown.CancelAsync().ConfigureAwait(false);
        try { await _consumerTask.ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        _shutdown.Dispose();
    }
}

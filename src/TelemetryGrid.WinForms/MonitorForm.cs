using TelemetryGrid.Application;
using TelemetryGrid.Domain;

namespace TelemetryGrid.WinForms;

public sealed class MonitorForm : Form
{
    private readonly TelemetryStream _stream;
    private readonly Label _status = new();
    private readonly ListBox _events = new();

    public MonitorForm(TelemetryStream stream)
    {
        _stream = stream;
        Text = "TelemetryGrid.SCADA — монитор телеметрии";
        Width = 900;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;
        _status.Dock = DockStyle.Top;
        _status.Height = 32;
        _status.Text = "Ожидание телеметрии";
        _events.Dock = DockStyle.Fill;
        _events.TabIndex = 0;
        _events.AccessibleName = "Поток событий телеметрии";
        Controls.Add(_events);
        Controls.Add(_status);
        _stream.BatchReady += OnBatchReady;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _stream.BatchReady -= OnBatchReady;
        base.Dispose(disposing);
    }

    private void OnBatchReady(object? sender, IReadOnlyList<TelemetryReading> batch)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(() => OnBatchReady(sender, batch));
            return;
        }

        _status.Text = $"Получено событий: {batch.Count} · {DateTime.Now:T}";
        foreach (var reading in batch)
            _events.Items.Insert(0, $"{reading.Timestamp:T} | {reading.SensorId} | {reading.Value:F2} {reading.Unit}");
        while (_events.Items.Count > 1_000) _events.Items.RemoveAt(_events.Items.Count - 1);
    }
}

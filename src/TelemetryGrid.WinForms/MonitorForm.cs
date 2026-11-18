using TelemetryGrid.Application;
using TelemetryGrid.Domain;

namespace TelemetryGrid.WinForms;

public sealed class MonitorForm : Form
{
    private readonly TelemetryStream _stream;
    private readonly Label _status = new();
    private readonly Label _emptyState = new();
    private readonly ListBox _events = new();
    private readonly Button _clearButton = new();
    private readonly Button _refreshButton = new();

    public MonitorForm(TelemetryStream stream)
    {
        _stream = stream;
        InitializeLayout();
        _stream.BatchReady += OnBatchReady;
        SetState(ViewState.Empty);
    }

    private void InitializeLayout()
    {
        Text = "TelemetryGrid.SCADA — монитор телеметрии";
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(640, 420);
        BackColor = ThemeTokens.Canvas;
        ForeColor = ThemeTokens.TextPrimary;
        KeyPreview = true;
        AccessibleName = "Монитор телеметрии TelemetryGrid.SCADA";
        AccessibleRole = AccessibleRole.Application;
        KeyDown += OnKeyDown;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16),
            BackColor = ThemeTokens.Canvas
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbar = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false,
            AccessibleName = "Панель команд монитора"
        };
        _refreshButton.Text = "Обновить";
        _refreshButton.AutoSize = true;
        _refreshButton.TabIndex = 0;
        _refreshButton.AccessibleName = "Обновить состояние монитора";
        _refreshButton.AccessibleRole = AccessibleRole.PushButton;
        _refreshButton.Click += (_, _) => SetState(_events.Items.Count == 0 ? ViewState.Empty : ViewState.Data);

        _clearButton.Text = "Очистить";
        _clearButton.AutoSize = true;
        _clearButton.TabIndex = 1;
        _clearButton.AccessibleName = "Очистить поток событий";
        _clearButton.AccessibleRole = AccessibleRole.PushButton;
        _clearButton.Click += (_, _) =>
        {
            _events.Items.Clear();
            SetState(ViewState.Empty);
        };
        toolbar.Controls.Add(_refreshButton);
        toolbar.Controls.Add(_clearButton);

        _status.AutoSize = true;
        _status.Dock = DockStyle.Fill;
        _status.Padding = new Padding(0, 8, 0, 8);
        _status.AccessibleName = "Состояние монитора";
        _status.AccessibleRole = AccessibleRole.StatusBar;

        var surface = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeTokens.Surface,
            Padding = new Padding(12),
            TabStop = false
        };
        surface.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, surface.ClientRectangle, ThemeTokens.Border, ButtonBorderStyle.Solid);

        _events.Dock = DockStyle.Fill;
        _events.TabIndex = 2;
        _events.AccessibleName = "Поток событий телеметрии";
        _events.AccessibleRole = AccessibleRole.List;
        _events.IntegralHeight = false;

        _emptyState.Dock = DockStyle.Fill;
        _emptyState.Text = "Нет данных телеметрии. Ожидание событий…";
        _emptyState.TextAlign = ContentAlignment.MiddleCenter;
        _emptyState.ForeColor = ThemeTokens.TextMuted;
        _emptyState.AccessibleName = "Пустое состояние: данных нет";
        _emptyState.AccessibleRole = AccessibleRole.StaticText;

        surface.Controls.Add(_events);
        surface.Controls.Add(_emptyState);
        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(_status, 0, 1);
        root.Controls.Add(surface, 0, 2);
        Controls.Add(root);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stream.BatchReady -= OnBatchReady;
            KeyDown -= OnKeyDown;
        }
        base.Dispose(disposing);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.K)
        {
            _events.Focus();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            _events.ClearSelected();
            e.Handled = true;
        }
    }

    private void OnBatchReady(object? sender, IReadOnlyList<TelemetryReading> batch)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(() => OnBatchReady(sender, batch));
            return;
        }

        foreach (var reading in batch)
            _events.Items.Insert(0, $"{reading.Timestamp:T} | {reading.SensorId} | {reading.Value:F2} {reading.Unit}");
        while (_events.Items.Count > 1_000) _events.Items.RemoveAt(_events.Items.Count - 1);
        _status.Text = $"Получено событий: {batch.Count} · {DateTime.Now:T}";
        SetState(ViewState.Data);
    }

    private void SetState(ViewState state)
    {
        _emptyState.Visible = state is ViewState.Empty or ViewState.Error;
        _events.Visible = state == ViewState.Data;
        _status.Text = state switch
        {
            ViewState.Empty => "Состояние: ожидание данных",
            ViewState.Error => "Состояние: ошибка загрузки",
            _ => _status.Text
        };
        _status.ForeColor = state == ViewState.Error ? ThemeTokens.Danger : ThemeTokens.TextMuted;
    }

    private enum ViewState
    {
        Loading,
        Empty,
        Error,
        Data
    }
}

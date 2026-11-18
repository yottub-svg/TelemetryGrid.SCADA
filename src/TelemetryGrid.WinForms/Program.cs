using TelemetryGrid.Application;

namespace TelemetryGrid.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var stream = new TelemetryStream();
        Application.Run(new MonitorForm(stream));
    }
}

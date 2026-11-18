using TelemetryGrid.Application;
using FormsApplication = System.Windows.Forms.Application;

namespace TelemetryGrid.WinForms;

internal static class Program
{
    [STAThread]
    private static async Task Main()
    {
        ApplicationConfiguration.Initialize();
        await using var stream = new TelemetryStream();
        FormsApplication.Run(new MonitorForm(stream));
    }
}

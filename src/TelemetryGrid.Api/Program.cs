using System.Runtime.CompilerServices;
using System.Text.Json;
using TelemetryGrid.Domain;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
var app = builder.Build();

app.MapHealthChecks("/health");
app.MapGet("/telemetry/stream", async (HttpContext context, int? count, CancellationToken cancellationToken) =>
{
    var requestedCount = Math.Clamp(count ?? 100, 1, 200_000);
    context.Response.ContentType = "application/x-ndjson; charset=utf-8";
    await foreach (var reading in CreateReadings(requestedCount, cancellationToken))
    {
        await JsonSerializer.SerializeAsync(context.Response.Body, reading, cancellationToken: cancellationToken);
        await context.Response.Body.WriteAsync("\n"u8.ToArray(), cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }
});

app.Run();

static async IAsyncEnumerable<TelemetryReading> CreateReadings(
    int count,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    for (var index = 0; index < count; index++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new TelemetryReading($"S-{index % 64 + 1:00}", DateTimeOffset.UtcNow, 20 + index % 80, "°C", 100);
        if ((index + 1) % 256 == 0) await Task.Yield();
    }
}

public partial class Program;

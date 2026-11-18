using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using TelemetryGrid.Domain;

namespace TelemetryGrid.Client;

public sealed class TelemetryApiClient(HttpClient httpClient)
{
    public async IAsyncEnumerable<TelemetryReading> StreamAsync(
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"telemetry/stream?count={count}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-ndjson"));
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(body);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var reading = JsonSerializer.Deserialize<TelemetryReading>(line);
            if (reading is not null) yield return reading;
        }
    }
}

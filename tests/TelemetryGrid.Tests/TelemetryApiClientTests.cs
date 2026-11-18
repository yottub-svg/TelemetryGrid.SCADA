using System.Net;
using System.Net.Http;
using System.Text;
using TelemetryGrid.Client;
using Xunit;

namespace TelemetryGrid.Tests;

public sealed class TelemetryApiClientTests
{
    [Fact]
    public async Task StreamAsync_reads_each_ndjson_record_incrementally()
    {
        using var httpClient = new HttpClient(new NdjsonHandler())
        {
            BaseAddress = new Uri("https://telemetry.test/")
        };
        var client = new TelemetryApiClient(httpClient);

        var readings = new List<TelemetryGrid.Domain.TelemetryReading>();
        await foreach (var reading in client.StreamAsync(2)) readings.Add(reading);

        Assert.Equal(2, readings.Count);
        Assert.Equal("S-01", readings[0].SensorId);
        Assert.Equal("S-02", readings[1].SensorId);
    }

    private sealed class NdjsonHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            const string payload = "{\"SensorId\":\"S-01\",\"Timestamp\":\"2026-01-01T00:00:00+00:00\",\"Value\":21,\"Unit\":\"°C\",\"QualityCode\":100}\n{\"SensorId\":\"S-02\",\"Timestamp\":\"2026-01-01T00:00:01+00:00\",\"Value\":22,\"Unit\":\"°C\",\"QualityCode\":100}\n";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/x-ndjson")
            };
            return Task.FromResult(response);
        }
    }
}

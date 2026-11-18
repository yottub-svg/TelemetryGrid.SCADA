namespace TelemetryGrid.Domain;

public sealed record TelemetryReading(
    string SensorId,
    DateTimeOffset Timestamp,
    double Value,
    string Unit,
    int QualityCode)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(SensorId)
                           && !double.IsNaN(Value)
                           && !double.IsInfinity(Value)
                           && QualityCode is >= 0 and <= 100;
}

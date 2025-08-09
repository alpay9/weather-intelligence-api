namespace Domain.Entities;
public class Forecast
{
    public long Id { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public DateTime Timestamp { get; set; } // UTC
    public string Source { get; set; } = "open-meteo";
    public double TempC { get; set; }
    public double? FeelsLikeC { get; set; }
    public double? WindKph { get; set; }
    public double? GustKph { get; set; }
    public double? HumidityPct { get; set; }
    public double? PrecipMm { get; set; }
    public double? CloudPct { get; set; }
    public double? UvIndex { get; set; }
    public double? Aqi { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

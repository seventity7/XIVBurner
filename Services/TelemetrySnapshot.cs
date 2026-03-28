using System;

namespace XIVBurner.Services;

public sealed class TelemetrySnapshot
{
    public float GpuTemperature { get; set; }
    public float GpuUsage { get; set; }
    public float GpuCoreClockMhz { get; set; }
    public float GpuMemoryClockMhz { get; set; }
    public float GpuFanPercent { get; set; }
    public float GpuPowerWatts { get; set; }
    public float GpuVramUsedGb { get; set; }
    public float GpuVramTotalGb { get; set; }

    public float RamUsedGb { get; set; }
    public float RamTotalGb { get; set; }

    public int Ping { get; set; }
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;

    public TelemetrySnapshot Clone()
    {
        return new TelemetrySnapshot
        {
            GpuTemperature = this.GpuTemperature,
            GpuUsage = this.GpuUsage,
            GpuCoreClockMhz = this.GpuCoreClockMhz,
            GpuMemoryClockMhz = this.GpuMemoryClockMhz,
            GpuFanPercent = this.GpuFanPercent,
            GpuPowerWatts = this.GpuPowerWatts,
            GpuVramUsedGb = this.GpuVramUsedGb,
            GpuVramTotalGb = this.GpuVramTotalGb,
            RamUsedGb = this.RamUsedGb,
            RamTotalGb = this.RamTotalGb,
            Ping = this.Ping,
            LastUpdatedUtc = this.LastUpdatedUtc,
        };
    }
}
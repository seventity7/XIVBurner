using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;

namespace XIVBurner.Services;

public sealed class TelemetryService : IDisposable
{
    private readonly Computer computer;
    private readonly object sync = new();
    private readonly CancellationTokenSource cts = new();
    private readonly Task workerTask;
    private readonly TimeSpan refreshInterval = TimeSpan.FromMilliseconds(2000);

    private TelemetrySnapshot snapshot = new();

    public TelemetryService()
    {
        this.computer = new Computer
        {
            IsCpuEnabled = false,
            IsGpuEnabled = true,
            IsMemoryEnabled = false,
            IsMotherboardEnabled = false,
            IsControllerEnabled = false,
            IsStorageEnabled = false,
            IsNetworkEnabled = false,
        };

        this.computer.Open();
        this.workerTask = Task.Run(this.PollLoopAsync);
    }

    public TelemetrySnapshot GetSnapshot()
    {
        lock (this.sync)
        {
            return this.snapshot.Clone();
        }
    }

    private async Task PollLoopAsync()
    {
        while (!this.cts.IsCancellationRequested)
        {
            try
            {
                var next = new TelemetrySnapshot();
                this.ReadSystemMemory(next);
                this.ReadGpu(next);
                next.LastUpdatedUtc = DateTime.UtcNow;

                lock (this.sync)
                {
                    this.snapshot = next;
                }
            }
            catch
            {
            }

            try
            {
                await Task.Delay(this.refreshInterval, this.cts.Token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private void ReadSystemMemory(TelemetrySnapshot target)
    {
        var memStatus = new MEMORYSTATUSEX();
        memStatus.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();

        if (!GlobalMemoryStatusEx(ref memStatus))
            return;

        var total = memStatus.ullTotalPhys;
        var avail = memStatus.ullAvailPhys;
        var used = total - avail;

        target.RamTotalGb = (float)(total / 1024d / 1024d / 1024d);
        target.RamUsedGb = (float)(used / 1024d / 1024d / 1024d);
    }

    private void ReadGpu(TelemetrySnapshot target)
    {
        float bestTemp = 0;
        int bestTempScore = int.MinValue;

        float bestUsage = 0;
        int bestUsageScore = int.MinValue;

        float bestCoreClock = 0;
        int bestCoreClockScore = int.MinValue;

        float bestMemClock = 0;
        int bestMemClockScore = int.MinValue;

        float bestFan = 0;
        int bestFanScore = int.MinValue;

        float bestPower = 0;
        int bestPowerScore = int.MinValue;

        float bestVramUsed = 0;
        int bestVramUsedScore = int.MinValue;

        float bestVramTotal = 0;
        int bestVramTotalScore = int.MinValue;

        foreach (var hardware in this.computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.GpuAmd &&
                hardware.HardwareType != HardwareType.GpuNvidia &&
                hardware.HardwareType != HardwareType.GpuIntel)
            {
                continue;
            }

            foreach (var (hw, sensor) in this.EnumerateSensorsRecursive(hardware))
            {
                if (!sensor.Value.HasValue)
                    continue;

                var value = sensor.Value.Value;
                var sensorName = sensor.Name ?? string.Empty;
                var hardwareName = hw.Name ?? string.Empty;
                var identifierText = sensor.Identifier?.ToString() ?? string.Empty;
                var text = $"{hardwareName} {sensorName} {identifierText}";

                switch (sensor.SensorType)
                {
                    case SensorType.Temperature:
                    {
                        var score = this.ScoreGpuTemperatureSensor(text, sensorName);
                        if (score > bestTempScore)
                        {
                            bestTempScore = score;
                            bestTemp = value;
                        }

                        break;
                    }

                    case SensorType.Load:
                    {
                        var score = this.ScoreGpuLoadSensor(text, sensorName);
                        if (score > bestUsageScore)
                        {
                            bestUsageScore = score;
                            bestUsage = value;
                        }

                        break;
                    }

                    case SensorType.Clock:
                    {
                        var coreScore = this.ScoreGpuCoreClockSensor(text, sensorName);
                        if (coreScore > bestCoreClockScore)
                        {
                            bestCoreClockScore = coreScore;
                            bestCoreClock = value;
                        }
                        else if (coreScore == bestCoreClockScore && coreScore >= 0)
                        {
                            bestCoreClock = Math.Max(bestCoreClock, value);
                        }

                        var memScore = this.ScoreGpuMemoryClockSensor(text, sensorName);
                        if (memScore > bestMemClockScore)
                        {
                            bestMemClockScore = memScore;
                            bestMemClock = value;
                        }
                        else if (memScore == bestMemClockScore && memScore >= 0)
                        {
                            bestMemClock = Math.Max(bestMemClock, value);
                        }

                        break;
                    }

                    case SensorType.Fan:
                    case SensorType.Control:
                    {
                        var score = this.ScoreGpuFanSensor(text, sensorName);
                        if (score > bestFanScore)
                        {
                            bestFanScore = score;
                            bestFan = value;
                        }

                        break;
                    }

                    case SensorType.Power:
                    {
                        var score = this.ScoreGpuPowerSensor(text, sensorName);
                        if (score > bestPowerScore)
                        {
                            bestPowerScore = score;
                            bestPower = value;
                        }

                        break;
                    }

                    case SensorType.Data:
                    case SensorType.SmallData:
                    {
                        var usedScore = this.ScoreGpuVramUsedSensor(text, sensorName);
                        if (usedScore > bestVramUsedScore)
                        {
                            bestVramUsedScore = usedScore;
                            bestVramUsed = this.NormalizeDataToGb(value);
                        }

                        var totalScore = this.ScoreGpuVramTotalSensor(text, sensorName);
                        if (totalScore > bestVramTotalScore)
                        {
                            bestVramTotalScore = totalScore;
                            bestVramTotal = this.NormalizeDataToGb(value);
                        }

                        break;
                    }
                }
            }
        }

        target.GpuTemperature = bestTemp;
        target.GpuUsage = bestUsage;
        target.GpuCoreClockMhz = bestCoreClock;
        target.GpuMemoryClockMhz = bestMemClock;
        target.GpuFanPercent = bestFan;
        target.GpuPowerWatts = bestPower;
        target.GpuVramUsedGb = bestVramUsed;
        target.GpuVramTotalGb = bestVramTotal;
    }

    private System.Collections.Generic.IEnumerable<(IHardware Hardware, ISensor Sensor)> EnumerateSensorsRecursive(IHardware hardware)
    {
        hardware.Update();

        foreach (var sensor in hardware.Sensors)
            yield return (hardware, sensor);

        foreach (var subHardware in hardware.SubHardware)
        {
            foreach (var pair in this.EnumerateSensorsRecursive(subHardware))
                yield return pair;
        }
    }

    private int ScoreGpuTemperatureSensor(string text, string sensorName)
    {
        var score = 0;

        if (Contains(text, "gpu core"))
            score += 140;
        else if (EqualsIgnoreCase(sensorName, "Core"))
            score += 130;
        else if (Contains(text, "gpu"))
            score += 100;

        if (Contains(text, "hot spot") || Contains(text, "hotspot") || Contains(text, "junction") || Contains(text, "memory"))
            score -= 300;

        return score;
    }

    private int ScoreGpuLoadSensor(string text, string sensorName)
    {
        var score = 0;

        if (Contains(text, "d3d 3d"))
            score += 140;
        else if (EqualsIgnoreCase(sensorName, "GPU Core"))
            score += 130;
        else if (EqualsIgnoreCase(sensorName, "Core"))
            score += 120;
        else if (EqualsIgnoreCase(sensorName, "GPU"))
            score += 118;
        else if (Contains(text, "gpu core"))
            score += 115;
        else if (Contains(text, "3d"))
            score += 100;
        else if (Contains(text, "d3d"))
            score += 90;
        else if (Contains(text, "gpu"))
            score += 80;

        if (Contains(text, "video encode") || Contains(text, "video decode") || Contains(text, "copy") || Contains(text, "memory"))
            score -= 250;

        return score;
    }

    private int ScoreGpuCoreClockSensor(string text, string sensorName)
    {
        var score = 0;

        if (Contains(text, "gpu core"))
            score += 140;
        else if (EqualsIgnoreCase(sensorName, "Core"))
            score += 130;
        else if (Contains(text, "graphics"))
            score += 120;

        if (Contains(text, "memory"))
            score -= 250;

        return score;
    }

    private int ScoreGpuMemoryClockSensor(string text, string sensorName)
    {
        var score = 0;

        if (Contains(text, "memory"))
            score += 140;
        else if (Contains(text, "vram"))
            score += 130;
        else
            score -= 20;

        return score;
    }

    private int ScoreGpuFanSensor(string text, string sensorName)
    {
        var score = 0;

        if (Contains(text, "gpu") && Contains(text, "fan"))
            score += 140;
        else if (Contains(text, "fan"))
            score += 130;
        else if (Contains(text, "control"))
            score += 100;

        return score;
    }

    private int ScoreGpuPowerSensor(string text, string sensorName)
    {
        var score = 0;

        if (Contains(text, "board"))
            score += 140;
        else if (Contains(text, "total"))
            score += 135;
        else if (Contains(text, "gpu"))
            score += 120;
        else if (Contains(text, "core"))
            score += 80;

        return score;
    }

    private int ScoreGpuVramUsedSensor(string text, string sensorName)
    {
        var score = 0;

        if (Contains(text, "dedicated memory used"))
            score += 140;
        else if (Contains(text, "gpu memory used"))
            score += 135;
        else if (Contains(text, "memory used"))
            score += 110;

        return score;
    }

    private int ScoreGpuVramTotalSensor(string text, string sensorName)
    {
        var score = 0;

        if (Contains(text, "dedicated memory total"))
            score += 140;
        else if (Contains(text, "gpu memory total"))
            score += 135;
        else if (Contains(text, "memory total"))
            score += 110;

        return score;
    }

    private float NormalizeDataToGb(float value)
    {
        if (value <= 0)
            return 0;

        if (value > 1024f * 1024f * 1024f)
            return value / 1024f / 1024f / 1024f;

        if (value > 64f)
            return value / 1024f;

        return value;
    }

    private static bool Contains(string source, string value)
        => source.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static bool EqualsIgnoreCase(string a, string b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    public void Dispose()
    {
        this.cts.Cancel();

        try
        {
            this.workerTask.Wait(1000);
        }
        catch
        {
        }

        this.computer.Close();
        this.cts.Dispose();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
}
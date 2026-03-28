using System;
using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace XIVBurner;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool OverlayVisible { get; set; } = true;
    public bool LockOverlay { get; set; } = false;

    public bool ShowGpu { get; set; } = true;
    public bool ShowGpuTemperature { get; set; } = true;
    public bool ShowGpuUsage { get; set; } = true;
    public bool ShowGpuCoreClock { get; set; } = true;
    public bool ShowGpuMemoryClock { get; set; } = true;
    public bool ShowGpuFan { get; set; } = true;
    public bool ShowGpuPower { get; set; } = true;
    public bool ShowVram { get; set; } = true;

    public bool ShowRam { get; set; } = true;
    public bool ShowFps { get; set; } = true;
    public bool ShowPing { get; set; } = true;

    public float GlobalTextScale { get; set; } = 1.0f;
    public int OrganizationLayout { get; set; } = 0; // 0 = Vertical, 1 = Inline, 2 = Compact
    public string GpuSensorLabel { get; set; } = "GPU";

    public Vector4 BackgroundColor { get; set; } = new(0f, 0f, 0f, 0.72f);
    public Vector4 SeparatorColor { get; set; } = new(1f, 1f, 1f, 0.14f);

    public Vector4 GpuLabelColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 RamLabelColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 FpsLabelColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 PingLabelColor { get; set; } = new(1f, 1f, 1f, 1f);

    public bool GpuLabelBold { get; set; } = false;
    public bool RamLabelBold { get; set; } = false;
    public bool FpsLabelBold { get; set; } = false;
    public bool PingLabelBold { get; set; } = false;

    public Vector4 GpuTemperatureColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 GpuUsageColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 GpuCoreClockColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 GpuMemoryClockColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 GpuFanColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 GpuPowerColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 VramColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 RamColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 FpsColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 PingColor { get; set; } = new(1f, 1f, 1f, 1f);

    public bool GpuTemperatureBold { get; set; } = false;
    public bool GpuUsageBold { get; set; } = false;
    public bool GpuCoreClockBold { get; set; } = false;
    public bool GpuMemoryClockBold { get; set; } = false;
    public bool GpuFanBold { get; set; } = false;
    public bool GpuPowerBold { get; set; } = false;
    public bool VramBold { get; set; } = false;
    public bool RamBold { get; set; } = false;
    public bool FpsBold { get; set; } = false;
    public bool PingBold { get; set; } = false;

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(IDalamudPluginInterface pi)
    {
        this.pluginInterface = pi;
    }

    public void Save()
    {
        this.pluginInterface?.SavePluginConfig(this);
    }
}
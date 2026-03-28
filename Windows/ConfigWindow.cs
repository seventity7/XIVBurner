using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace XIVBurner.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly Func<bool> getOverlayVisible;
    private readonly Action<bool> setOverlayVisible;

    private const ImGuiColorEditFlags SwatchFlags =
        ImGuiColorEditFlags.NoInputs |
        ImGuiColorEditFlags.NoLabel |
        ImGuiColorEditFlags.AlphaPreviewHalf |
        ImGuiColorEditFlags.AlphaBar;

    private const ImGuiTableFlags CompactTableFlags =
        ImGuiTableFlags.BordersInnerV |
        ImGuiTableFlags.SizingFixedFit |
        ImGuiTableFlags.NoHostExtendX;

    public ConfigWindow(
        Configuration configuration,
        Func<bool> getOverlayVisible,
        Action<bool> setOverlayVisible)
        : base("XIVBurner Settings###XIVBurnerConfig")
    {
        this.configuration = configuration;
        this.getOverlayVisible = getOverlayVisible;
        this.setOverlayVisible = setOverlayVisible;

        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(620, 700),
            MaximumSize = new Vector2(1000, 1400),
        };
        this.Size = new Vector2(680, 760);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(6, 4));

        ImGui.BeginChild("##SettingsScrollRoot", new Vector2(0, 0), false, ImGuiWindowFlags.AlwaysVerticalScrollbar);

        this.DrawTopRow();
        ImGui.Spacing();

        if (ImGui.BeginTable("SettingsMainLayout", 2, ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Left", ImGuiTableColumnFlags.WidthFixed, 250f);
            ImGui.TableSetupColumn("Right", ImGuiTableColumnFlags.WidthFixed, 320f);

            ImGui.TableNextColumn();
            this.DrawLeftColumn();

            ImGui.TableNextColumn();
            this.DrawRightColumn();

            ImGui.EndTable();
        }

        ImGui.EndChild();
        ImGui.PopStyleVar();
    }

    private void DrawTopRow()
    {
        if (ImGui.BeginTable("TopRow", 3, ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Behavior", ImGuiTableColumnFlags.WidthFixed, 150f);
            ImGui.TableSetupColumn("GpuMetrics", ImGuiTableColumnFlags.WidthFixed, 185f);
            ImGui.TableSetupColumn("OverlayToggle", ImGuiTableColumnFlags.WidthFixed, 180f);

            ImGui.TableNextColumn();
            this.DrawBehaviorBlock();

            ImGui.TableNextColumn();
            this.DrawGpuToggleBlock();

            ImGui.TableNextColumn();
            this.DrawOverlayToggleBlock();

            ImGui.EndTable();
        }
    }

    private void DrawLeftColumn()
    {
        this.DrawOrganizationBlock();
        ImGui.Spacing();
        this.DrawSectionDivider();
        this.DrawGeneralColorsBlock();
        ImGui.Spacing();
        this.DrawSectionDivider();
        this.DrawSectionLabelsBlock();
    }

    private void DrawRightColumn()
    {
        this.DrawGpuMetricsBlock();
        ImGui.Spacing();
        this.DrawSectionDivider();
        this.DrawOtherMetricsBlock();
    }

    private void DrawBehaviorBlock()
    {
        ImGui.TextUnformatted("Behavior");
        ImGui.Spacing();

        var lockOverlay = this.configuration.LockOverlay;
        if (ImGui.Checkbox("Lock Overlay", ref lockOverlay))
        {
            this.configuration.LockOverlay = lockOverlay;
            this.configuration.Save();
        }
    }

    private void DrawGpuToggleBlock()
    {
        ImGui.TextUnformatted("GPU Metrics");
        ImGui.Spacing();

        var showGpu = this.configuration.ShowGpu;
        if (ImGui.Checkbox("Show GPU Section", ref showGpu))
        {
            this.configuration.ShowGpu = showGpu;
            this.configuration.Save();
        }
    }

    private void DrawOverlayToggleBlock()
    {
        ImGui.TextUnformatted("Overlay Toggle");
        ImGui.Spacing();

        bool overlayVisible = this.getOverlayVisible();

        var show = overlayVisible;
        if (ImGui.Checkbox("Show", ref show) && show)
            this.setOverlayVisible(true);

        ImGui.SameLine();

        var hide = !overlayVisible;
        if (ImGui.Checkbox("Hide", ref hide) && hide)
            this.setOverlayVisible(false);
    }

    private void DrawOrganizationBlock()
    {
        ImGui.TextUnformatted("Organization");
        ImGui.Spacing();

        float globalTextScale = this.configuration.GlobalTextScale;
        ImGui.SetNextItemWidth(110f);
        if (ImGui.InputFloat("##GlobalTextScale", ref globalTextScale, 0f, 0f, "%.2f"))
        {
            globalTextScale = Math.Clamp(globalTextScale, 0.70f, 2.50f);
            this.configuration.GlobalTextScale = globalTextScale;
            this.configuration.Save();
        }

        ImGui.SameLine();
        ImGui.TextUnformatted("Global Text");

        string gpuSensorLabel = this.configuration.GpuSensorLabel ?? "GPU";
        ImGui.SetNextItemWidth(110f);
        if (ImGui.InputText("##GpuSensorLabel", ref gpuSensorLabel, 32))
        {
            gpuSensorLabel = gpuSensorLabel.Trim();
            this.configuration.GpuSensorLabel = string.IsNullOrWhiteSpace(gpuSensorLabel) ? "GPU" : gpuSensorLabel;
            this.configuration.Save();
        }

        ImGui.SameLine();
        ImGui.TextUnformatted("GPU Sensor");

        var layout = this.configuration.OrganizationLayout;
        var layoutOptions = new[] { "Vertical", "Inline", "Compact" };
        ImGui.SetNextItemWidth(120f);
        if (ImGui.Combo("##LayoutStyle", ref layout, layoutOptions, layoutOptions.Length))
        {
            this.configuration.OrganizationLayout = layout;
            this.configuration.Save();
        }

        ImGui.SameLine();
        ImGui.TextUnformatted("Layout Style");
    }

    private void DrawGeneralColorsBlock()
    {
        ImGui.TextUnformatted("General Colors");
        ImGui.Spacing();

        if (ImGui.BeginTable("GeneralColorsTable", 2, ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Setting", ImGuiTableColumnFlags.WidthFixed, 120f);
            ImGui.TableSetupColumn("Color", ImGuiTableColumnFlags.WidthFixed, 44f);
            ImGui.TableHeadersRow();

            this.DrawColorOnlyRow("Background", this.configuration.BackgroundColor, value =>
            {
                this.configuration.BackgroundColor = value;
                this.configuration.Save();
            });

            this.DrawColorOnlyRow("Separator", this.configuration.SeparatorColor, value =>
            {
                this.configuration.SeparatorColor = value;
                this.configuration.Save();
            });

            ImGui.EndTable();
        }
    }

    private void DrawSectionLabelsBlock()
    {
        ImGui.TextUnformatted("Section Labels");
        ImGui.Spacing();

        if (ImGui.BeginTable("SectionLabelsTable", 3, CompactTableFlags))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 115f);
            ImGui.TableSetupColumn("Color", ImGuiTableColumnFlags.WidthFixed, 34f);
            ImGui.TableSetupColumn("Bold", ImGuiTableColumnFlags.WidthFixed, 34f);
            ImGui.TableHeadersRow();

            this.DrawLabelStyleRow("GPU", this.configuration.GpuLabelColor, value =>
            {
                this.configuration.GpuLabelColor = value;
                this.configuration.Save();
            }, this.configuration.GpuLabelBold, value =>
            {
                this.configuration.GpuLabelBold = value;
                this.configuration.Save();
            });

            this.DrawLabelStyleRow("RAM", this.configuration.RamLabelColor, value =>
            {
                this.configuration.RamLabelColor = value;
                this.configuration.Save();
            }, this.configuration.RamLabelBold, value =>
            {
                this.configuration.RamLabelBold = value;
                this.configuration.Save();
            });

            this.DrawLabelStyleRow("FPS", this.configuration.FpsLabelColor, value =>
            {
                this.configuration.FpsLabelColor = value;
                this.configuration.Save();
            }, this.configuration.FpsLabelBold, value =>
            {
                this.configuration.FpsLabelBold = value;
                this.configuration.Save();
            });

            this.DrawLabelStyleRow("Ping", this.configuration.PingLabelColor, value =>
            {
                this.configuration.PingLabelColor = value;
                this.configuration.Save();
            }, this.configuration.PingLabelBold, value =>
            {
                this.configuration.PingLabelBold = value;
                this.configuration.Save();
            });

            ImGui.EndTable();
        }
    }

    private void DrawGpuMetricsBlock()
    {
        ImGui.TextUnformatted("GPU Metrics");
        ImGui.Spacing();

        if (ImGui.BeginTable("GpuMetricsTable", 4, CompactTableFlags))
        {
            ImGui.TableSetupColumn("Metric", ImGuiTableColumnFlags.WidthFixed, 140f);
            ImGui.TableSetupColumn("Show", ImGuiTableColumnFlags.WidthFixed, 46f);
            ImGui.TableSetupColumn("Color", ImGuiTableColumnFlags.WidthFixed, 44f);
            ImGui.TableSetupColumn("Bold", ImGuiTableColumnFlags.WidthFixed, 46f);
            ImGui.TableHeadersRow();

            this.DrawMetricStyleRow("GPU Temp", this.configuration.ShowGpuTemperature, value =>
            {
                this.configuration.ShowGpuTemperature = value;
                this.configuration.Save();
            }, this.configuration.GpuTemperatureColor, value =>
            {
                this.configuration.GpuTemperatureColor = value;
                this.configuration.Save();
            }, this.configuration.GpuTemperatureBold, value =>
            {
                this.configuration.GpuTemperatureBold = value;
                this.configuration.Save();
            });

            this.DrawMetricStyleRow("GPU Usage", this.configuration.ShowGpuUsage, value =>
            {
                this.configuration.ShowGpuUsage = value;
                this.configuration.Save();
            }, this.configuration.GpuUsageColor, value =>
            {
                this.configuration.GpuUsageColor = value;
                this.configuration.Save();
            }, this.configuration.GpuUsageBold, value =>
            {
                this.configuration.GpuUsageBold = value;
                this.configuration.Save();
            });

            this.DrawMetricStyleRow("GPU Clock", this.configuration.ShowGpuCoreClock, value =>
            {
                this.configuration.ShowGpuCoreClock = value;
                this.configuration.Save();
            }, this.configuration.GpuCoreClockColor, value =>
            {
                this.configuration.GpuCoreClockColor = value;
                this.configuration.Save();
            }, this.configuration.GpuCoreClockBold, value =>
            {
                this.configuration.GpuCoreClockBold = value;
                this.configuration.Save();
            });

            this.DrawMetricStyleRow("GPU Mem", this.configuration.ShowGpuMemoryClock, value =>
            {
                this.configuration.ShowGpuMemoryClock = value;
                this.configuration.Save();
            }, this.configuration.GpuMemoryClockColor, value =>
            {
                this.configuration.GpuMemoryClockColor = value;
                this.configuration.Save();
            }, this.configuration.GpuMemoryClockBold, value =>
            {
                this.configuration.GpuMemoryClockBold = value;
                this.configuration.Save();
            });

            this.DrawMetricStyleRow("Fan Speed", this.configuration.ShowGpuFan, value =>
            {
                this.configuration.ShowGpuFan = value;
                this.configuration.Save();
            }, this.configuration.GpuFanColor, value =>
            {
                this.configuration.GpuFanColor = value;
                this.configuration.Save();
            }, this.configuration.GpuFanBold, value =>
            {
                this.configuration.GpuFanBold = value;
                this.configuration.Save();
            });

            this.DrawMetricStyleRow("GPU Power", this.configuration.ShowGpuPower, value =>
            {
                this.configuration.ShowGpuPower = value;
                this.configuration.Save();
            }, this.configuration.GpuPowerColor, value =>
            {
                this.configuration.GpuPowerColor = value;
                this.configuration.Save();
            }, this.configuration.GpuPowerBold, value =>
            {
                this.configuration.GpuPowerBold = value;
                this.configuration.Save();
            });

            this.DrawMetricStyleRow("VRAM", this.configuration.ShowVram, value =>
            {
                this.configuration.ShowVram = value;
                this.configuration.Save();
            }, this.configuration.VramColor, value =>
            {
                this.configuration.VramColor = value;
                this.configuration.Save();
            }, this.configuration.VramBold, value =>
            {
                this.configuration.VramBold = value;
                this.configuration.Save();
            });

            ImGui.EndTable();
        }
    }

    private void DrawOtherMetricsBlock()
    {
        ImGui.TextUnformatted("Other Metrics");
        ImGui.Spacing();

        if (ImGui.BeginTable("MiscMetricsTable", 4, CompactTableFlags))
        {
            ImGui.TableSetupColumn("Metric", ImGuiTableColumnFlags.WidthFixed, 140f);
            ImGui.TableSetupColumn("Show", ImGuiTableColumnFlags.WidthFixed, 46f);
            ImGui.TableSetupColumn("Color", ImGuiTableColumnFlags.WidthFixed, 44f);
            ImGui.TableSetupColumn("Bold", ImGuiTableColumnFlags.WidthFixed, 46f);
            ImGui.TableHeadersRow();

            this.DrawMetricStyleRow("RAM", this.configuration.ShowRam, value =>
            {
                this.configuration.ShowRam = value;
                this.configuration.Save();
            }, this.configuration.RamColor, value =>
            {
                this.configuration.RamColor = value;
                this.configuration.Save();
            }, this.configuration.RamBold, value =>
            {
                this.configuration.RamBold = value;
                this.configuration.Save();
            });

            this.DrawMetricStyleRow("FPS", this.configuration.ShowFps, value =>
            {
                this.configuration.ShowFps = value;
                this.configuration.Save();
            }, this.configuration.FpsColor, value =>
            {
                this.configuration.FpsColor = value;
                this.configuration.Save();
            }, this.configuration.FpsBold, value =>
            {
                this.configuration.FpsBold = value;
                this.configuration.Save();
            });

            this.DrawMetricStyleRow("Ping", this.configuration.ShowPing, value =>
            {
                this.configuration.ShowPing = value;
                this.configuration.Save();
            }, this.configuration.PingColor, value =>
            {
                this.configuration.PingColor = value;
                this.configuration.Save();
            }, this.configuration.PingBold, value =>
            {
                this.configuration.PingBold = value;
                this.configuration.Save();
            });

            ImGui.EndTable();
        }
    }

    private void DrawSectionDivider()
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }

    private void DrawColorOnlyRow(string label, Vector4 color, Action<Vector4> onChanged)
    {
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted(label);

        ImGui.TableSetColumnIndex(1);
        var localColor = color;
        ImGui.PushID(label);
        if (ImGui.ColorEdit4("##color", ref localColor, SwatchFlags))
            onChanged(localColor);
        ImGui.PopID();
    }

    private void DrawLabelStyleRow(string label, Vector4 color, Action<Vector4> onColorChanged, bool bold, Action<bool> onBoldChanged)
    {
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted(label);

        ImGui.TableSetColumnIndex(1);
        var localColor = color;
        ImGui.PushID($"{label}_label_color");
        if (ImGui.ColorEdit4("##color", ref localColor, SwatchFlags))
            onColorChanged(localColor);
        ImGui.PopID();

        ImGui.TableSetColumnIndex(2);
        var localBold = bold;
        ImGui.PushID($"{label}_label_bold");
        if (ImGui.Checkbox("##bold", ref localBold))
            onBoldChanged(localBold);
        ImGui.PopID();
    }

    private void DrawMetricStyleRow(
        string label,
        bool visible,
        Action<bool> onVisibleChanged,
        Vector4 color,
        Action<Vector4> onColorChanged,
        bool bold,
        Action<bool> onBoldChanged)
    {
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted(label);

        ImGui.TableSetColumnIndex(1);
        var localVisible = visible;
        ImGui.PushID($"{label}_visible");
        if (ImGui.Checkbox("##visible", ref localVisible))
            onVisibleChanged(localVisible);
        ImGui.PopID();

        ImGui.TableSetColumnIndex(2);
        var localColor = color;
        ImGui.PushID($"{label}_color");
        if (ImGui.ColorEdit4("##color", ref localColor, SwatchFlags))
            onColorChanged(localColor);
        ImGui.PopID();

        ImGui.TableSetColumnIndex(3);
        var localBold = bold;
        ImGui.PushID($"{label}_bold");
        if (ImGui.Checkbox("##bold", ref localBold))
            onBoldChanged(localBold);
        ImGui.PopID();
    }
}
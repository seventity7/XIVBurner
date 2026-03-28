using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using XIVBurner.Services;

namespace XIVBurner.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly TelemetryService telemetry;

    private const float PaddingX = 10.0f;
    private const float NormalPaddingY = 8.0f;
    private const float InlinePaddingY = 4.0f;
    private const float LineGap = 4.0f;
    private const float CompactLineGap = 2.0f;
    private const float SeparatorGapBefore = 6.0f;
    private const float SeparatorGapAfter = 6.0f;
    private const float PanelRounding = 6.0f;
    private const float SegmentGap = 8.0f;
    private const float CompactSegmentGap = 4.0f;
    private const float CompactLabelGap = 5.0f;
    private const float SmallFpsSuffixScale = 0.5f;

    private static readonly Vector4 DefaultTextColor = new(1f, 1f, 1f, 1f);

    public MainWindow(Configuration configuration, TelemetryService telemetry)
        : base("###XIVBurnerMain")
    {
        this.configuration = configuration;
        this.telemetry = telemetry;

        this.Flags =
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse |
            ImGuiWindowFlags.NoBackground |
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoResize;

        this.RespectCloseHotkey = false;

        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(100, 50),
            MaximumSize = new Vector2(3000, 3000),
        };
    }

    public void Dispose()
    {
    }

    public override void PreDraw()
    {
        if (this.configuration.LockOverlay)
            this.Flags |= ImGuiWindowFlags.NoMove;
        else
            this.Flags &= ~ImGuiWindowFlags.NoMove;

        var rows = this.BuildRows();
        var totalSize = this.CalculateWindowSize(rows);

        ImGui.SetNextWindowSize(totalSize, ImGuiCond.Always);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);
    }

    public override void Draw()
    {
        var rows = this.BuildRows();

        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        var drawList = ImGui.GetWindowDrawList();
        var font = ImGui.GetFont();
        var baseScale = MathF.Max(0.70f, this.configuration.GlobalTextScale);
        var lineHeight = this.MeasureTextHeight("Ag", baseScale, 1.0f);
        var paddingY = this.GetVerticalPadding();
        var lineGap = this.GetLineGap();

        drawList.AddRectFilled(
            windowPos,
            windowPos + windowSize,
            ImGui.ColorConvertFloat4ToU32(this.configuration.BackgroundColor),
            PanelRounding);

        var separatorColor = ImGui.ColorConvertFloat4ToU32(this.configuration.SeparatorColor);

        float x = windowPos.X + PaddingX;
        float y = windowPos.Y + paddingY;
        float contentWidth = MathF.Max(0, windowSize.X - (PaddingX * 2.0f));

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];

            if (row.SeparatorBefore)
            {
                y += SeparatorGapBefore;
                drawList.AddLine(
                    new Vector2(x, y),
                    new Vector2(x + contentWidth, y),
                    separatorColor,
                    1.0f);
                y += SeparatorGapAfter;
            }

            float currentX = x + row.IndentX;
            for (int s = 0; s < row.Segments.Count; s++)
            {
                var segment = row.Segments[s];
                this.DrawTextSegment(drawList, font, baseScale, new Vector2(currentX, y), segment);
                currentX += this.MeasureTextWidth(segment.Text, baseScale, segment.ScaleMultiplier);

                if (s < row.Segments.Count - 1)
                    currentX += row.CustomGapAfter > 0 ? row.CustomGapAfter : this.GetSegmentGap();
            }

            y += MathF.Max(lineHeight, row.Height);

            if (i < rows.Count - 1)
                y += lineGap;
        }
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor(1);
        ImGui.PopStyleVar(3);
    }

    private List<DisplayRow> BuildRows()
    {
        var snapshot = this.telemetry.GetSnapshot();
        var fps = ImGui.GetIO().Framerate;

        return this.configuration.OrganizationLayout switch
        {
            1 => this.BuildInlineRows(snapshot, fps),
            2 => this.BuildCompactRows(snapshot, fps),
            _ => this.BuildVerticalRows(snapshot, fps),
        };
    }

    private List<DisplayRow> BuildVerticalRows(TelemetrySnapshot snapshot, float fps)
    {
        var rows = new List<DisplayRow>();
        var gpuRows = new List<DisplayRow>();
        var miscRows = new List<DisplayRow>();
        var gpuLabel = this.GetGpuLabel();
        var scale = MathF.Max(0.70f, this.configuration.GlobalTextScale);
        var gpuIndent = this.MeasureTextWidth(gpuLabel, scale, 1.0f) + this.GetSegmentGap();

        bool firstGpuRow = true;

        void AddGpuVerticalRow(string metricLabel, Vector4 metricColor, bool metricBold, string value)
        {
            if (firstGpuRow)
            {
                gpuRows.Add(this.MakeMetricRow(gpuLabel, this.configuration.GpuLabelColor, this.configuration.GpuLabelBold, metricLabel, metricColor, metricBold, value, 0f));
                firstGpuRow = false;
            }
            else
            {
                gpuRows.Add(this.MakeMetricRow(string.Empty, this.configuration.GpuLabelColor, this.configuration.GpuLabelBold, metricLabel, metricColor, metricBold, value, gpuIndent));
            }
        }

        if (this.configuration.ShowGpu)
        {
            if (this.configuration.ShowGpuTemperature)
                AddGpuVerticalRow("Temp:", this.configuration.GpuTemperatureColor, this.configuration.GpuTemperatureBold, this.FormatValue(snapshot.GpuTemperature, "°C", "{0:0}"));

            if (this.configuration.ShowGpuUsage)
                AddGpuVerticalRow("Usage:", this.configuration.GpuUsageColor, this.configuration.GpuUsageBold, this.FormatValue(snapshot.GpuUsage, "%", "{0:0}"));

            if (this.configuration.ShowGpuCoreClock)
                AddGpuVerticalRow("Clock:", this.configuration.GpuCoreClockColor, this.configuration.GpuCoreClockBold, this.FormatValue(snapshot.GpuCoreClockMhz, "MHz", "{0:0}"));

            if (this.configuration.ShowGpuMemoryClock)
                AddGpuVerticalRow("Mem:", this.configuration.GpuMemoryClockColor, this.configuration.GpuMemoryClockBold, this.FormatValue(snapshot.GpuMemoryClockMhz, "MHz", "{0:0}"));

            if (this.configuration.ShowGpuFan)
                AddGpuVerticalRow("Fan:", this.configuration.GpuFanColor, this.configuration.GpuFanBold, this.FormatValue(snapshot.GpuFanPercent, "%", "{0:0}"));

            if (this.configuration.ShowGpuPower)
                AddGpuVerticalRow("Power:", this.configuration.GpuPowerColor, this.configuration.GpuPowerBold, this.FormatValue(snapshot.GpuPowerWatts, "W", "{0:0}"));

            if (this.configuration.ShowVram)
            {
                var value = snapshot.GpuVramTotalGb > 0.0001f
                    ? $"{snapshot.GpuVramUsedGb:0} / {snapshot.GpuVramTotalGb:0} GB"
                    : "N/A";
                AddGpuVerticalRow("VRAM:", this.configuration.VramColor, this.configuration.VramBold, value);
            }
        }

        if (this.configuration.ShowRam)
        {
            var value = snapshot.RamTotalGb > 0.0001f
                ? $"{snapshot.RamUsedGb:0} / {snapshot.RamTotalGb:0} GB"
                : "N/A";
            miscRows.Add(this.MakeMetricRow("RAM", this.configuration.RamLabelColor, this.configuration.RamLabelBold, "", this.configuration.RamColor, this.configuration.RamBold, value, 0f));
        }

        if (this.configuration.ShowFps)
            miscRows.Add(this.MakeMetricRow("FPS", this.configuration.FpsLabelColor, this.configuration.FpsLabelBold, "", this.configuration.FpsColor, this.configuration.FpsBold, this.FormatValue(fps, string.Empty, "{0:0}"), 0f));

        if (this.configuration.ShowPing)
        {
            var value = snapshot.Ping > 0 ? $"{snapshot.Ping:0} ms" : "N/A";
            miscRows.Add(this.MakeMetricRow("Ping", this.configuration.PingLabelColor, this.configuration.PingLabelBold, "", this.configuration.PingColor, this.configuration.PingBold, value, 0f));
        }

        this.AddGroup(rows, gpuRows, false);
        this.AddGroup(rows, miscRows, rows.Count > 0);

        if (rows.Count == 0)
            rows.Add(this.MakeSingleSegmentRow("No metrics enabled", DefaultTextColor, false));

        return this.FinalizeRows(rows);
    }

    private List<DisplayRow> BuildInlineRows(TelemetrySnapshot snapshot, float fps)
    {
        var rows = new List<DisplayRow>();
        var row = new List<DisplaySegment>();
        var gpuLabel = this.GetGpuLabel();

        if (this.configuration.ShowGpu)
        {
            bool anyGpuMetric = false;

            void AddGpuValue(string value, Vector4 color, bool bold)
            {
                if (!anyGpuMetric)
                {
                    row.Add(new DisplaySegment(gpuLabel, this.configuration.GpuLabelColor, this.configuration.GpuLabelBold));
                    row.Add(new DisplaySegment(" ", DefaultTextColor, false));
                    anyGpuMetric = true;
                }
                else
                {
                    row.Add(new DisplaySegment("  ", DefaultTextColor, false));
                }

                row.Add(new DisplaySegment(value, color, bold));
            }

            if (this.configuration.ShowGpuTemperature)
                AddGpuValue(this.FormatValue(snapshot.GpuTemperature, "°C", "{0:0}"), this.configuration.GpuTemperatureColor, this.configuration.GpuTemperatureBold);

            if (this.configuration.ShowGpuUsage)
                AddGpuValue(this.FormatValue(snapshot.GpuUsage, "%", "{0:0}"), this.configuration.GpuUsageColor, this.configuration.GpuUsageBold);

            if (this.configuration.ShowGpuCoreClock)
                AddGpuValue(this.FormatValue(snapshot.GpuCoreClockMhz, "MHz", "{0:0}"), this.configuration.GpuCoreClockColor, this.configuration.GpuCoreClockBold);

            if (this.configuration.ShowGpuMemoryClock)
                AddGpuValue(this.FormatValue(snapshot.GpuMemoryClockMhz, "MHz", "{0:0}"), this.configuration.GpuMemoryClockColor, this.configuration.GpuMemoryClockBold);

            if (this.configuration.ShowGpuFan)
                AddGpuValue(this.FormatValue(snapshot.GpuFanPercent, "%", "{0:0}"), this.configuration.GpuFanColor, this.configuration.GpuFanBold);

            if (this.configuration.ShowGpuPower)
                AddGpuValue(this.FormatValue(snapshot.GpuPowerWatts, "W", "{0:0}"), this.configuration.GpuPowerColor, this.configuration.GpuPowerBold);

            if (this.configuration.ShowVram)
            {
                var value = snapshot.GpuVramTotalGb > 0.0001f
                    ? $"{snapshot.GpuVramUsedGb:0}/{snapshot.GpuVramTotalGb:0} GB"
                    : "N/A";
                AddGpuValue(value, this.configuration.VramColor, this.configuration.VramBold);
            }
        }

        void AddSectionSeparatorIfNeeded()
        {
            if (row.Count > 0)
                row.Add(new DisplaySegment("   ", DefaultTextColor, false));
        }

        bool hasMisc = false;

        if (this.configuration.ShowRam)
        {
            if (row.Count > 0)
                AddSectionSeparatorIfNeeded();

            row.Add(new DisplaySegment("RAM", this.configuration.RamLabelColor, this.configuration.RamLabelBold));
            row.Add(new DisplaySegment(" ", DefaultTextColor, false));

            var value = snapshot.RamTotalGb > 0.0001f
                ? $"{snapshot.RamUsedGb:0}/{snapshot.RamTotalGb:0} GB"
                : "N/A";
            row.Add(new DisplaySegment(value, this.configuration.RamColor, this.configuration.RamBold));
            hasMisc = true;
        }

        if (this.configuration.ShowFps)
        {
            if (row.Count > 0 && !hasMisc)
                AddSectionSeparatorIfNeeded();
            else if (hasMisc)
                row.Add(new DisplaySegment("  ", DefaultTextColor, false));

            row.Add(new DisplaySegment("FPS", this.configuration.FpsLabelColor, this.configuration.FpsLabelBold));
            row.Add(new DisplaySegment(" ", DefaultTextColor, false));
            row.Add(new DisplaySegment(this.FormatValue(fps, string.Empty, "{0:0}"), this.configuration.FpsColor, this.configuration.FpsBold));
            hasMisc = true;
        }

        if (this.configuration.ShowPing)
        {
            if (row.Count > 0 && !hasMisc)
                AddSectionSeparatorIfNeeded();
            else if (hasMisc)
                row.Add(new DisplaySegment("  ", DefaultTextColor, false));

            row.Add(new DisplaySegment("Ping", this.configuration.PingLabelColor, this.configuration.PingLabelBold));
            row.Add(new DisplaySegment(" ", DefaultTextColor, false));
            row.Add(new DisplaySegment(snapshot.Ping > 0 ? $"{snapshot.Ping:0} ms" : "N/A", this.configuration.PingColor, this.configuration.PingBold));
            hasMisc = true;
        }

        if (row.Count == 0)
            row.Add(new DisplaySegment("No metrics enabled", DefaultTextColor, false));

        rows.Add(new DisplayRow(row, false, 0f, 0f));
        return this.FinalizeRows(rows);
    }

    private List<DisplayRow> BuildCompactRows(TelemetrySnapshot snapshot, float fps)
    {
        var rows = new List<DisplayRow>();
        var gpuLabel = this.GetGpuLabel();
        var scale = MathF.Max(0.70f, this.configuration.GlobalTextScale);

        var baseLabels = new List<string> { gpuLabel };
        if (this.configuration.ShowFps) baseLabels.Add("FPS");
        if (this.configuration.ShowRam) baseLabels.Add("RAM");
        if (this.configuration.ShowPing) baseLabels.Add("Ping");

        float widestLabelWidth = 0f;
        foreach (var label in baseLabels)
            widestLabelWidth = MathF.Max(widestLabelWidth, this.MeasureTextWidth(label, scale, 1.0f));

        float valueColumnStart = widestLabelWidth + CompactLabelGap;

        if (this.configuration.ShowGpu)
        {
            var gpuSegments = new List<DisplaySegment>
            {
                new(gpuLabel, this.configuration.GpuLabelColor, this.configuration.GpuLabelBold),
                new(this.MakeSpacerForWidth(Math.Max(0f, valueColumnStart - this.MeasureTextWidth(gpuLabel, scale, 1.0f)), scale), DefaultTextColor, false),
            };

            if (this.configuration.ShowGpuTemperature)
                gpuSegments.Add(new DisplaySegment(this.FormatValue(snapshot.GpuTemperature, "°C", "{0:0}"), this.configuration.GpuTemperatureColor, this.configuration.GpuTemperatureBold));

            if (this.configuration.ShowGpuUsage)
            {
                gpuSegments.Add(new DisplaySegment(" ", DefaultTextColor, false));
                gpuSegments.Add(new DisplaySegment(this.FormatValue(snapshot.GpuUsage, "%", "{0:0}"), this.configuration.GpuUsageColor, this.configuration.GpuUsageBold));
            }

            if (this.configuration.ShowGpuCoreClock)
            {
                gpuSegments.Add(new DisplaySegment(" ", DefaultTextColor, false));
                gpuSegments.Add(new DisplaySegment(this.FormatValue(snapshot.GpuCoreClockMhz, "MHz", "{0:0}"), this.configuration.GpuCoreClockColor, this.configuration.GpuCoreClockBold));
            }

            if (this.configuration.ShowGpuMemoryClock)
            {
                gpuSegments.Add(new DisplaySegment(" ", DefaultTextColor, false));
                gpuSegments.Add(new DisplaySegment(this.FormatValue(snapshot.GpuMemoryClockMhz, "MHz", "{0:0}"), this.configuration.GpuMemoryClockColor, this.configuration.GpuMemoryClockBold));
            }

            if (this.configuration.ShowGpuFan)
            {
                gpuSegments.Add(new DisplaySegment(" ", DefaultTextColor, false));
                gpuSegments.Add(new DisplaySegment(this.FormatValue(snapshot.GpuFanPercent, "%", "{0:0}"), this.configuration.GpuFanColor, this.configuration.GpuFanBold));
            }

            if (this.configuration.ShowGpuPower)
            {
                gpuSegments.Add(new DisplaySegment(" ", DefaultTextColor, false));
                gpuSegments.Add(new DisplaySegment(this.FormatValue(snapshot.GpuPowerWatts, "W", "{0:0}"), this.configuration.GpuPowerColor, this.configuration.GpuPowerBold));
            }

            if (this.configuration.ShowVram)
            {
                var text = snapshot.GpuVramTotalGb > 0.0001f
                    ? $"{snapshot.GpuVramUsedGb:0}/{snapshot.GpuVramTotalGb:0} GB"
                    : "N/A";
                gpuSegments.Add(new DisplaySegment(" ", DefaultTextColor, false));
                gpuSegments.Add(new DisplaySegment(text, this.configuration.VramColor, this.configuration.VramBold));
            }

            if (gpuSegments.Count > 2)
                rows.Add(new DisplayRow(gpuSegments, false, 0f, 0f));
        }

        if (this.configuration.ShowFps)
        {
            var fpsValueText = this.FormatValue(fps, string.Empty, "{0:0}");

            rows.Add(new DisplayRow(new List<DisplaySegment>
            {
                new("FPS", this.configuration.FpsLabelColor, this.configuration.FpsLabelBold),
                new(this.MakeSpacerForWidth(Math.Max(74f, valueColumnStart - this.MeasureTextWidth("FPS", scale, 1.0f)), scale), DefaultTextColor, false),
                new(fpsValueText, this.configuration.FpsColor, this.configuration.FpsBold),
                new("FPS", this.configuration.FpsColor, this.configuration.FpsBold, SmallFpsSuffixScale),
            }, false, 0f, 2f));
        }

        if (this.configuration.ShowRam)
        {
            var text = snapshot.RamTotalGb > 0.0001f
                ? $"{snapshot.RamUsedGb:0}/{snapshot.RamTotalGb:0} GB"
                : "N/A";

            rows.Add(new DisplayRow(new List<DisplaySegment>
            {
                new("RAM", this.configuration.RamLabelColor, this.configuration.RamLabelBold),
                new(this.MakeSpacerForWidth(Math.Max(0f, valueColumnStart - this.MeasureTextWidth("RAM", scale, 1.0f)), scale), DefaultTextColor, false),
                new(text, this.configuration.RamColor, this.configuration.RamBold),
            }, false, 0f, 0f));
        }

        if (this.configuration.ShowPing)
        {
            var text = snapshot.Ping > 0 ? $"{snapshot.Ping:0} ms" : "N/A";
            rows.Add(new DisplayRow(new List<DisplaySegment>
            {
                new("Ping", this.configuration.PingLabelColor, this.configuration.PingLabelBold),
                new(this.MakeSpacerForWidth(Math.Max(0f, valueColumnStart - this.MeasureTextWidth("Ping", scale, 1.0f)), scale), DefaultTextColor, false),
                new(text, this.configuration.PingColor, this.configuration.PingBold),
            }, false, 0f, 0f));
        }

        if (rows.Count == 0)
            rows.Add(this.MakeSingleSegmentRow("No metrics enabled", DefaultTextColor, false));

        return this.FinalizeRows(rows);
    }

    private string MakeSpacerForWidth(float targetWidth, float scale)
    {
        if (targetWidth <= 0f)
            return string.Empty;

        float spaceWidth = Math.Max(1f, this.MeasureTextWidth(" ", scale, 1.0f));
        int spaces = Math.Max(1, (int)MathF.Round(targetWidth / spaceWidth));
        return new string(' ', spaces);
    }

    private List<DisplayRow> FinalizeRows(List<DisplayRow> rows)
    {
        float scale = MathF.Max(0.70f, this.configuration.GlobalTextScale);
        var finalized = new List<DisplayRow>(rows.Count);

        foreach (var row in rows)
        {
            float height = 0f;
            foreach (var segment in row.Segments)
                height = MathF.Max(height, this.MeasureTextHeight(segment.Text, scale, segment.ScaleMultiplier));

            finalized.Add(new DisplayRow(row.Segments, row.SeparatorBefore, height, row.CustomGapAfter, row.IndentX));
        }

        return finalized;
    }

    private void AddGroup(List<DisplayRow> target, List<DisplayRow> source, bool withSeparator)
    {
        if (source.Count == 0)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            var row = source[i];
            target.Add(new DisplayRow(row.Segments, withSeparator && i == 0, row.Height, row.CustomGapAfter, row.IndentX));
        }
    }

    private DisplayRow MakeSingleSegmentRow(string text, Vector4 color, bool bold)
    {
        return new DisplayRow(new List<DisplaySegment> { new(text, color, bold) }, false);
    }

    private DisplayRow MakeMetricRow(
        string groupLabel,
        Vector4 groupColor,
        bool groupBold,
        string metricLabel,
        Vector4 metricColor,
        bool metricBold,
        string value,
        float indentX)
    {
        var segments = new List<DisplaySegment>();

        if (!string.IsNullOrEmpty(groupLabel))
            segments.Add(new DisplaySegment(groupLabel, groupColor, groupBold));

        if (!string.IsNullOrEmpty(metricLabel))
            segments.Add(new DisplaySegment(metricLabel, metricColor, metricBold));

        segments.Add(new DisplaySegment(value, metricColor, metricBold));

        return new DisplayRow(segments, false, 0f, 0f, indentX);
    }

    private Vector2 CalculateWindowSize(List<DisplayRow> rows)
    {
        float scale = MathF.Max(0.70f, this.configuration.GlobalTextScale);
        float paddingY = this.GetVerticalPadding();
        float lineGap = this.GetLineGap();

        float maxWidth = 0.0f;
        float totalHeight = paddingY * 2.0f;

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            float rowWidth = row.IndentX;

            for (int j = 0; j < row.Segments.Count; j++)
            {
                rowWidth += this.MeasureTextWidth(row.Segments[j].Text, scale, row.Segments[j].ScaleMultiplier);

                if (j < row.Segments.Count - 1)
                    rowWidth += row.CustomGapAfter > 0 ? row.CustomGapAfter : this.GetSegmentGap();
            }

            maxWidth = MathF.Max(maxWidth, rowWidth);

            if (row.SeparatorBefore)
                totalHeight += SeparatorGapBefore + 1.0f + SeparatorGapAfter;

            totalHeight += MathF.Max(row.Height, this.MeasureTextHeight("Ag", scale, 1.0f));

            if (i < rows.Count - 1)
                totalHeight += lineGap;
        }

        return new Vector2(
            MathF.Ceiling(maxWidth + (PaddingX * 2.0f)),
            MathF.Ceiling(totalHeight));
    }

    private void DrawTextSegment(ImDrawListPtr drawList, ImFontPtr font, float baseScale, Vector2 pos, DisplaySegment segment)
    {
        float finalScale = baseScale * segment.ScaleMultiplier;
        float fontSize = ImGui.GetFontSize() * finalScale;
        var color = ImGui.ColorConvertFloat4ToU32(segment.Color);

        if (segment.Bold)
            drawList.AddText(font, fontSize, new Vector2(pos.X + 0.8f, pos.Y), color, segment.Text);

        drawList.AddText(font, fontSize, pos, color, segment.Text);
    }

    private float MeasureTextWidth(string text, float baseScale, float scaleMultiplier)
    {
        ImGui.SetWindowFontScale(baseScale * scaleMultiplier);
        var size = ImGui.CalcTextSize(text);
        ImGui.SetWindowFontScale(1.0f);
        return size.X;
    }

    private float MeasureTextHeight(string text, float baseScale, float scaleMultiplier)
    {
        ImGui.SetWindowFontScale(baseScale * scaleMultiplier);
        var size = ImGui.CalcTextSize(text);
        ImGui.SetWindowFontScale(1.0f);
        return size.Y;
    }

    private string FormatValue(float value, string unit, string format)
    {
        if (value <= 0.0001f)
            return "N/A";

        var formatted = string.Format(format, value);
        return string.IsNullOrWhiteSpace(unit) ? formatted : $"{formatted} {unit}";
    }

    private string GetGpuLabel()
    {
        var text = this.configuration.GpuSensorLabel?.Trim();
        return string.IsNullOrWhiteSpace(text) ? "GPU" : text;
    }

    private float GetVerticalPadding()
    {
        return this.configuration.OrganizationLayout == 1 ? InlinePaddingY : NormalPaddingY;
    }

    private float GetLineGap()
    {
        return this.configuration.OrganizationLayout == 2 ? CompactLineGap : LineGap;
    }

    private float GetSegmentGap()
    {
        return this.configuration.OrganizationLayout == 2 ? CompactSegmentGap : SegmentGap;
    }

    private readonly struct DisplayRow
    {
        public List<DisplaySegment> Segments { get; }
        public bool SeparatorBefore { get; }
        public float Height { get; }
        public float CustomGapAfter { get; }
        public float IndentX { get; }

        public DisplayRow(List<DisplaySegment> segments, bool separatorBefore, float height = 0f, float customGapAfter = 0f, float indentX = 0f)
        {
            this.Segments = segments;
            this.SeparatorBefore = separatorBefore;
            this.Height = height;
            this.CustomGapAfter = customGapAfter;
            this.IndentX = indentX;
        }
    }

    private readonly struct DisplaySegment
    {
        public string Text { get; }
        public Vector4 Color { get; }
        public bool Bold { get; }
        public float ScaleMultiplier { get; }

        public DisplaySegment(string text, Vector4 color, bool bold, float scaleMultiplier = 1.0f)
        {
            this.Text = text;
            this.Color = color;
            this.Bold = bold;
            this.ScaleMultiplier = scaleMultiplier;
        }
    }
}
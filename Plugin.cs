using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using XIVBurner.Services;
using XIVBurner.Windows;

namespace XIVBurner;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string MainCommandName = "/XIVBurner";
    private const string ConfigCommandName = "/XIVBurnerconfig";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("XIVBurner");

    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private TelemetryService Telemetry { get; init; }

    public Plugin()
    {
        this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.Configuration.Initialize(PluginInterface);

        this.Telemetry = new TelemetryService();
        this.MainWindow = new MainWindow(this.Configuration, this.Telemetry);
        this.ConfigWindow = new ConfigWindow(
            this.Configuration,
            () => this.MainWindow.IsOpen,
            value => this.MainWindow.IsOpen = value);

        this.WindowSystem.AddWindow(this.ConfigWindow);
        this.WindowSystem.AddWindow(this.MainWindow);

        CommandManager.AddHandler(MainCommandName, new CommandInfo(this.OnMainCommand)
        {
            HelpMessage = "Toggle XIVBurner overlay.",
        });

        CommandManager.AddHandler(ConfigCommandName, new CommandInfo(this.OnConfigCommand)
        {
            HelpMessage = "Open XIVBurner settings.",
        });

        PluginInterface.UiBuilder.Draw += this.DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi += this.ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += this.ToggleMainUi;

        this.MainWindow.IsOpen = this.Configuration.OverlayVisible;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= this.DrawUi;
        PluginInterface.UiBuilder.OpenConfigUi -= this.ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= this.ToggleMainUi;

        CommandManager.RemoveHandler(MainCommandName);
        CommandManager.RemoveHandler(ConfigCommandName);

        this.WindowSystem.RemoveAllWindows();

        this.ConfigWindow.Dispose();
        this.MainWindow.Dispose();
        this.Telemetry.Dispose();
    }

    private void DrawUi()
    {
        this.WindowSystem.Draw();
        this.Configuration.OverlayVisible = this.MainWindow.IsOpen;
    }

    private void OnMainCommand(string command, string args)
    {
        this.MainWindow.Toggle();
    }

    private void OnConfigCommand(string command, string args)
    {
        this.ConfigWindow.IsOpen = true;
    }

    private void ToggleConfigUi()
    {
        this.ConfigWindow.Toggle();
    }

    private void ToggleMainUi()
    {
        this.MainWindow.Toggle();
    }
}
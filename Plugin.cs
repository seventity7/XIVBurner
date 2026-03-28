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
    public string Name => "XIVBurner";

    private const string CommandName = "/xivburner";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;

    private readonly WindowSystem windowSystem = new("XIVBurner");

    private readonly Configuration configuration;
    private readonly TelemetryService telemetryService;
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;

    public Plugin()
    {
        this.configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.configuration.Initialize(PluginInterface);

        this.telemetryService = new TelemetryService();

        this.mainWindow = new MainWindow(this.configuration, this.telemetryService);
        this.configWindow = new ConfigWindow(
            this.configuration,
            () => this.mainWindow.IsOpen,
            value =>
            {
                this.mainWindow.IsOpen = value;
                this.configuration.OverlayVisible = value;
                this.configuration.Save();
            });

        this.windowSystem.AddWindow(this.mainWindow);
        this.windowSystem.AddWindow(this.configWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Open XIVBurner settings.",
        });

        PluginInterface.UiBuilder.Draw += this.DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += this.OpenMainUI;

        ClientState.Login += this.OnLogin;
        ClientState.Logout += this.OnLogout;

        this.mainWindow.IsOpen = true;
        this.configuration.OverlayVisible = true;
        this.configuration.Save();
    }

    public void Dispose()
    {
        ClientState.Login -= this.OnLogin;
        ClientState.Logout -= this.OnLogout;

        PluginInterface.UiBuilder.Draw -= this.DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUI;
        PluginInterface.UiBuilder.OpenMainUi -= this.OpenMainUI;

        CommandManager.RemoveHandler(CommandName);

        this.windowSystem.RemoveAllWindows();

        this.mainWindow.Dispose();
        this.configWindow.Dispose();
        this.telemetryService.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        this.configWindow.IsOpen = true;
    }

    private void DrawUI()
    {
        this.mainWindow.IsOpen = this.configuration.OverlayVisible;
        this.windowSystem.Draw();
    }

    private void OpenConfigUI()
    {
        this.configWindow.IsOpen = true;
    }

    private void OpenMainUI()
    {
        this.mainWindow.IsOpen = true;
        this.configuration.OverlayVisible = true;
        this.configuration.Save();
    }

    private void OnLogin()
    {
        this.mainWindow.IsOpen = true;
        this.configuration.OverlayVisible = true;
        this.configuration.Save();
    }

    private void OnLogout(int type, int code)
    {
        this.mainWindow.IsOpen = true;
    }
}
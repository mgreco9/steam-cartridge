using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using CartridgeDesigner.Avalonia.ViewModels;
using CartridgeDesigner.Avalonia.Views;

using SteamCartridge.App.ViewModels;
using SteamCartridge.App.Views;
using SteamCartridge.Core;
using SteamCartridge.Core.Workspace;

using SteamCartridgeLauncher;

namespace SteamCartridge.App;

public partial class App : Application
{
    private CartridgeReader _reader;
    private GameLauncher _launcher;
    private MainViewModel? _model;

    ///////////////////////////
    //// LAUNCH MANAGEMENT ////
    ///////////////////////////
    public App()
    {
        _reader = CartridgeReader.InitializeCartridgeReader();
        _launcher = new GameLauncher(_reader);
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _launcher.Activate();
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        base.OnFrameworkInitializationCompleted();
    }

    ////////////////////////////////
    //// WINDOW OPEN MANAGEMENT ////
    ////////////////////////////////
    public async void TrayIcon_Clicked(object? sender, EventArgs e)
    {
        await OpenWindow();
    }

    private async Task OpenWindow()
    {
        // 0 - Deactivate game launcher
        _launcher.Pause();

        // 1 - Initialize the config
        Window window = new MainWindow();
        window.Show();
        ConfigService config = await InitializeConfig(window);

        // 2 - Initialize workspace
        WorkspaceManager ws = WorkspaceManager.InitializeWorkspace(config);

        // 3 - Start main window
        _model = MainViewModel.Initialize(ws, _reader);
        MainView view = new MainView()
        {
            DataContext = _model
        };

        window.DataContext = new MainWindowModel
        {
            CurrentView = view
        };

        window.Closed += CloseWindow;
    }

    private void CloseWindow(object? sender, EventArgs e)
    {
        // 1 - Reactivate game launcher
        _launcher.Activate();

        // 2 - Dispose of the Main View model
        _model?.Dispose();
    }

    private async Task<ConfigService> InitializeConfig(Window window)
    {
        // 1 - Setup config if not present
        ConfigService config;
        if (!ConfigService.Exists())
        {
            config = new ConfigService();
            ConfigurationWindow setupWindow = new ConfigurationWindow();

            ConfigurationWindowModel vm = new ConfigurationWindowModel(config);
            setupWindow.DataContext = vm;

            // Show setup dialog before main app usage
            await setupWindow.ShowDialog(window);
        }

        // 2 - Otherwise, load the config from json
        else
            config = ConfigService.Load();

        // 3 - Return the config
        return config;
    }
}
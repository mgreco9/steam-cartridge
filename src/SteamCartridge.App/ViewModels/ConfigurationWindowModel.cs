// FirstLaunchSetupViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using System.Collections.ObjectModel;
using SteamCartridge.Core;
using ReactiveUI;
using System;

namespace SteamCartridge.App.ViewModels;

public partial class ConfigurationWindowModel : ReactiveObject
{
    private readonly ConfigService _configService;

    public string SteamClientID
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    public string SteamAPIKey
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    public string SteamGridDBAPIKey
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    public string SaveDirectory
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ConfigurationWindowModel(ConfigService configService)
    {
        _configService = configService;

        SteamClientID = "";
        SteamAPIKey = "";
        SteamGridDBAPIKey = "";
        SaveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public void Save_Click(Window? window)
    {
        _configService.SteamClientID = SteamClientID;
        _configService.SteamAPIKey = SteamAPIKey;
        _configService.SteamGridDBAPIKey = SteamGridDBAPIKey;
        _configService.SaveDirectory = SaveDirectory;

        _configService.Save();

        window?.Close();
    }

    public void Cancel_Click(Window? window)
    {
        window?.Close();
    }
}
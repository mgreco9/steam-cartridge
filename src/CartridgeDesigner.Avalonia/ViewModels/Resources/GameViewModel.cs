using Avalonia.Media;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using SteamCartridge.Core.Workspace;

public partial class GameViewModel : ObservableObject
{
    public GameData Game { get; }
    public bool IsIgnored => Game.PhysicalState == CartridgePhysicalState.IGNORED;
    public string IgnoreButton => IsIgnored ? "✔" : "✖";

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public GameViewModel(GameData game)
    {
        Game = game;
        IsSelected = false;
    }
}
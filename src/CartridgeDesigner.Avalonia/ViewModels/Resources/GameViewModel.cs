using System.ComponentModel;
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
        Game.PropertyChanged += OnGamePropertyChanged;
    }

    private void OnGamePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Bindings read IgnoreButton off this view model, not off GameData.
        // Forward the cache change so the button text updates without rebuilding the row.
        if (e.PropertyName is nameof(GameData.PhysicalState) or null)
        {
            OnPropertyChanged(nameof(IsIgnored));
            OnPropertyChanged(nameof(IgnoreButton));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using SteamCartridge.Core.Workspace;
using static SteamCartridge.Core.Workspace.GameData;

namespace CartridgeDesigner.Avalonia.ViewModels.LibraryViewModel;

public partial class LibraryFolderViewModel : ReactiveObject
{
    private WorkspaceManager _ws;

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public LibraryFolderViewModel(WorkspaceManager ws)
    {
        _ws = ws;
        _fullGameDictionary = [];
        foreach(GameData game in _ws.GetLibrary())
        {
            _fullGameDictionary.Add(game.GameID, new GameViewModel(game));
        }

        FilteredGameList = [.. _fullGameDictionary.Values];
        SelectedItem = [];

        DesignFilter =
        [
            new(CartridgeDesignState.DESIGNED),
            new(CartridgeDesignState.NOT_DESIGNED)
        ];

        foreach(FilterOption<CartridgeDesignState> filter in DesignFilter)
            filter.OnFilterUpdate += OnFilterUpdate;
        
        CartridgeFilter =
        [
            new(CartridgePhysicalState.ASSIGNED),
            new(CartridgePhysicalState.NOT_ASSIGNED),
            new(CartridgePhysicalState.IGNORED, false)
        ];

        foreach(FilterOption<CartridgePhysicalState> filter in CartridgeFilter)
            filter.OnFilterUpdate += OnFilterUpdate;

        OnFilterUpdate();
    }

    //////////////////////////////
    //// FILTERING MANAGEMENT ////
    //////////////////////////////
    public string? SearchText
    {
        get;
        set
        {
            if(field == value)
                return;

            field = value;
            OnFilterUpdate();
        }
    }

    public ObservableCollection<FilterOption<CartridgeDesignState>> DesignFilter {get;}
    public ObservableCollection<FilterOption<CartridgePhysicalState>> CartridgeFilter {get;}

    private void OnFilterUpdate()
    {
        // 1 - Retrieve the filters
        HashSet<CartridgeDesignState> enabledDesignFilter = [.. DesignFilter
                .Where(f => f.IsEnabled)
                .Select(f => f.Value)];

        HashSet<CartridgePhysicalState> enabledCartridgeFilter = [.. CartridgeFilter
                .Where(f => f.IsEnabled)
                .Select(f => f.Value)];

        // 2 - Filter the games
        FilteredGameList = [.. _fullGameDictionary.Values
                .Where(g => enabledDesignFilter.Contains(g.Game.DesignState))
                .Where(g => enabledCartridgeFilter.Contains(g.Game.PhysicalState))
                .Where(g => SearchText == null || g.Game.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .OrderBy(g => g.Game.Name)];
    }

    //////////////////////////////
    //// LIST ITEM MANAGEMENT ////
    //////////////////////////////
    public Dictionary<int, GameViewModel> _fullGameDictionary;

    public ObservableCollection<GameViewModel> FilteredGameList
    {
        get;
        private set =>this.RaiseAndSetIfChanged(ref field, value);
    }

    public List<GameViewModel> SelectedItem{ get; }

    public GameViewModel? LastSelectedItem { get; private set; }

    public void Refresh()
    {
        HashSet<CartridgeDesignState> enabledDesignFilter = [.. DesignFilter
                .Where(f => f.IsEnabled)
                .Select(f => f.Value)];

        HashSet<CartridgePhysicalState> enabledCartridgeFilter = [.. CartridgeFilter
                .Where(f => f.IsEnabled)
                .Select(f => f.Value)];

        foreach(GameData game in _ws.GetLibrary())
        {
            if(_fullGameDictionary.ContainsKey(game.GameID))
                continue;

            GameViewModel gameModel = new GameViewModel(game);
            _fullGameDictionary.Add(game.GameID, gameModel);

            if(!enabledDesignFilter.Contains(game.DesignState))
                continue;
                
            if(!enabledCartridgeFilter.Contains(game.PhysicalState))
                continue;

            if(SearchText != null && !game.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                continue;

            FilteredGameList.Add(gameModel);
        }

        FilteredGameList = [.. FilteredGameList.OrderBy(g => g.Game.Name)];
    }

    internal void SelectItem(GameViewModel item, KeyModifiers key)
    {
        // 1 - If none only select new item
        if (key == KeyModifiers.None)
        {
            LastSelectedItem?.IsSelected = false;
            SelectedItem.ForEach(g => g.IsSelected = false);

            LastSelectedItem = item;
            SelectedItemChanged?.Invoke(LastSelectedItem.Game);
            item.IsSelected = true;

            SelectedItem.Clear();
            SelectedItem.Add(item);
        }

        // 2 - If ctrl, add new selected item to list
        else if (key == KeyModifiers.Control)
        {
            LastSelectedItem = item;
            SelectedItemChanged?.Invoke(LastSelectedItem.Game);
            item.IsSelected = true;
            SelectedItem.Add(item);
        }

        // 3 - If shift, add whole select item range to list
        else if (key == KeyModifiers.Shift)
        {
            SelectedItem.ForEach(g => g.IsSelected = false);

            LastSelectedItem ??= item;
            int lastIndex = FilteredGameList.IndexOf(LastSelectedItem);
            int newIndex = FilteredGameList.IndexOf(item);
            int minIdx = Math.Min(lastIndex, newIndex);
            int maxIdx = Math.Max(lastIndex, newIndex);

            SelectedItem.Clear();
            for(int i = minIdx; i<=maxIdx; i++)
            {
                GameViewModel game = FilteredGameList[i];
                game.IsSelected = true;
                SelectedItem.Add(FilteredGameList[i]);
            }
        }
    }

    [RelayCommand]
    public void IgnoreGame(GameViewModel item)
    {
        if (item == null)
            return;

        GameManager manager = _ws.LaunchGameManager(item.Game);
        if (item.Game.PhysicalState != CartridgePhysicalState.IGNORED)
        {
            item.Game.PhysicalState = CartridgePhysicalState.IGNORED;
            manager.UpdateGame();

            bool showIgnore = CartridgeFilter
                .Where(f => f.Value == CartridgePhysicalState.IGNORED)
                .All(f => f.IsEnabled);

            if(!showIgnore)
                FilteredGameList.Remove(item);
        }
        else
        {
            item.Game.PhysicalState = CartridgePhysicalState.NOT_ASSIGNED;
            manager.UpdateGame();

            bool showNotIgnore = CartridgeFilter
                .Where(f => f.Value == CartridgePhysicalState.NOT_ASSIGNED)
                .All(f => f.IsEnabled);

            if(!showNotIgnore)
                FilteredGameList.Remove(item);
        }
    }

    public event Action<GameData?>? SelectedItemChanged;
}
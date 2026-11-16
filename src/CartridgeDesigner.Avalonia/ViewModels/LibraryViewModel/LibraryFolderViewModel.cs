using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Avalonia.Input;

using CommunityToolkit.Mvvm.Input;

using ReactiveUI;

using SteamCartridge.Core.Events;
using SteamCartridge.Core.Workspace;

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
        foreach (GameData game in _ws.GetLibrary())
        {
            _fullGameDictionary.Add(game.GameID, new GameViewModel(game));
        }

        // Further cache changes (sync, design state, assign, ignore) update this list.
        _ws.GameCached += OnGameCached;
        _ws.GameUpdated += OnGameUpdated;

        FilteredGameList = [.. _fullGameDictionary.Values];
        SelectedItem = [];

        DesignFilter =
        [
            new(CartridgeDesignState.DESIGNED),
            new(CartridgeDesignState.NOT_DESIGNED)
        ];

        foreach (FilterOption<CartridgeDesignState> filter in DesignFilter)
            filter.OnFilterUpdate += OnFilterUpdate;

        CartridgeFilter =
        [
            new(CartridgePhysicalState.ASSIGNED),
            new(CartridgePhysicalState.NOT_ASSIGNED),
            new(CartridgePhysicalState.IGNORED, false)
        ];

        foreach (FilterOption<CartridgePhysicalState> filter in CartridgeFilter)
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
            if (field == value)
                return;

            field = value;
            OnFilterUpdate();
        }
    }

    public ObservableCollection<FilterOption<CartridgeDesignState>> DesignFilter { get; }
    public ObservableCollection<FilterOption<CartridgePhysicalState>> CartridgeFilter { get; }

    private void OnFilterUpdate()
    {
        // Rebuild from the cache projection. Individual game edits use ReconcileGame instead.
        FilteredGameList = [.. _fullGameDictionary.Values
                .Where(g => MatchesFilter(g.Game))
                .OrderBy(g => g.Game.Name)];
    }

    private bool MatchesFilter(GameData game)
    {
        bool designOk = DesignFilter.Any(f => f.IsEnabled && f.Value == game.DesignState);
        bool cartridgeOk = CartridgeFilter.Any(f => f.IsEnabled && f.Value == game.PhysicalState);
        bool searchOk = SearchText == null || game.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        return designOk && cartridgeOk && searchOk;
    }

    //////////////////////////////
    //// LIST ITEM MANAGEMENT ////
    //////////////////////////////
    public Dictionary<int, GameViewModel> _fullGameDictionary;

    public ObservableCollection<GameViewModel> FilteredGameList
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public List<GameViewModel> SelectedItem { get; }

    public GameViewModel? LastSelectedItem { get; private set; }

    public void Refresh()
    {
        // Safety net after a library sync. GameCached already inserted new games;
        // this picks up anything that was written without an event.
        foreach (GameData game in _ws.GetLibrary())
            AddGameIfMissing(game);

        FilteredGameList = [.. FilteredGameList.OrderBy(g => g.Game.Name)];
    }

    private void OnGameCached(object? sender, GameData game)
    {
        UiThread.Run(() => AddGameIfMissing(game));
    }

    private void OnGameUpdated(object? sender, GameCacheChangeEventArgs e)
    {
        // Name, design state and physical state decide whether the row stays visible and where it sits.
        if (e.PropertyName is not (
            nameof(GameData.Name) or
            nameof(GameData.DesignState) or
            nameof(GameData.PhysicalState)))
            return;

        UiThread.Run(() => ReconcileGame(e.Game));
    }

    private void AddGameIfMissing(GameData game)
    {
        if (_fullGameDictionary.TryGetValue(game.GameID, out GameViewModel? existing))
        {
            if (!ReferenceEquals(existing.Game, game))
            {
                int staleIndex = FilteredGameList.IndexOf(existing);
                if (staleIndex >= 0)
                    FilteredGameList.RemoveAt(staleIndex);

                _fullGameDictionary.Remove(game.GameID);
            }
            else
            {
                ReconcileGame(game);
                return;
            }
        }

        GameViewModel gameModel = new GameViewModel(game);
        _fullGameDictionary.Add(game.GameID, gameModel);

        if (MatchesFilter(game))
            InsertFiltered(gameModel);
    }

    private void ReconcileGame(GameData game)
    {
        if (!_fullGameDictionary.TryGetValue(game.GameID, out GameViewModel? gameModel))
            return;

        if (!ReferenceEquals(gameModel.Game, game))
            return;

        bool visible = MatchesFilter(game);
        int index = FilteredGameList.IndexOf(gameModel);

        if (!visible)
        {
            if (index >= 0)
                FilteredGameList.RemoveAt(index);
            return;
        }

        if (index >= 0 && IsInNameOrder(index))
            return;

        if (index >= 0)
            FilteredGameList.RemoveAt(index);

        InsertFiltered(gameModel);
    }

    private bool IsInNameOrder(int index)
    {
        string name = FilteredGameList[index].Game.Name;
        bool afterPrevious = index == 0
            || string.Compare(FilteredGameList[index - 1].Game.Name, name, StringComparison.CurrentCulture) <= 0;
        bool beforeNext = index == FilteredGameList.Count - 1
            || string.Compare(name, FilteredGameList[index + 1].Game.Name, StringComparison.CurrentCulture) <= 0;
        return afterPrevious && beforeNext;
    }

    private void InsertFiltered(GameViewModel gameModel)
    {
        int insertAt = 0;
        while (insertAt < FilteredGameList.Count
            && string.Compare(FilteredGameList[insertAt].Game.Name, gameModel.Game.Name, StringComparison.CurrentCulture) < 0)
        {
            insertAt++;
        }

        FilteredGameList.Insert(insertAt, gameModel);
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
            for (int i = minIdx; i <= maxIdx; i++)
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

        // The list membership follows GameUpdated. This command only changes the cache.
        GameManager manager = _ws.LaunchGameManager(item.Game);
        item.Game.PhysicalState = item.Game.PhysicalState == CartridgePhysicalState.IGNORED
            ? CartridgePhysicalState.NOT_ASSIGNED
            : CartridgePhysicalState.IGNORED;
        manager.UpdateGame();
    }

    public void Dispose()
    {
        _ws.GameCached -= OnGameCached;
        _ws.GameUpdated -= OnGameUpdated;
    }

    public event Action<GameData?>? SelectedItemChanged;
}
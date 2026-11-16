
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CartridgeDesigner.Avalonia.ViewModels;

using CommunityToolkit.Mvvm.Input;

using ReactiveUI;

using SteamCartridge.Core.Events;
using SteamCartridge.Core.Workspace;

namespace CartridgeDesigner.Avalonia.ViewModels.ClassificationViewModel;

public partial class ClassificationMainViewModel : TabViewModel
{
    private WorkspaceManager _ws;

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public ClassificationMainViewModel(WorkspaceManager ws)
    {
        _ws = ws;
        _gameToUpdate = [];

        CategoryList = [];
        HashSet<int> existingGameId = [];
        foreach (Category category in _ws.CategoryList)
        {
            CategoryViewModel model = new CategoryViewModel(category);
            model.LoadGameList(ws);

            foreach (FranchiseViewModel franchise in model.FranchiseList)
            {
                foreach (GameViewModel game in franchise.GameList)
                {
                    existingGameId.Add(game.Game.GameID);
                }
            }
            CategoryList.Add(model);
        }

        CategoryList = [.. CategoryList.OrderBy(c => c.Category.Name)];

        List<GameViewModel> initialGames = [];
        foreach (GameData game in _ws.GetLibrary())
        {
            if (existingGameId.Contains(game.GameID))
                continue;

            if (game.PhysicalState == CartridgePhysicalState.IGNORED)
                continue;

            initialGames.Add(new GameViewModel(game));
        }

        GameList = new ObservableCollection<GameViewModel>(initialGames.OrderBy(g => g.Game.Name));

        // Cache mutations from this tab and from the others land here.
        _ws.CategoryAdded += OnCategoryAdded;
        _ws.CategoryRemoved += OnCategoryRemoved;
        _ws.CategoryGameChanged += OnCategoryGameChanged;
        _ws.GameCached += OnGameCached;
        _ws.GameUpdated += OnGameUpdated;
    }

    /////////////////////////
    //// GAME MANAGEMENT ////
    /////////////////////////
    public ObservableCollection<GameViewModel> GameList { get; }


    [RelayCommand]
    public void IgnoreGame(GameViewModel item)
    {
        if (item == null)
            return;

        // GameUpdated removes the row from the unassigned list and from the library.
        GameManager manager = _ws.LaunchGameManager(item.Game);
        if (item.Game.PhysicalState == CartridgePhysicalState.IGNORED)
            return;

        item.Game.PhysicalState = CartridgePhysicalState.IGNORED;
        manager.UpdateGame();
    }

    /////////////////////////////
    //// CATEGORY MANAGEMENT ////
    /////////////////////////////
    public ObservableCollection<CategoryViewModel> CategoryList { get; }
    public CategoryViewModel? SelectedCategory
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public void CreateCategory(Category category)
    {
        // 1 - Update the cache. OnCategoryAdded updates the list.
        _ws.AddCategory(category);

        // 2 - Push change
        _ = DebouncePushCategoryUpdate();
    }

    public void RemoveCategory(CategoryViewModel category)
    {
        // 1 - Copy first: each removal mutates the franchise lists through the event handler
        List<GameViewModel> games = category.FranchiseList.SelectMany(franchise => franchise.GameList).ToList();

        // 2 - Return every game to the cache as unclassified
        foreach (GameViewModel game in games)
        {
            _ws.RemoveGameFromCategory(category.Category, game.Game);
            _ = DebouncePushGameUpdate(game.Game);
        }

        // 3 - Remove the category itself
        _ws.RemoveCategory(category.Category);

        // 4 - Push change
        _ = DebouncePushCategoryUpdate();
    }

    public void AddGameToCategory(CategoryViewModel category, GameViewModel game, string franchise)
    {
        // 1 - Update the cache. OnCategoryGameChanged moves the row.
        _ws.AddGameToCategory(category.Category, game.Game, franchise);

        // 2 - Push updates
        _ = DebouncePushGameUpdate(game.Game);
        _ = DebouncePushCategoryUpdate();
    }

    [RelayCommand]
    public void RemoveGameFromCategory(GameViewModel game)
    {
        if (SelectedCategory == null)
            return;

        // 1 - Update the cache. OnCategoryGameChanged moves the row back.
        _ws.RemoveGameFromCategory(SelectedCategory.Category, game.Game);

        // 2 - Push updates
        _ = DebouncePushGameUpdate(game.Game);
        _ = DebouncePushCategoryUpdate();
    }

    private void OnCategoryAdded(object? sender, Category category)
    {
        UiThread.Run(() =>
        {
            if (CategoryList.Any(c => ReferenceEquals(c.Category, category)))
                return;

            CategoryViewModel model = new CategoryViewModel(category);
            int index = 0;
            while (index < CategoryList.Count
                && string.Compare(CategoryList[index].Category.Name, category.Name, StringComparison.CurrentCulture) < 0)
            {
                index++;
            }

            CategoryList.Insert(index, model);
        });
    }

    private void OnCategoryRemoved(object? sender, Category category)
    {
        UiThread.Run(() =>
        {
            CategoryViewModel? model = CategoryList.FirstOrDefault(c => ReferenceEquals(c.Category, category));
            if (model == null)
                return;

            // Games are normally moved by CategoryGameChanged first.
            // If the category still holds rows, return them so they do not disappear.
            foreach (FranchiseViewModel franchise in model.FranchiseList)
            {
                foreach (GameViewModel game in franchise.GameList)
                {
                    if (game.Game.PhysicalState == CartridgePhysicalState.IGNORED)
                        continue;

                    if (!GameList.Contains(game))
                        GameList.Add(game);
                }
            }

            CategoryList.Remove(model);
            if (ReferenceEquals(SelectedCategory, model))
                SelectedCategory = null;
        });
    }

    private void OnCategoryGameChanged(object? sender, CategoryGameChangeEventArgs e)
    {
        UiThread.Run(() =>
        {
            CategoryViewModel? category = CategoryList.FirstOrDefault(c => ReferenceEquals(c.Category, e.Category));
            if (category == null)
                return;

            if (e.Added)
                ShowGameInCategory(category, e.Game);
            else
                ShowGameOutsideCategory(category, e.Game);
        });
    }

    private void ShowGameInCategory(CategoryViewModel category, GameData game)
    {
        GameViewModel gameModel = FindGameViewModel(game) ?? new GameViewModel(game);
        GameList.Remove(gameModel);

        FranchiseViewModel? franchiseView = null;
        foreach (FranchiseViewModel candidate in category.FranchiseList)
        {
            if (candidate.Franchise.Name == game.Franchise)
            {
                franchiseView = candidate;
                break;
            }
        }

        if (franchiseView == null)
        {
            Franchise? franchise = category.Category.GetFranchise(game.Franchise);
            if (franchise == null)
                return;

            franchiseView = new FranchiseViewModel(franchise);
            category.FranchiseList.Add(franchiseView);
        }

        if (!franchiseView.GameList.Contains(gameModel))
            franchiseView.GameList.Add(gameModel);
    }

    private void ShowGameOutsideCategory(CategoryViewModel category, GameData game)
    {
        FranchiseViewModel? franchiseView = null;
        GameViewModel? gameModel = null;
        foreach (FranchiseViewModel candidate in category.FranchiseList)
        {
            gameModel = candidate.GameList.FirstOrDefault(g => g.Game.GameID == game.GameID);
            if (gameModel == null)
                continue;

            franchiseView = candidate;
            break;
        }

        if (franchiseView == null || gameModel == null)
            return;

        franchiseView.GameList.Remove(gameModel);
        if (franchiseView.GameList.Count == 0)
            category.FranchiseList.Remove(franchiseView);

        if (game.PhysicalState != CartridgePhysicalState.IGNORED && !GameList.Contains(gameModel))
            GameList.Add(gameModel);
    }

    private void OnGameCached(object? sender, GameData game)
    {
        UiThread.Run(() =>
        {
            AdoptCachedInstance(game);
            SyncUnassigned(game);
        });
    }

    private void AdoptCachedInstance(GameData game)
    {
        // SaveGame can replace the cached object. Rows must point at that instance
        // or later property changes will update an object the screen no longer reads.
        GameViewModel? current = FindGameViewModel(game);
        if (current == null || ReferenceEquals(current.Game, game))
            return;

        GameViewModel replacement = new GameViewModel(game);
        int unassignedIndex = GameList.IndexOf(current);
        if (unassignedIndex >= 0)
            GameList[unassignedIndex] = replacement;

        foreach (CategoryViewModel category in CategoryList)
        {
            foreach (FranchiseViewModel franchise in category.FranchiseList)
            {
                int index = franchise.GameList.IndexOf(current);
                if (index >= 0)
                    franchise.GameList[index] = replacement;
            }
        }
    }

    private void OnGameUpdated(object? sender, GameCacheChangeEventArgs e)
    {
        // Franchise and genre changes are structural and handled by CategoryGameChanged.
        // Physical state decides whether an unclassified game stays in the unassigned list.
        if (e.PropertyName == nameof(GameData.PhysicalState))
        {
            UiThread.Run(() => SyncUnassigned(e.Game));
            return;
        }

        // When a game is renamed, re-position it in GameList if present
        if (e.PropertyName == nameof(GameData.Name))
        {
            UiThread.Run(() =>
            {
                GameViewModel? existing = GameList.FirstOrDefault(g => g.Game.GameID == e.Game.GameID);
                if (existing != null)
                {
                    int currentIndex = GameList.IndexOf(existing);
                    bool inOrder = (currentIndex == 0 || string.Compare(GameList[currentIndex - 1].Game.Name, existing.Game.Name, StringComparison.CurrentCulture) <= 0)
                        && (currentIndex == GameList.Count - 1 || string.Compare(existing.Game.Name, GameList[currentIndex + 1].Game.Name, StringComparison.CurrentCulture) <= 0);

                    if (!inOrder)
                    {
                        GameList.RemoveAt(currentIndex);
                        InsertGameInOrder(existing);
                    }
                }
            });
        }
    }

    private void SyncUnassigned(GameData game)
    {
        if (CategoryList.Any(category => category.Category.ContainsGame(game)))
        {
            GameViewModel? classified = GameList.FirstOrDefault(g => g.Game.GameID == game.GameID);
            if (classified != null)
                GameList.Remove(classified);
            return;
        }

        GameViewModel? existing = GameList.FirstOrDefault(g => g.Game.GameID == game.GameID);
        bool show = game.PhysicalState != CartridgePhysicalState.IGNORED;

        if (show && existing == null)
            InsertGameInOrder(FindGameViewModel(game) ?? new GameViewModel(game));
        else if (!show && existing != null)
            GameList.Remove(existing);
    }

    private void InsertGameInOrder(GameViewModel gameModel)
    {
        int index = 0;
        while (index < GameList.Count
            && string.Compare(GameList[index].Game.Name, gameModel.Game.Name, StringComparison.CurrentCulture) < 0)
        {
            index++;
        }
        GameList.Insert(index, gameModel);
    }

    private GameViewModel? FindGameViewModel(GameData game)
    {
        GameViewModel? found = GameList.FirstOrDefault(g => g.Game.GameID == game.GameID);
        if (found != null)
            return found;

        foreach (CategoryViewModel category in CategoryList)
        {
            foreach (FranchiseViewModel franchise in category.FranchiseList)
            {
                found = franchise.GameList.FirstOrDefault(g => g.Game.GameID == game.GameID);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    public override void Dispose()
    {
        _ws.CategoryAdded -= OnCategoryAdded;
        _ws.CategoryRemoved -= OnCategoryRemoved;
        _ws.CategoryGameChanged -= OnCategoryGameChanged;
        _ws.GameCached -= OnGameCached;
        _ws.GameUpdated -= OnGameUpdated;
    }


    //////////////////////
    //// GAME UPDATER ////
    //////////////////////
    private CancellationTokenSource? _gameDebounceCts;
    private HashSet<GameData> _gameToUpdate;
    private async Task DebouncePushGameUpdate(GameData game)
    {
        try
        {
            // 1 - Cancel previous game update
            _gameDebounceCts?.Cancel();

            // 2 - Initialize new cancellation token
            CancellationTokenSource cts = new CancellationTokenSource();
            _gameDebounceCts = cts;

            // 3 - Add game to list
            _gameToUpdate.Add(game);

            // 4 - Wait 1 min
            await Task.Delay(1000, cts.Token);

            // 5 - If not cancelled, execute crop
            PushGameUpdate();
        }
        catch (TaskCanceledException)
        {
            // If cancelled, do nothing
        }
    }

    private void PushGameUpdate()
    {
        foreach (GameData game in _gameToUpdate)
        {
            _ws.LaunchGameManager(game).UpdateGame();
        }
        _gameToUpdate.Clear();
    }

    //////////////////////////
    //// CATEGORY UPDATER ////
    //////////////////////////
    private CancellationTokenSource? _categoryDebounceCts;
    private async Task DebouncePushCategoryUpdate()
    {
        try
        {
            // 1 - Cancel previous category update
            _categoryDebounceCts?.Cancel();

            // 2 - Initialize new cancellation token
            CancellationTokenSource cts = new CancellationTokenSource();
            _categoryDebounceCts = cts;

            // 3 - Wait 1 min
            await Task.Delay(1000, cts.Token);

            // 4 - If not cancelled, execute crop
            PushCategoryUpdate();
        }
        catch (TaskCanceledException)
        {
            // If cancelled, do nothing
        }
    }

    private void PushCategoryUpdate()
    {
        _ws.PushCategory();
    }
}
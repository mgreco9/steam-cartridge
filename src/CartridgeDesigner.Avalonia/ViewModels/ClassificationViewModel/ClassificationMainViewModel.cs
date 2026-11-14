
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using DynamicData;

using ReactiveUI;

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
            CategoryList = [.. CategoryList.OrderBy(c => c.Category.Name)];
        }

        GameList = [];
        foreach (GameData game in _ws.GetLibrary())
        {
            if (existingGameId.Contains(game.GameID))
                continue;

            if (game.PhysicalState == CartridgePhysicalState.IGNORED)
                continue;

            GameList.Add(new GameViewModel(game));
        }
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

        GameManager manager = _ws.LaunchGameManager(item.Game);
        if (item.Game.PhysicalState != CartridgePhysicalState.IGNORED)
        {
            item.Game.PhysicalState = CartridgePhysicalState.IGNORED;
            manager.UpdateGame();

            GameList.Remove(item);
        }
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
        // 1 - Create category
        _ws.CategoryList.Add(category);

        // 2 - Push change
        _ = DebouncePushCategoryUpdate();

        // 3 - Update view
        CategoryList.Add(new CategoryViewModel(category));
    }

    public void RemoveCategory(CategoryViewModel category)
    {
        // 1 - Empty category
        foreach (FranchiseViewModel franchise in category.FranchiseList)
        {
            foreach (GameViewModel game in franchise.GameList)
            {
                category.Category.RemoveGame(game.Game);
            }
        }

        // 2 - Remove category
        _ws.CategoryList.Remove(category.Category);

        // 3 - Push change
        _ = DebouncePushCategoryUpdate();

        // 4 - Update view
        CategoryList.Remove(category);
        foreach (FranchiseViewModel franchise in category.FranchiseList)
        {
            GameList.AddRange(franchise.GameList);
        }
    }

    public void AddGameToCategory(CategoryViewModel category, GameViewModel game, string franchise)
    {
        // 1 - Update game franchise
        game.Game.Franchise = franchise;

        // 2 - Add game to category
        category.Category.AddGame(game.Game);

        // 3 - Push updates
        _ = DebouncePushGameUpdate(game.Game);
        _ = DebouncePushCategoryUpdate();

        // 4 - Update view
        GameList.Remove(game);
        FranchiseViewModel? correspondingFranchise = null;
        foreach (FranchiseViewModel franchiseView in category.FranchiseList)
        {
            if (franchiseView.Franchise.Name == franchise)
            {
                correspondingFranchise = franchiseView;
                break;
            }
        }
        if (correspondingFranchise == null)
        {
            correspondingFranchise = new FranchiseViewModel(category.Category.GetFranchise(franchise)!);
            category.FranchiseList.Add(correspondingFranchise);
        }

        correspondingFranchise.GameList.Add(game);
    }

    [RelayCommand]
    public void RemoveGameFromCategory(GameViewModel game)
    {
        if (SelectedCategory == null)
            return;

        // 1 - Remove game from category
        SelectedCategory.Category.RemoveGame(game.Game);

        // 2 - Push updates
        _ = DebouncePushGameUpdate(game.Game);
        _ = DebouncePushCategoryUpdate();

        // 3 - Update view
        GameList.Add(game);
        FranchiseViewModel? correspondingFranchise = null;
        foreach (FranchiseViewModel franchiseView in SelectedCategory.FranchiseList)
        {
            if (franchiseView.Franchise.Name == game.Game.Franchise)
            {
                correspondingFranchise = franchiseView;
                break;
            }
        }
        if (correspondingFranchise == null)
            return;

        correspondingFranchise.GameList.Remove(game);

        if (correspondingFranchise.GameList.Count == 0)
            SelectedCategory.FranchiseList.Remove(correspondingFranchise);
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
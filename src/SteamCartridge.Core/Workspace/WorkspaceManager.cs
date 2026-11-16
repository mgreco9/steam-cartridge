using System.ComponentModel;
using System.Text.Json;
using SteamCartridge.Core.Workspace;
using SteamCartridge.Core.Events;

namespace SteamCartridge.Core.Workspace;

public class WorkspaceManager : IWorkspaceEvents
{
    private static readonly JsonSerializerOptions SerializeOption = new()
    {
        WriteIndented = true
    };

    private readonly Dictionary<string, GameData> _loaderGameList;
    private readonly object _gameCacheLock = new();
    private readonly object _categoryLock = new();

    public WorkspaceManager(IConfigService configService)
    {
        _loaderGameList = [];
        ClientId = configService.GetClientId();
        SteamAPIKey = configService.GetSteamAPIKey();
        SteamGridDBAPIKey = configService.GetStreamGridDBAPIKey();
        SavePath = configService.GetSaveDirectory();
        PDFOutputPath = Path.Combine(SavePath, "Cartridge Cover.pdf");
        CategoryList = [];
    }

    public event EventHandler<List<Category>>? CategoriesLoaded;
    public event EventHandler<Category>? CategoryAdded;
    public event EventHandler<Category>? CategoryRemoved;
    public event EventHandler? CategoriesCleared;
    public event EventHandler<CategoryGameChangeEventArgs>? CategoryGameChanged;
    public event EventHandler<GameData>? GameCached;
    public event EventHandler<GameCacheChangeEventArgs>? GameUpdated;

    public static WorkspaceManager InitializeWorkspace(IConfigService configService)
    {
        // 1 - Retrieve ws path
        string wsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SteamCartridge");

        // 2 - Initialize workspace
        WorkspaceManager ws = new WorkspaceManager(configService);

        // 3 - Initialize directory if not already done
        Directory.CreateDirectory(ws.SavePath);
        Directory.CreateDirectory(ws.LibraryPath);
        string layoutPath = ws.LayoutPath;
        if(!Path.Exists(layoutPath))
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Assets", "default-layout.json"), layoutPath);
        
        // 4 - Load categories from disk
        string categoryPath = ws.CategoryPath;
        if(Path.Exists(categoryPath))
        {
            using FileStream fs = File.OpenRead(categoryPath);
            using JsonDocument json = JsonDocument.Parse(fs);

            ws.CategoryList = JsonSerializer.Deserialize<List<Category>>(json) ?? throw new InvalidDataException($"Corrupted data category");
            
            // 5 - Raise CategoriesLoaded event after successful load
            ws.CategoriesLoaded?.Invoke(ws, ws.CategoryList);
        }

        // 6 - Return workspace
        return ws;
    }

    //////////////////////////////////
    //// CONFIGURATION MANAGEMENT ////
    //////////////////////////////////
    public string ClientId { get; private set; }
    public string SteamAPIKey { get; private set; }
    public string SteamGridDBAPIKey { get; private set; }
    public string PDFOutputPath { get; private set; }
    public string SavePath { get; private set; }

    public void UpdateConfiguration(IConfigService config)
    {
        // 1 - Update cache
        ClientId = config.GetClientId();
        SteamAPIKey = config.GetSteamAPIKey();
        SteamGridDBAPIKey = config.GetStreamGridDBAPIKey();
        SavePath = config.GetSaveDirectory();

        // 2 - Update json file TODO
    }

    ///////////////////////////
    //// LAYOUT MANAGEMENT ////
    ///////////////////////////
    private string LayoutPath => Path.Combine(SavePath, "layout.json");

    public JsonDocument GetLayoutJson()
    {
        Stream jsonStream = File.OpenRead(LayoutPath);
        return JsonDocument.Parse(jsonStream);
    }

    ////////////////////////////
    //// LIBRARY MANAGEMENT ////
    ////////////////////////////
    private string LibraryPath => Path.Combine(SavePath, "SteamLibrary");

    private string GameFolderPath(int gameId) => GameFolderPath(gameId.ToString());
    private string GameFolderPath(string gameId) => Path.Combine(LibraryPath, gameId);
    private string GameDataPath(int gameId) => GameDataPath(gameId.ToString());
    private string GameDataPath(string gameId) => Path.Combine(GameFolderPath(gameId), "data.json");

    public GameData[] GetLibrary()
    {
        // 1 - Retrieve the list of directories in the library
        string[] directoryList = Directory.GetDirectories(LibraryPath);

        // 2 - For each directory
        GameData[] gameDataList = new GameData[directoryList.Length];
        int idx = 0;
        foreach (string directory in directoryList)
        {
            // 2.1 - Parse the name of the game id (name of the directory)
            string gameId = Path.GetFileName(directory) ?? throw new InvalidDataException($"Corrupted data : {directory}");

            // 2.2 - Parse the game data and add it to the list
            gameDataList[idx++] = LoadGame(gameId);
        }

        // 3 - Return the list of game data
        return gameDataList;
    }

    public GameData LoadGame(int gameId) => LoadGame(gameId.ToString());
    public GameData LoadGame(string gameId)
    {
        // 1 - Return the cached instance so every screen shares the same object
        lock (_gameCacheLock)
        {
            if (_loaderGameList.TryGetValue(gameId, out GameData? cached))
                return cached;
        }

        // 2 - Read the file outside the lock
        using FileStream fs = File.OpenRead(GameDataPath(gameId));
        using JsonDocument json = JsonDocument.Parse(fs);
        GameData gameData = JsonSerializer.Deserialize<GameData>(json) ?? throw new InvalidDataException($"Corrupted data : {gameId}");

        // 3 - Publish it into the cache. Another caller may have won the race.
        lock (_gameCacheLock)
        {
            if (_loaderGameList.TryGetValue(gameId, out GameData? cached))
                return cached;

            TrackGame(gameData);
            return gameData;
        }
    }

    public bool GameExists(int gameId) => GameExists(gameId.ToString());
    public bool GameExists(string gameId)
    {
        string path = GameDataPath(gameId);
        return Path.Exists(path);
    }

    public void SaveGame(GameData game)
    {
        // 1 - Insert into the cache. Notify when the id is new or the cached
        //     instance is replaced. A property edit on the same instance is
        //     already announced by GameUpdated.
        string id = game.GameID.ToString();
        bool notify;
        lock (_gameCacheLock)
        {
            _loaderGameList.TryGetValue(id, out GameData? previous);
            notify = previous == null || !ReferenceEquals(previous, game);
            TrackGame(game);
        }

        // 2 - Persist the same instance that the cache now holds
        string jsonString = JsonSerializer.Serialize(game, SerializeOption);
        string path = GameDataPath(game.GameID);

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new Exception("error path"));
        File.WriteAllText(path, jsonString);

        // 3 - Tell listeners the cache gained a new instance
        if (notify)
            GameCached?.Invoke(this, game);
    }

    /// <summary>
    /// Stores the game and listens to its property changes.
    /// Must be called under <see cref="_gameCacheLock"/>.
    /// </summary>
    private void TrackGame(GameData game)
    {
        string id = game.GameID.ToString();
        if (_loaderGameList.TryGetValue(id, out GameData? previous))
        {
            if (ReferenceEquals(previous, game))
                return;

            previous.PropertyChanged -= OnCachedGameChanged;
        }

        game.PropertyChanged -= OnCachedGameChanged;
        game.PropertyChanged += OnCachedGameChanged;
        _loaderGameList[id] = game;
    }

    private void OnCachedGameChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not GameData game)
            return;

        // Property changes already happened on the cached instance.
        // Listeners project that change onto whichever list is showing the game.
        GameUpdated?.Invoke(this, new GameCacheChangeEventArgs(game, e.PropertyName));
    }

    ///////////////////////////
    //// EDITOR MANAGEMENT ////
    ///////////////////////////
    public GameManager LaunchGameManager(int gameId) => LaunchGameManager(gameId.ToString());
    public GameManager LaunchGameManager(string gameId) => LaunchGameManager(LoadGame(gameId));
    public GameManager LaunchGameManager(GameData game)
    {
        return new GameManager(GameFolderPath(game.GameID), game);
    }

    public GameManager LaunchGameManagerLocally(int gameId) => LaunchGameManagerLocally(gameId.ToString());
    public GameManager LaunchGameManagerLocally(string gameId) => LaunchGameManagerLocally(LoadGame(gameId));
    public GameManager LaunchGameManagerLocally(GameData game)
    {
        // 1 - Retrieve local ws path
        string wsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SteamCartridge", game.GameID.ToString());

        // 2 - Initialize directory if not already done
        Directory.CreateDirectory(wsPath);

        // 3 - Copy every data from save directory into ws
        string savePath = GameFolderPath(game.GameID);
        
        // 3.1 - Copy every subdirectories
        foreach (string dirPath in Directory.GetDirectories(savePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(savePath, wsPath));
        }

        // 3.2 - Copy every files
        foreach (string newPath in Directory.GetFiles(savePath, "*.*",SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(savePath, wsPath), true);
        }

        // 4 - Launch Local Game Manager
        return new LocalGameManager(wsPath, savePath, game);
    }

    /////////////////////////////
    //// CATEGORY MANAGEMENT ////
    /////////////////////////////
    public List<Category> CategoryList;
    private string CategoryPath => Path.Combine(SavePath, "category.json");

    public void AddCategory(Category category)
    {
        lock (_categoryLock)
        {
            CategoryList.Add(category);
        }

        CategoryAdded?.Invoke(this, category);
    }

    public void RemoveCategory(Category category)
    {
        bool removed;
        lock (_categoryLock)
        {
            removed = CategoryList.Remove(category);
        }

        if (removed)
            CategoryRemoved?.Invoke(this, category);
    }

    public void AddGameToCategory(Category category, GameData game, string franchise)
    {
        // 1 - Mutate the cached game and the category.
        //     GameData raises PropertyChanged, which becomes GameUpdated.
        game.Franchise = franchise;
        category.AddGame(game);

        // 2 - Tell listeners the game moved into this category
        CategoryGameChanged?.Invoke(this, new CategoryGameChangeEventArgs(category, game, added: true));
    }

    public void RemoveGameFromCategory(Category category, GameData game)
    {
        // 1 - Mutate the category. Game property changes notify on their own.
        category.RemoveGame(game);

        // 2 - Tell listeners the game left this category
        CategoryGameChanged?.Invoke(this, new CategoryGameChangeEventArgs(category, game, added: false));
    }

    public void PushCategory()
    {
        string jsonString = JsonSerializer.Serialize(CategoryList, SerializeOption);
        File.WriteAllText(CategoryPath, jsonString);
    }
}
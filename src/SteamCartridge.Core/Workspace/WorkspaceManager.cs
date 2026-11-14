using System.Text.Json;

namespace SteamCartridge.Core.Workspace;

public class WorkspaceManager
{
    private static readonly JsonSerializerOptions SerializeOption = new()
    {
        WriteIndented = true
    };

    private Dictionary<string, GameData> _loaderGameList;

    /////////////////
    //// CREATOR ////
    /////////////////
    private WorkspaceManager(IConfigService configService)
    {
        _loaderGameList = [];
        ClientId = configService.GetClientId();
        SteamAPIKey = configService.GetSteamAPIKey();
        SteamGridDBAPIKey = configService.GetStreamGridDBAPIKey();
        SavePath = configService.GetSaveDirectory();
        PDFOutputPath = Path.Combine(SavePath, "Cartridge Cover.pdf");
        CategoryList = [];
    }

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
        
        string categoryPath = ws.CategoryPath;
        if(Path.Exists(categoryPath))
        {
            using FileStream fs = File.OpenRead(categoryPath);
            using JsonDocument json = JsonDocument.Parse(fs);

            ws.CategoryList = JsonSerializer.Deserialize<List<Category>>(json) ?? throw new InvalidDataException($"Corrupted data category");
        }

        // 4 - Return workspace
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
        if(_loaderGameList.TryGetValue(gameId, out GameData? gameData))
            return gameData;

        using FileStream fs = File.OpenRead(GameDataPath(gameId));
        using JsonDocument json = JsonDocument.Parse(fs);

        gameData = JsonSerializer.Deserialize<GameData>(json) ?? throw new InvalidDataException($"Corrupted data : {gameId}");
        _loaderGameList[gameId] = gameData;

        return gameData;
    }

    public bool GameExists(int gameId) => GameExists(gameId.ToString());
    public bool GameExists(string gameId)
    {
        string path = GameDataPath(gameId);
        return Path.Exists(path);
    }

    public void SaveGame(GameData game)
    {
        _loaderGameList[game.GameID.ToString()] = game;

        string jsonString = JsonSerializer.Serialize(game, SerializeOption);
        string path = GameDataPath(game.GameID);

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new Exception("error path"));
        File.WriteAllText(path, jsonString);
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

    public void PushCategory()
    {
        string jsonString = JsonSerializer.Serialize(CategoryList, SerializeOption);
        File.WriteAllText(CategoryPath, jsonString);
    }
}
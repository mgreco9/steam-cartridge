using System.Text.Json;

namespace SteamCartridge.Core.Workspace;

public class GameManager : IDisposable
{
    protected static readonly JsonSerializerOptions SerializeOption = new()
    {
        WriteIndented = true
    };

    protected readonly string _wsPath;
    protected readonly FolderManager _cartridgeManager;
    protected readonly Dictionary<Repository, FolderManager> _launchedRepositoryList;
    public GameData Game { get; }


    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public GameManager(string wsPath, GameData game)
    {
        _wsPath = wsPath;
        _launchedRepositoryList = [];
        _cartridgeManager = LaunchRepositoryManager(Repository.CARTRIDGE);
        Game = game;
    }

    public virtual void SaveChange()
    {
        // NOTHING TO DO
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    ////////////////////////////////
    //// GAME UPDATE MANAGEMENT ////
    ////////////////////////////////
    public void UpdateGame()
    {
        string jsonString = JsonSerializer.Serialize(Game, SerializeOption);
        File.WriteAllText(Path.Combine(_wsPath, "data.json"), jsonString);
    }

    ////////////////////////////
    //// REPOSITORY MANAGER ////
    ////////////////////////////
    protected string RepositoryPath(Repository repository)
    {
        return repository switch
        {
            Repository.CARTRIDGE => Path.Combine(_wsPath, "Cartridge"),
            Repository.FRONT_HOTIZONTAL => Path.Combine(_wsPath, "Front_Horizontal"),
            Repository.FRONT_VERTICAL => Path.Combine(_wsPath, "Front_Vertical"),
            Repository.BACK => Path.Combine(_wsPath, "Back"),
            Repository.ICON => Path.Combine(_wsPath, "Icon"),
            _ => throw new NotImplementedException(),
        };
    }

    public FolderManager LaunchRepositoryManager(Repository repository)
    {
        // 1 - If folder already instantiated, return it
        if(_launchedRepositoryList.TryGetValue(repository, out FolderManager? repo))
            return repo;

        // 2 - Load repository manager
        string repositoryPath = RepositoryPath(repository);
        Directory.CreateDirectory(repositoryPath);
        repo = new FolderManager(repositoryPath);

        // 3 - Save manager in cache
        _launchedRepositoryList[repository] = repo;

        // 4 - Return manager
        return repo;
    }

    ///////////////////////
    //// COVER MANAGER ////
    ///////////////////////
    private readonly Dictionary<string, List<Action<ImageItem>>> _subscriptions = new();

    public void PushCoverImage(ImageItem image)
    {
        // 1 - Save the image
        _cartridgeManager.SaveImage(image);

        // 2 - Push the image event 
        if (_subscriptions.TryGetValue(image.Name, out var handlers))
        {
            foreach (var handler in handlers)
            {
                handler(image);
            }
        }
    }

    public void SubscribeCoverImageModifiedEvent(string id, Action<ImageItem> handler)
    {
        // 1 - Initialize subscription key if does not exist already
        if (!_subscriptions.TryGetValue(id, out List<Action<ImageItem>>? value))
        {
            value = new List<Action<ImageItem>>();
            _subscriptions[id] = value;
        }

        // 2 - Add the new value to the list
       value.Add(handler);
    }
}
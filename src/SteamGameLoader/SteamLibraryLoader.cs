using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

using craftersmine.SteamGridDBNet;

using HtmlAgilityPack;

using Polly;
using Polly.Retry;

using SteamCartridge.Core.Workspace;

namespace SteamGameLoader;

public class SteamLibraryLoader
{
    public static class RetryHelper
    {
        public static async Task<T> ExecuteAsync<T>(
            Func<Task<T>> action,
            int retryCount = 3)
        {
            AsyncRetryPolicy retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

            return await retryPolicy.ExecuteAsync(action);
        }
    }

    private readonly WorkspaceManager _ws;
    private readonly SteamGridDb _sgdb;
    private string OwnedGamesURI => $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={_ws.SteamAPIKey}&steamid={_ws.ClientId}&include_appinfo=true";

    public SteamLibraryLoader(WorkspaceManager ws)
    {
        _ws = ws;
        _sgdb = new SteamGridDb(ws.SteamGridDBAPIKey);
    }

    public async Task LoadSteamLibrary()
    {
        // 1 - Retrieve json game list
        List<int> gameIdList = await RetrieveLibraryGameIds();

        // 2 - Build each game 
        foreach (int gameId in gameIdList)
        {
            if (_ws.GameExists(gameId))
                continue;

            await LoadGame(gameId);
        }
    }

    public async Task LoadGame(int gameId)
    {
        // 1 - Retrieve game data
        GameData gameData = await RetrieveGameData(gameId);

        // 2 - Save it in ws
        _ws.SaveGame(gameData);

        if (gameData.PhysicalState == CartridgePhysicalState.ERROR)
            return;

        // 3 - Retrieve and save the associated images
        using GameManager gameManager = _ws.LaunchGameManager(gameId);
        await PullGameImages(gameManager);
    }

    public async Task<List<int>> RetrieveLibraryGameIds()
    {
        // 1 - Query user game library
        using HttpClient client = new();
        string? json = await client.GetStringAsync(OwnedGamesURI);

        // 2 - Retrieve json document
        JsonDocument gameLibrary = JsonDocument.Parse(json);

        // 3 - Retrieve game list 
        JsonElement gameList = gameLibrary.RootElement
                .GetProperty("response")
                .GetProperty("games");

        // 4 - Add each game id into a list
        List<int> gameIdList = [];
        foreach (JsonElement game in gameList.EnumerateArray())
        {
            gameIdList.Add(game.GetProperty("appid").GetInt32());
        }

        // 5 - Return the game id list
        return gameIdList;
    }

    /////////////////////////////
    //// GAME DATA RETRIEVER ////
    /////////////////////////////
    public async Task<GameData> RetrieveGameData(int gameId)
    {
        // 1 - Initialize game data
        GameData gameData = new GameData()
        {
            GameID = gameId
        };

        // 2 - Initialise the client
        using HttpClient httpClient = new HttpClient();

        // 3 - Query game details
        HttpResponseMessage httpResponse = await httpClient.GetAsync($"https://store.steampowered.com/api/appdetails?appids={gameId}");
        JsonDocument json = await httpResponse.Content.ReadFromJsonAsync<JsonDocument>() ?? throw new Exception($"Could not query {gameId}");

        if (!json.RootElement.GetProperty(gameId.ToString()).GetProperty("success").GetBoolean())
        {
            gameData.PhysicalState = CartridgePhysicalState.ERROR;
            gameData.Name = "Error";
            return gameData;
        }

        // 4 - Parse the retrieve data into the object
        JsonElement gameDataJson = json.RootElement.GetProperty(gameId.ToString()).GetProperty("data");

        gameData.Name = gameDataJson.GetProperty("name").GetString() ?? throw new Exception($"Parsing error");
        gameData.Franchise = gameData.Name;

        string date = gameDataJson.GetProperty("release_date").GetProperty("date").GetString() ?? throw new Exception($"Parsing error");

        try
        {
            gameData.ReleaseDate = DateTime.ParseExact(date, "d MMM, yyyy", CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            try
            {
                gameData.ReleaseDate = DateTime.ParseExact(date, "MMM d, yyyy", CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
            }
        }

        // 5 - Query game tags
        httpResponse = await httpClient.GetAsync($"https://store.steampowered.com/app/{gameId}");
        string html = await httpResponse.Content.ReadAsStringAsync() ?? throw new Exception($"Could not query {gameId}");

        // 6 - Parse the html data to retrieve the game tags
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(html);

        HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//a[contains(@class,'app_tag')]");

        gameData.TagList = nodes?
            .Select(n => n.InnerText.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList() ?? [];

        // 7 - Return game data result
        return gameData;
    }

    //////////////////////////////
    //// GAME IMAGE RETRIEVER ////
    //////////////////////////////
    public async Task PullGameImages(GameManager ws)
    {
        // 1 - Retrieve game id
        int gameId = ws.Game.GameID;

        FolderManager repositoryWs;

        // 2 - Retrieve horizontal front page
        repositoryWs = ws.LaunchRepositoryManager(Repository.FRONT_HOTIZONTAL);
        SteamGridDbGrid[] horizontalFrontImages = await RetryHelper.ExecuteAsync(() => _sgdb.GetGridsByPlatformGameIdAsync(SteamGridDbGamePlatform.Steam, gameId,
                styles: SteamGridDbStyles.Alternate,
                types: SteamGridDbTypes.Static,
                dimensions: SteamGridDbDimensions.W920H430,
                limit: 10));

        for (int i = 0; i < horizontalFrontImages.Length; i++)
        {
            using Stream stream = await RetryHelper.ExecuteAsync(() => horizontalFrontImages[i].GetImageAsStreamAsync(false));
            using MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            ImageItem image = new ImageItem
            {
                Name = i.ToString(),
                Thumbnail = ms.ToArray()
            };
            repositoryWs.SaveImage(image);
        }

        // 2 - Retrieve vertical front page
        repositoryWs = ws.LaunchRepositoryManager(Repository.FRONT_VERTICAL);
        SteamGridDbGrid[] verticalFrontImages = await RetryHelper.ExecuteAsync(() => _sgdb.GetGridsByPlatformGameIdAsync(SteamGridDbGamePlatform.Steam, gameId,
                styles: SteamGridDbStyles.Alternate,
                types: SteamGridDbTypes.Static,
                dimensions: SteamGridDbDimensions.W600H900,
                limit: 10));

        for (int i = 0; i < verticalFrontImages.Length; i++)
        {
            using Stream stream = await RetryHelper.ExecuteAsync(() => verticalFrontImages[i].GetImageAsStreamAsync(false));
            using MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            ImageItem image = new ImageItem
            {
                Name = i.ToString(),
                Thumbnail = ms.ToArray()
            };
            repositoryWs.SaveImage(image);
        }

        // 3 - Retrieve back cover
        repositoryWs = ws.LaunchRepositoryManager(Repository.BACK);
        SteamGridDbHero[] backImages = await RetryHelper.ExecuteAsync(() => _sgdb.GetHeroesByPlatformGameIdAsync(SteamGridDbGamePlatform.Steam, gameId,
                styles: SteamGridDbStyles.Alternate,
                types: SteamGridDbTypes.Static,
                limit: 10));

        for (int i = 0; i < backImages.Length; i++)
        {
            using Stream stream = await RetryHelper.ExecuteAsync(() => backImages[i].GetImageAsStreamAsync(false));
            using MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            ImageItem image = new ImageItem
            {
                Name = i.ToString(),
                Thumbnail = ms.ToArray()
            };
            repositoryWs.SaveImage(image);
        }

        // 4 - Retrieve icons
        repositoryWs = ws.LaunchRepositoryManager(Repository.ICON);
        SteamGridDbIcon[] iconImages = await RetryHelper.ExecuteAsync(() => _sgdb.GetIconsByPlatformGameIdAsync(SteamGridDbGamePlatform.Steam, gameId,
                types: SteamGridDbTypes.Static,
                limit: 10));

        for (int i = 0; i < iconImages.Length; i++)
        {
            using Stream stream = await RetryHelper.ExecuteAsync(() => iconImages[i].GetImageAsStreamAsync(false));
            using MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            ImageItem image = new ImageItem
            {
                Name = i.ToString(),
                Thumbnail = ms.ToArray()
            };
            repositoryWs.SaveImage(image);
        }
    }
}
namespace SteamCartridge.Core.Workspace;

public class Franchise
{
    public string Name { get; set; }
    public HashSet<int> GameIdList { get; set; }

    public Franchise()
    {
        Name = "";
        GameIdList = [];
    }

    public void AddGame(GameData game) => GameIdList.Add(game.GameID);
    public void RemoveGame(GameData game) => GameIdList.Remove(game.GameID);
    public void ContainsGame(GameData game) => GameIdList.Contains(game.GameID);
    
}
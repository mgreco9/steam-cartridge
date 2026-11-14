
namespace SteamCartridge.Core.Workspace;

public class Category
{
    public string Name { get; set; }
    public string Prefix { get; set; }
    public List<Franchise> FranchiseList { get; set; }

    public Category()
    {
        Name = "";
        Prefix = "";
        FranchiseList = [];
    }

    public Franchise? GetFranchise(string franchiseName)
    {
        foreach (Franchise franchise in FranchiseList)
        {
            if (franchise.Name == franchiseName)
                return franchise;
        }

        return null;
    }

    public void AddGame(GameData game)
    {
        // 1 - Retrieve Game Franchise
        string franchiseName = game.Franchise;

        // 2 - If franchise already contained, add it to existing one
        Franchise? correspondingFranchise = null;
        foreach (Franchise franchise in FranchiseList)
        {
            if (franchise.Name == franchiseName)
            {
                correspondingFranchise = franchise;
                break;
            }
        }

        // 3 - If franchise not found, create it
        if (correspondingFranchise == null)
        {
            correspondingFranchise = new Franchise { Name = franchiseName };
            FranchiseList.Add(correspondingFranchise);
        }

        // 4 - Add game to franchise
        correspondingFranchise.AddGame(game);

        // 5 - Set game genre and numerotation
        game.Genre = Name;
        game.Classification = Prefix;
        game.Numerotation = GetGameNumerotation(game);
    }

    public void RemoveGame(GameData game)
    {
        // 1 - Retrieve Game Franchise
        string franchiseName = game.Franchise;

        // 2 - Find franchise containing game
        Franchise? targetFranchise = null;
        foreach (Franchise franchise in FranchiseList)
        {
            if (franchise.Name == franchiseName)
            {
                targetFranchise = franchise;
                break;
            }
        }

        // 3 - Remove game from franchise
        if (targetFranchise == null)
            return;

        targetFranchise.RemoveGame(game);
        if (targetFranchise.GameIdList.Count == 0)
            FranchiseList.Remove(targetFranchise);

        // 4 - Set gResme genre and numerotation
        game.Genre = "Undefined";
        game.Numerotation = "0";
    }

    public bool ContainsGame(GameData game)
    {
        // 1 - Retrieve Game Franchise
        string franchiseName = game.Franchise;

        // 2 - Iterate over each franchise
        foreach (Franchise franchise in FranchiseList)
        {
            if (franchise.Name == franchiseName)
                return true;
        }

        // 3 - Franchise not found, return false
        return false;
    }

    public string GetGameNumerotation(GameData game)
    {
        // 1 - Retrieve Game Franchise
        string franchiseName = game.Franchise;

        // 2 - Iterate over each franchise
        int numerotation = 1;
        foreach (Franchise franchise in FranchiseList)
        {
            if (franchise.Name == franchiseName)
                break;

            numerotation++;
        }

        // 3 - If franchise is not found, throw exception
        if (numerotation > FranchiseList.Count)
            throw new Exception("game is not in category");

        // 4 - Return numerotation
        return numerotation.ToString();
    }
}
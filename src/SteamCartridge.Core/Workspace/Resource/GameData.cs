namespace SteamCartridge.Core.Workspace;

public class GameData
{
    public required int GameID { get; set; }
    public string Name { get; set; }
    public DateTime ReleaseDate { get; set; }
    public CartridgeDesignState DesignState { get; set; }
    public CartridgePhysicalState PhysicalState { get; set; }
    public string Genre { get; set; }
    public string Franchise { get; set; }
    public string Classification { get; set; }
    public string Numerotation { get; set; }
    public List<string> TagList { get; set; }

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public GameData()
    {
        Name = "";
        DesignState = CartridgeDesignState.NOT_DESIGNED;
        PhysicalState = CartridgePhysicalState.NOT_ASSIGNED;
        Genre = "Undefined";
        Franchise = "Undefined";
        Classification = ".";
        Numerotation = "0";
        TagList = [];
    }
}
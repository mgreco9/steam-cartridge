using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using SteamCartridge.Core.Workspace;

public class FranchiseViewModel : ObservableObject
{
    public Franchise Franchise { get; }
    public ObservableCollection<GameViewModel> GameList { get; }

    public FranchiseViewModel(Franchise franchise)
    {
        Franchise = franchise;
        GameList = [];
    }

    public void LoadGameList(WorkspaceManager ws)
    {
        foreach (int gameId in Franchise.GameIdList)
        {
            GameList.Add(new GameViewModel(ws.LoadGame(gameId)));
        }
    }
}
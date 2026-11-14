using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using SteamCartridge.Core.Workspace;

public class CategoryViewModel : ObservableObject
{
    public Category Category { get; }
    public ObservableCollection<FranchiseViewModel> FranchiseList { get; }

    public CategoryViewModel(Category category)
    {
        Category = category;

        FranchiseList = [];
        foreach (Franchise franchise in Category.FranchiseList)
        {
            FranchiseList.Add(new FranchiseViewModel(franchise));
        }
    }

    public void LoadGameList(WorkspaceManager ws)
    {
        foreach (FranchiseViewModel franchise in FranchiseList)
        {
            franchise.LoadGameList(ws);
        }
    }
}
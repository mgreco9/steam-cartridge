using System.Text.Json;

namespace SteamCartridge.Core.Workspace;

public class LocalGameManager : GameManager
{
    protected readonly string _savePath;

    public LocalGameManager(string wsPath, string savePath, GameData game) : base(wsPath, game)
    {
        _savePath = savePath;
    }

    public override void SaveChange()
    {
        // 1 - Copy each new file to save folder
        foreach((Repository repository, FolderManager manager) in _launchedRepositoryList)
        {
            Directory.CreateDirectory(SaveRepositoryPath(repository));

            foreach(string savedImage in manager.SavedImageList)
            {
                string originPath = Path.Combine(RepositoryPath(repository), savedImage);
                string targetPath = Path.Combine(SaveRepositoryPath(repository), savedImage);

                File.Copy(originPath, targetPath, true);
            }
        }

        // 2 - Copy data to save folder
        File.Copy(Path.Combine(_wsPath, "data.json"), Path.Combine(_savePath, "data.json"), true);
    }

    public override void Dispose()
    {
        // 1 - Delete local workspace
        Directory.Delete(_wsPath, true);

        // 2 - Finish dispose
        GC.SuppressFinalize(this);
    }

    ////////////////////////////
    //// REPOSITORY MANAGER ////
    ////////////////////////////
    protected string SaveRepositoryPath(Repository repository)
    {
        return repository switch
        {
            Repository.CARTRIDGE => Path.Combine(_savePath, "Cartridge"),
            Repository.FRONT_HOTIZONTAL => Path.Combine(_savePath, "Front_Horizontal"),
            Repository.FRONT_VERTICAL => Path.Combine(_savePath, "Front_Vertical"),
            Repository.BACK => Path.Combine(_savePath, "Back"),
            Repository.ICON => Path.Combine(_savePath, "Icon"),
            _ => throw new NotImplementedException(),
        };
    }
}
namespace SteamCartridge.Core.Workspace;

public class FolderManager
{
    protected readonly string _wsPath;
    public HashSet<string> SavedImageList { get; }

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public FolderManager(string wsPath)
    {
        _wsPath = wsPath;
        SavedImageList = [];
    }

    ///////////////////////
    //// IMAGE MANAGER ////
    ///////////////////////
    public void SaveImage(ImageItem image)
    {
        SavedImageList.Add($"{image.Name}.png");
        File.WriteAllBytes(Path.Combine(_wsPath, $"{image.Name}.png"), image.Thumbnail);
    }

    public ImageItem[] LoadAllImages()
    {
        // 1 - Retrieve each image path
        string[] imagePathList = [.. Directory.GetFiles(_wsPath).Where(f => f.EndsWith(".png") || f.EndsWith(".jpg"))];

        // 2 - Load the images
        ImageItem[] result = new ImageItem[imagePathList.Length];
        int idx = 0;
        foreach (string imagePath in imagePathList)
        {
            result[idx++] = new ImageItem
            {
                Name = Path.GetFileNameWithoutExtension(imagePath),
                Thumbnail = File.ReadAllBytes(imagePath)
            };
        }

        // 3 - Return the result
        return result;
    }

    public ImageItem? LoadImage(string name)
    {
        string filename = Path.Combine(_wsPath, $"{name}.png");

        if (!Path.Exists(filename))
            return null;

        return new ImageItem
        {
            Name = Path.GetFileNameWithoutExtension(name),
            Thumbnail = File.ReadAllBytes(filename)
        };
    }
}
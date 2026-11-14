using Avalonia.Media.Imaging;

namespace SteamCartridge.Core.Workspace;

public class ImageItem
{
        public required string Name { get; set; }
        public required byte[] Thumbnail { get; set; }
}

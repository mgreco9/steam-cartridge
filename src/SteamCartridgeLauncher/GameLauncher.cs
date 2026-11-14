using System.Diagnostics;

namespace SteamCartridgeLauncher;

public class GameLauncher
{
    private bool _running;
    private readonly CartridgeReader _reader;

    public GameLauncher(CartridgeReader reader)
    {
        _running = false;
        _reader = reader;

        reader.CardInserted += CartridgeInsertedEvent;
    }

    public void Activate()
    {
        _running = true;
    }

    public void Pause()
    {
        _running = false;
    }

    public void CartridgeInsertedEvent()
    {
        if(!_running)
            return;

        string? text = _reader.ReadCard();

        if(text == null)
            return;

        if(!text.StartsWith("steam:"))
            return;

        string gameId = text[6..];
        Process.Start(new ProcessStartInfo
        {
            FileName = $"steam://run/{gameId}",
            UseShellExecute = true
        });
    }
}
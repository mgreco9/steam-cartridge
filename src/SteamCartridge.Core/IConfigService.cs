namespace SteamCartridge.Core;

public interface IConfigService
{
    public string GetClientId();
    public string GetSteamAPIKey();
    public string GetStreamGridDBAPIKey();
    public string GetSaveDirectory();
}
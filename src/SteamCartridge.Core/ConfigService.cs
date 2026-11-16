using System.Text.Json;

namespace SteamCartridge.Core;

public class ConfigService : IConfigService
{
    private static readonly JsonSerializerOptions SerializeOption = new()
    {
        WriteIndented = true
    };
    
    public ConfigService()
    {
        SteamClientID = "";
        SteamAPIKey = "";
        SteamGridDBAPIKey = "";
        SaveDirectory = "";
    }

    public string SteamClientID { get; set; }
    public string SteamAPIKey { get; set; }
    public string SteamGridDBAPIKey { get; set; }
    public string SaveDirectory { get; set; }
    private static string ConfigFileLocation => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SteamCartridge", "config.json");

    public string GetClientId()
    {
        return SteamClientID;
    }

    public string GetSteamAPIKey()
    {
        return SteamAPIKey;
    }

    public string GetStreamGridDBAPIKey()
    {
        return SteamGridDBAPIKey;
    }

    public string GetSaveDirectory()
    {
        return SaveDirectory;
    }

    public void Save()
    {
        string jsonString = JsonSerializer.Serialize(this, SerializeOption);
        string path = ConfigFileLocation;

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new Exception("error path"));
        File.WriteAllText(path, jsonString);
    }

    public static ConfigService Load()
    {
        using FileStream fs = File.OpenRead(ConfigFileLocation);
        using JsonDocument json = JsonDocument.Parse(fs);

        ConfigService config = JsonSerializer.Deserialize<ConfigService>(json) ?? throw new InvalidDataException($"Corrupted config");
        return config;
    }

    public static bool Exists()
    {
        return File.Exists(ConfigFileLocation);
    }
}

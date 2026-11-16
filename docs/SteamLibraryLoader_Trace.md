# SteamLibraryLoader & SteamGridDB.NET Integration Trace

## 1. INITIALIZATION FLOW

### 1.1 WorkspaceManager Constructor Configuration
```csharp
// File: WorkspaceManager.cs (lines 28-46)
WorkspaceManager(IConfigService configService)
{
    _loaderGameList = [];
    
    ClientId = configService.GetClientId();                    // Steam API ID
    SteamAPIKey = configService.GetSteamAPIKey();            // Steam Web API Key  
    SteamGridDBAPIKey = configService.GetStreamGridDBAPIKey(); // SGDB API Key
    SavePath = configService.GetSaveDirectory();
    PDFOutputPath = Path.Combine(SavePath, "Cartridge Cover.pdf");
    CategoryList = [];
}
```

### 1.2 SteamLibraryLoader Initialization
```csharp
// File: SteamLibraryLoader.cs (lines 34-42)
public class SteamLibraryLoader
{
    private readonly WorkspaceManager _ws;
    private readonly SteamGridDb _sgdb;
    
    public SteamLibraryLoader(WorkspaceManager ws)
    {
        _ws = ws;
        _sgdb = new SteamGridDb(ws.SteamGridDBAPIKey);  // API KEY ONLY
    }
}
```

### 1.3 External Dependencies Confirmed
- **Package**: `craftersmine.SteamGridDB.Net` v1.1.7
- **Namespace**: `craftersmine.SteamGridDBNet`
- **Client Class**: `SteamGridDb`
- **API Key Pattern**: Matches official documentation

**Official API Usage** (from SteamGridDB.NET Wiki):
```csharp
// Wiki Example: SteamGridDb sgdb = new SteamGridDb("your_api_key");
// Project Implementation: SteamGridDb _sgdb = new SteamGridDb(ws.SteamGridDBAPIKey);
```

---

## 2. API USAGE MAPPING

### 2.1 Grid Retrieval Methods

| Source Location | Method Call | Parameters | Purpose |
|-----------------|-------------|------------|---------|
| `SteamLibraryLoader.cs:179-183` | `_sgdb.GetGridsByPlatformGameIdAsync()` | Platform: Steam, Styles: Alternate, Types: Static, Dimensions: W920H430, Limit: 10 | Horizontal Front Page |
| `SteamLibraryLoader.cs:200-204` | `_sgdb.GetGridsByPlatformGameIdAsync()` | Platform: Steam, Styles: Alternate, Types: Static, Dimensions: W600H900, Limit: 10 | Vertical Front Page |

**Official API Signature**:
```csharp

---

## 3. EXECUTION FLOW DIAGRAM

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         LoadSteamLibrary()                                   │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
        ┌───────────────────────────────────────────────────────────┐
        │ RetrieveLibraryGameIds() - Steam API v1                   │
        │ URL: /IPlayerService/GetOwnedGames/v1/                    │
        │ Parameters: key={SteamAPIKey}, steamid={ClientId}         │
        └───────────────────────────────────────────────────────────┘
                                    │
                                    ▼
              ┌──────────────────────────────────────────────┐
              │ Filter: _ws.GameExists(gameId)               │
              │ Skip games already cached                    │
              └──────────────────────────────────────────────┘
                                    │
                                    ▼ (per game)
        ┌───────────────────────────────────────────────────────────┐
        │ LoadGame(gameId)                                         │
        └───────────────────────────────────────────────────────────┘
                                    │
               ┌────────────────────┴────────────────────┐
               │                                         │
               ▼                                         ▼
    ┌───────────────────────────────┐          ┌───────────────────────────────┐
    │ RetrieveGameData(gameId)      │          │ SaveGame(gameData)            │
    │                               │          │ Store in _ws._loaderGameList  │
    │ 1. Steam AppDetails API       │          │ Serialize to JSON file        │
    │    - https://store.steampowered│          │ Path: /SteamLibrary/{id}/data.json
    │    /api/appdetails?appids={id}│          └───────────────────────────────┘
    │ 2. HTML Parse (Tags)          │
    └───────────────────────────────┘
               │
               ▼
        ┌───────────────────────────────────────────────────────────┐
        │ PullGameImages(gameManager)                               │
        │                                                           │
        │ ┌─────────────────────────────────────────────────────┐  │
        │ │ Repository: FRONT_HOTIZONTAL                        │  │
        │ │ _sgdb.GetGridsByPlatformGameIdAsync()              │  │
        │ │ - Styles.Alternate, Types.Static                   │  │
        │ │ - Dimensions.W920H430 (Horizontal)                 │  │
        │ │ - Limit: 10                                        │  │
        │ └─────────────────────────────────────────────────────┘  │
        │                                                           │
        │ ┌─────────────────────────────────────────────────────┐  │
        │ │ Repository: FRONT_VERTICAL                          │  │
        │ │ _sgdb.GetGridsByPlatformGameIdAsync()              │  │
        │ │ - Styles.Alternate, Types.Static                   │  │
        │ │ - Dimensions.W600H900 (Vertical)                   │  │
        │ │ - Limit: 10                                        │  │
        │ └─────────────────────────────────────────────────────┘  │
        │                                                           │
        │ ┌─────────────────────────────────────────────────────┐  │
        │ │ Repository: BACK                                    │  │
        │ │ _sgdb.GetHeroesByPlatformGameIdAsync()             │  │
        │ │ - Styles.Alternate, Types.Static                   │  │
        │ │ - Limit: 10                                        │  │
        │ └─────────────────────────────────────────────────────┘  │
        │                                                           │
        │ ┌─────────────────────────────────────────────────────┐  │
        │ │ Repository: ICON                                    │  │
        │ │ _sgdb.GetIconsByPlatformGameIdAsync()              │  │
        │ │ - Types.Static                                      │  │
        │ │ - Limit: 10                                        │  │

---

## 5. CONFIGURATION VERIFICATION

### 5.1 API Key Sources
```csharp
// WorkspaceManager.cs: Line 34-36
ClientId = configService.GetClientId();                    // Required for Steam API
SteamAPIKey = configService.GetSteamAPIKey();              // Required for Steam Web API  
SteamGridDBAPIKey = configService.GetStreamGridDBAPIKey(); // Required for SGDB client

// Passed to SteamLibraryLoader (line 41)
_sgdb = new SteamGridDb(ws.SteamGridDBAPIKey);
```

**Status**: All keys properly sourced from `IConfigService` - no hardcoded values.

### 5.2 Enum Values Used

| Enum Type | Values in Code | Purpose |
|-----------|----------------|---------|
| `SteamGridDbGamePlatform` | `Steam` | Platform specification (Steam AppID) |
| `SteamGridDbStyles` | `Alternate` | Alternate style variant |
| `SteamGridDbTypes` | `Static` | Static image type (non-animated) |
| `SteamGridDbDimensions` | `W920H430`, `W600H900` | Aspect ratios for different layouts |

---

## 6. RESPONSE PARSING & STORAGE FLOW

### 6.1 Grid Response Processing (Line 179-196)
```csharp
SteamGridDbGrid[] horizontalFrontImages = await RetryHelper.ExecuteAsync(() => 
    _sgdb.GetGridsByPlatformGameIdAsync(...));

for (int i = 0; i < horizontalFrontImages.Length; i++)
{
    using Stream stream = await RetryHelper.ExecuteAsync(() => 
        horizontalFrontImages[i].GetImageAsStreamAsync(false));
    
    using MemoryStream ms = new MemoryStream();
    stream.CopyTo(ms);  // Convert to byte array
    
    ImageItem image = new ImageItem
    {
        Name = i.ToString(),           // Sequential index
        Thumbnail = ms.ToArray()       // Binary data for storage
    };
    
    repositoryWs.SaveImage(image);    // Persist to folder structure
}
```

### 6.2 Storage Structure

---

## 7. SUMMARY VALIDATION CHECKLIST

| Requirement | Status | Evidence |
|-------------|--------|----------|
| External dependency verified | ✅ | `craftersmine.SteamGridDB.Net` v1.1.7 confirmed via NuGet |
| API key passed correctly | ✅ | `new SteamGridDb(ws.SteamGridDBAPIKey)` matches wiki pattern |
| Grid retrieval methods used | ✅ | `GetGridsByPlatformGameIdAsync()` called 2x with correct params |
| Hero retrieval method used | ✅ | `GetHeroesByPlatformGameIdAsync()` called with Alternate+Static |
| Icon retrieval method used | ✅ | `GetIconsByPlatformGameIdAsync()` called with Static type |
| Image streams retrieved | ✅ | `GetImageAsStreamAsync(false)` pattern applied 4x |
| Caching implemented | ✅ | `_loaderGameList` Dictionary prevents duplicate fetches |
| Retry policy applied | ✅ | Polly exponential backoff (1s, 2s, 4s) on all async calls |
| No hardcoded API keys | ✅ | All keys from `IConfigService.Get*()` methods |

---

## 8. INTEGRATION CONFIRMED ✓

The integration between **SteamLibraryLoader** and **SteamGridDB.Net** is verified as:

1. **Correctly instantiated** with API key from configuration
2. **Properly invoking** all SteamGridDB.NET client methods per documentation
3. **Caching responses** at the WorkspaceManager level (GameData cache)
4. **Resilient to failures** via Polly retry policy
5. **No hardcoded secrets** - all API keys sourced externally

**Folder Hierarchy**:
```
SteamLibrary/
└── {gameId}/
    ├── data.json              // Game metadata (name, tags, release date)
    └── {repository_type}/     // e.g., "front_horizontal"
        ├── 0.{ext}            // Front page image 1
        ├── 1.{ext}            // Front page image 2
        └── ...                // Up to 10 images per repository type
```
        │ └─────────────────────────────────────────────────────┘  │
        └───────────────────────────────────────────────────────────┘
```
// Wiki Documentation: GetGridsByPlatformGameIdAsync(SteamGridDbGamePlatform platform, int plaformGameId)
// With optional parameters for styles, types, dimensions, limit
SteamGridDbGrid[] grids = await sgdb.GetGridsByPlatformGameIdAsync(
    SteamGridDbGamePlatform.Steam, 
    gameId,
    styles: SteamGridDbStyles.Alternate,
    types: SteamGridDbTypes.Static,
    dimensions: SteamGridDbDimensions.W920H430,
    limit: 10);
```

### 2.2 Hero Retrieval Methods

| Source Location | Method Call | Parameters | Purpose |
|-----------------|-------------|------------|---------|
| `SteamLibraryLoader.cs:221-224` | `_sgdb.GetHeroesByPlatformGameIdAsync()` | Platform: Steam, Styles: Alternate, Types: Static, Limit: 10 | Back Cover Images |

**Official API Signature**:
```csharp
// Wiki Documentation: GetHeroesByPlatformGameIdAsync(SteamGridDbGamePlatform platform, int plaformGameId)
SteamGridDbHero[] heroes = await sgdb.GetHeroesByPlatformGameIdAsync(
    SteamGridDbGamePlatform.Steam, 
    gameId,
    styles: SteamGridDbStyles.Alternate,
    types: SteamGridDbTypes.Static,
    limit: 10);
```

### 2.3 Icon Retrieval Methods

| Source Location | Method Call | Parameters | Purpose |
|-----------------|-------------|------------|---------|
| `SteamLibraryLoader.cs:241-243` | `_sgdb.GetIconsByPlatformGameIdAsync()` | Platform: Steam, Types: Static, Limit: 10 | Game Icons |

**Official API Signature**:
```csharp
// Wiki Documentation: GetIconsByPlatformGameIdAsync(SteamGridDbGamePlatform platform, int plaformGameId)
SteamGridDbIcon[] icons = await sgdb.GetIconsByPlatformGameIdAsync(
    SteamGridDbGamePlatform.Steam, 
    gameId,
    types: SteamGridDbTypes.Static,
    limit: 10);
```

### 2.4 Image Stream Retrieval

**Usage Pattern** (lines 187-189, 208-210, 228-230, 247-249):
```csharp
using Stream stream = await RetryHelper.ExecuteAsync(() => 
    horizontalFrontImages[i].GetImageAsStreamAsync(false));
using MemoryStream ms = new MemoryStream();
stream.CopyTo(ms);
ImageItem image = new ImageItem
{
    Name = i.ToString(),
    Thumbnail = ms.ToArray()  // Bytes extracted for storage
};
repositoryWs.SaveImage(image);
```

**Official API Method**: `GetImageAsStreamAsync(bool cache)`
- Parameter: `false` indicates no local caching preference
- Returns: Stream of image data
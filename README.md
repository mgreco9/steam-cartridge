# Steam Cartridge

A desktop application for creating custom physical Steam game cartridges with NFC support. This project transforms your digital Steam library into a retro-style physical collection, bringing back the charm of classic game cartridges with modern technology.

## 🎯 Project Goal

The Steam Cartridge aims to recreate the nostalgic experience of physical game media in the digital age by:

1. **Physical Collection**: Create realistic-looking Steam cartridge covers
2. **Tangible Gaming Experience**: Hold and display your games physically while still launching from Steam
3. **Personalization**: Customize artwork, labels, and layouts to match your personal style

### How It Works

1. **Design Phase**: Use the built-in visual editor to create custom cartridge cover artwork with your favorite game images
2. **Export Phase**: Generate high-quality PDFs of your custom cartridge designs for printing
3. **NFC Integration**: Write Steam game IDs to NFC cards, creating instant access when scanned
4. **Launch**: Present an NFC card to trigger Steam to launch the associated game

## 📋 Requirements

### System Requirements

- **Operating System**: Windows 10/11, macOS, or Linux (Avalonia cross-platform UI)
- **.NET Runtime**: .NET 8.0 or later (for end users)
- **.NET SDK**: Visual Studio 2022 with .NET 8.0 workload or later (for developers building from source)

### Hardware (Optional but Recommended)

- **NFC Reader/Writer**: Required for NFC card creation (e.g., PN532, EM4100, or USB NFC reader like RC-SWR10)
- **Printer** (optional): For printing cartridge covers
- **Cardstock Paper** (optional): 210gsm or higher recommended for high-quality prints

## 🔑 API Configuration

The application requires external API keys to function properly. These are stored locally on your machine and not synced with any server.

### Required API Keys

| Key | Purpose |
|-----|---------|
| **Steam Web API Key** | Access Steam game data, library information, and artwork |
| **SteamGridDB API Key** | Retrieve high-quality game artwork from the community database |

### Where to Configure

Create or edit `%APPDATA%\SteamCartridge\config.json` on first run. Example structure:

```json
{
    "clientId": "76561198080452425",
    "SteamAPIKey": "YOUR_STEAM_WEB_API_KEY_HERE",
    "SteamGridDBAPIKey": "YOUR_STEAMGRIDDB_API_KEY_HERE",
    "SaveDirectory": "C:\\Users\\YourName\\Documents\\Cartridges"
}
```

### How to Obtain Keys

#### Steam Web API Key
1. Visit https://steamcommunity.com/dev/apikey
2. Log in with your Steam account
3. Agree to the terms and enter any domain name (e.g., localhost)
4. Copy the generated API key

#### SteamGridDB API Key
1. Visit https://www.steamgriddb.com/
2. Create a free account (optional but recommended)
3. Navigate to Settings → Developer Settings
4. Generate an API key

### Field Descriptions

| Config Field | Required | Description |
|--------------|----------|-------------|
| `clientId` | ✓ | Your Steam 64-bit user ID (found in Steam profile URL) |
| `SteamAPIKey` | ✓ | Steam Web API key from above |
| `SteamGridDBAPIKey` | ✗ | SteamGridDB key for artwork (optional but recommended) |
| `SaveDirectory` | ✗ | Directory where cartridge files are saved (defaults to Documents/Cartridges) |

## 📄 Layout Files

Layout defines the visual structure of cartridge covers. Key fields include:

### Root Properties

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `templateId` | string | ✓ | Unique identifier for the template |
| `elements` | array | ✓ | Definition of visual elements |
| `layout` | array | ✓ | Positioning configuration |
| `pdfTemplate` | object | ✗ | PDF export settings (optional) |

### Element Types

**Image Element:**
```json
{
    "type": "image",
    "id": "front",
    "label": "Front Cover",
    "width": 26,      // in cm
    "height": 16.5,   // in cm
    "folder": "Front_Vertical"
}
```

**Text Element:**
```json
{
    "type": "text",
    "id": "title",
    "label": "Title",
    "font": "Arial",
    "fontsize": 180,
    "width": 26,
    "height": 1.5,
    "source": "Name"
}
```

**Blurred Element (derived from another image):**
```json
{
    "type": "blurred",
    "id": "side",
    "label": "Side Panel",
    "blur": 80,
    "width": 3.4,
    "height": 16.5,
    "source": "front"
}
```

### Layout Positioning

```json
{
    "imageId": "front",
    "x": 13,         // position in cm from left edge
    "y": 0,          // position in cm from top edge
    "width": 26,
    "height": 16.5
}
```

### PDF Template Items

**Rectangle:**
```json
{
    "type": "rectangle",
    "x": 0,
    "y": 0,
    "width": 26,
    "height": 16.5
}
```

**Line:**
```json
{
    "type": "line",
    "x1": 13,        // Start X in cm
    "y1": 0,         // Start Y in cm
    "x2": 13,        // End X in cm
    "y2": 16.5,      // End Y in cm
    "color": "#ffffff",
    "thickness": 2
}
```

**Note:** All measurements are in centimeters (cm). The application loads configuration via `ConfigService.Load()` on startup and processes layout files to generate cartridge PDFs for printing.

## 🖥️ Usage Tips

### First Time Setup

1. **Install the application** by downloading the `.exe` installer or building from source
2. **Run for first time** - The app will create a default configuration file
3. **Obtain API keys** (see "How to Obtain Keys" section above)
4. **Edit config.json** with your Steam user ID and API keys
5. **Create cartridge layouts** using the built-in visual editor

### Creating Your First Cartridge

1. Launch the application and let it load your Steam library
2. Select games you want to create cartridge covers for
3. Choose or create a layout template
4. Customize artwork, titles, and images
5. Export as PDF and print on cardstock
6. Write NFC chip using compatible reader (if applicable)

### Troubleshooting

**Steam API calls failing:**
- Verify your Steam user ID is correct (check Steam profile URL)
- Ensure your Steam Web API key has valid permissions
- Check firewall settings if running behind corporate network

**NFC card not detected:**
- Try different USB ports (preferably USB 3.0)
- Install/update NFC driver from manufacturer
- Restart the application after connecting hardware

**PDF exports look distorted:**
- Ensure layout files use correct dimensions (26cm x 16.5cm for standard cartridge size)
- Check printer settings (select "Actual Size" or "100% scale")
- Use high-quality cardstock paper
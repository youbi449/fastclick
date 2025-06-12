# FastClick - Automated Mouse Clicker

FastClick is a Windows automation tool that allows you to perform mouse clicks at predefined screen positions using global hotkeys.

## Features

- **Point Configuration**: Create points with specific screen coordinates
- **Global Hotkeys**: Trigger mouse actions from anywhere on your system
- **Multiple Actions**: Support for left click, right click, double click, mouse down/up, and middle click
- **Multi-Screen Support**: Works with multiple monitor setups
- **Repeat Actions**: Configure repeat count and delays between actions
- **System Tray**: Runs in background with system tray integration
- **Save/Load**: Automatic configuration persistence with import/export functionality

## Requirements

- Windows 10/11
- .NET 8.0 Runtime
- Administrator privileges (required for global hotkeys and mouse automation)

## Installation

1. Download the latest release
2. Extract to a folder of your choice
3. Run `FastClick.exe` as Administrator

## Usage

### Adding a Point

1. Click "Add Point" button
2. Either:
   - Click "Start Capture" and then click anywhere on screen to capture position
   - Click "Get Current Position" to use current mouse cursor position
   - Manually enter X, Y coordinates
3. Enter a name for the point
4. Click OK

### Setting Hotkeys

1. Select a point from the list
2. Click "Set Hotkey" button
3. Press the key combination you want to use (e.g., Ctrl+1, Alt+F1, etc.)
4. Click OK

### Configuring Actions

In the Point Details panel, you can configure:
- **Action**: Type of mouse action (Left Click, Right Click, etc.)
- **Repeat Count**: How many times to perform the action
- **Delay**: Milliseconds to wait between repeated actions
- **Screen**: Which monitor to use for multi-screen setups

### Running in Background

- Click "Minimize to Tray" to run FastClick in the background
- Right-click the system tray icon to show/hide or exit
- The application will ask if you want to minimize to tray when closing

## Global Hotkeys

FastClick registers system-wide hotkeys that work even when the application is not in focus. Common key combinations include:

- `Ctrl + 1`, `Ctrl + 2`, etc.
- `Alt + F1`, `Alt + F2`, etc.
- `Shift + Ctrl + A`, etc.

## Multi-Screen Support

FastClick automatically detects all connected monitors. In the Point Details panel:
- Screen 0 = Primary monitor
- Screen 1, 2, etc. = Additional monitors
- Coordinates are relative to each screen's top-left corner

## Configuration Files

- Configuration is automatically saved to `%APPDATA%/FastClick/fastclick_config.json`
- Use File → Export/Import to backup or share configurations

## Security

FastClick requires administrator privileges to:
- Register global hotkeys
- Send mouse events to other applications
- Access low-level mouse hooks for point capture

The application only performs the actions you configure and does not collect or transmit any data.

## Troubleshooting

### Hotkeys Not Working
- Ensure FastClick is running as Administrator
- Check that another application isn't using the same key combination
- Verify the "FastClick Enabled" checkbox is checked

### Points Not Clicking Correctly
- Verify the coordinates are correct for your screen resolution
- Check the screen index for multi-monitor setups
- Test the point using the "Test" button

### Application Won't Start
- Install .NET 8.0 Runtime if not already installed
- Run as Administrator
- Check Windows Event Viewer for error details

## Building from Source

Requirements:
- Visual Studio 2022 or .NET 8.0 SDK
- Windows 10/11

```bash
git clone https://github.com/yourusername/fastclick.git
cd fastclick
dotnet build
dotnet run
```

## License

MIT License - See LICENSE file for details

## Support

For issues, questions, or feature requests, please create an issue on the GitHub repository.
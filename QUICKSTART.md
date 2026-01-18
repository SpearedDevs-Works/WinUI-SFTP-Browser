# Quick Start Guide

This guide will help you get the WinUI-SFTP-Browser up and running quickly.

## Prerequisites Checklist

Before you begin, ensure you have:

- [ ] Windows 10 (build 17763 or later) or Windows 11
- [ ] Visual Studio 2022 (version 17.0 or later)
- [ ] .NET 8.0 SDK installed
- [ ] The following Visual Studio workloads:
  - [ ] .NET Desktop Development
  - [ ] Universal Windows Platform development

## 5-Minute Setup

### Step 1: Clone the Repository (1 minute)

```bash
git clone https://github.com/SpearedDevs-Works/WinUI-SFTP-Browser.git
cd WinUI-SFTP-Browser
```

### Step 2: Open in Visual Studio (1 minute)

```bash
# Option 1: Double-click the solution file
WinUI-SFTP-Browser.sln

# Option 2: Open from command line (if VS Code is in PATH)
start WinUI-SFTP-Browser.sln
```

### Step 3: Restore NuGet Packages (1 minute)

Visual Studio should automatically restore packages. If not:

1. Right-click on the solution in Solution Explorer
2. Select "Restore NuGet Packages"
3. Wait for the restore to complete

### Step 4: Build the Solution (1 minute)

**Method 1: Using Keyboard**
- Press `Ctrl+Shift+B`

**Method 2: Using Menu**
- Build → Build Solution

**Method 3: Using Command Line**
```bash
dotnet build WinUI-SFTP-Browser.sln --configuration Debug
```

### Step 5: Run the Application (1 minute)

**Method 1: Debug Mode**
- Press `F5` or click the "Play" button

**Method 2: Without Debugging**
- Press `Ctrl+F5`

**Method 3: From Build Output**
- Navigate to `WinUI-SFTP-Browser/bin/x64/Debug/net8.0-windows10.0.19041.0/`
- Double-click `WinUI-SFTP-Browser.exe`

## First Use

### Connecting to an SFTP Server

1. **Launch the application**
2. **Click "New Connection"** in the toolbar
3. **Enter server details**:
   - Host: Your SFTP server address (e.g., `sftp.example.com`)
   - Port: Usually `22` (default)
   - Username: Your SFTP username
   - Password: Your SFTP password
4. **Click "Connect"**

### Browsing Files

- **Navigate folders**: Double-click on a folder
- **Go back**: Click the back arrow (◀) button
- **Go forward**: Click the forward arrow (▶) button
- **Refresh**: Click the refresh button

### Uploading Files

1. **Navigate** to the destination folder
2. **Click "Upload"** in the toolbar
3. **Select files** from your computer
4. **Wait** for upload to complete

### Downloading Files

1. **Select** one or more files/folders
2. **Click "Download"** or right-click → Download
3. **Choose** a local folder
4. **Wait** for download to complete

### Other Operations

- **Delete**: Select items → Click "Delete" → Confirm
- **Rename**: Right-click → Rename → Enter new name
- **New Folder**: Click "New Folder" → Enter name
- **Properties**: Right-click → Properties
- **Sync**: Click "Sync" → Select local folder

## Troubleshooting

### Build Errors

**Problem**: "Unable to find package"
- **Solution**: Restore NuGet packages (Step 3)

**Problem**: "Project targets Windows"
- **Solution**: Ensure you're on Windows, not Linux/macOS

**Problem**: "XAML Compiler error"
- **Solution**: Make sure Windows App SDK is installed via NuGet

### Connection Issues

**Problem**: "Connection failed"
- **Solution**: Check server address, port, and credentials
- **Solution**: Ensure SFTP server is running and accessible
- **Solution**: Check firewall settings

**Problem**: "Authentication failed"
- **Solution**: Verify username and password
- **Solution**: Ensure SFTP user has proper permissions

### Runtime Errors

**Problem**: Application won't start
- **Solution**: Ensure .NET 8.0 runtime is installed
- **Solution**: Check Windows version compatibility (minimum 1809)

**Problem**: UI looks wrong
- **Solution**: Update to Windows 11 for best experience
- **Solution**: Update graphics drivers

## Development Tips

### Modifying the Code

**Adding a new feature**:
1. Create/modify ViewModels for state management
2. Update XAML for UI changes
3. Implement service layer logic if needed
4. Test thoroughly

**Changing UI appearance**:
- Edit `MainWindow.xaml` for layout
- Modify `App.xaml` for global styles
- Update colors in `ResourceDictionary`

### Debugging

**Set breakpoints**:
- Click in the left margin of code editor
- Press `F9` on a line

**Step through code**:
- `F10`: Step over
- `F11`: Step into
- `Shift+F11`: Step out

**View variables**:
- Hover over variables during debugging
- Use "Locals" window (Debug → Windows → Locals)

## Next Steps

After getting the app running:

1. **Read** [ARCHITECTURE.md](ARCHITECTURE.md) to understand the codebase
2. **Review** [CONTRIBUTING.md](CONTRIBUTING.md) if you want to contribute
3. **Check** [IMPLEMENTATION.md](IMPLEMENTATION.md) for feature details
4. **Explore** the code starting with `MainWindow.xaml` and `MainWindowViewModel.cs`

## Getting Help

- **Documentation**: See README.md for detailed documentation
- **Issues**: Check existing issues on GitHub
- **Questions**: Open a new issue with the "question" label

## Quick Reference

| Action | Method |
|--------|--------|
| Build | `Ctrl+Shift+B` |
| Run (Debug) | `F5` |
| Run (No Debug) | `Ctrl+F5` |
| Stop Debugging | `Shift+F5` |
| Restore Packages | Right-click solution → Restore NuGet Packages |
| Clean Build | Build → Clean Solution |
| Rebuild | Build → Rebuild Solution |

## Success!

If you've followed these steps, you should now have a working WinUI-SFTP-Browser application!

The app should look like Windows 11 File Explorer with:
- Modern Mica backdrop
- Rounded corners
- Fluent icons
- Smooth animations

Enjoy browsing your SFTP servers! 🚀

# WinUI-SFTP-Browser

A modern WinUI 3-based SFTP file browser for Windows 11 with a File Explorer-like interface and context menu support for file operations.

## Features

- **Modern Windows 11 UI**: Utilizes WinUI 3 with Mica backdrop for a native Windows 11 look and feel
- **SFTP Connection**: Connect to any SFTP server with username/password authentication
- **File Browser**: Browse remote files and directories with a familiar File Explorer-style interface
- **File Operations**: Upload, download, delete, rename files and folders
- **Context Menu**: Right-click context menu with file operations matching Windows 11 style
- **Folder Management**: Create new folders and navigate through directory structure
- **Sync Functionality**: Synchronize remote folders with local directories
- **Progress Indication**: Visual feedback during file operations
- **Multi-Selection**: Select and operate on multiple files at once

## Technology Stack

- **WinUI 3**: Modern Windows UI framework with Windows App SDK
- **C# .NET 8**: Latest .NET with C# programming language
- **SSH.NET**: Third-party library for SFTP protocol implementation
- **CommunityToolkit.Mvvm**: MVVM helpers for clean architecture

## Prerequisites

To build and run this application, you need:

- Windows 10 version 1809 (build 17763) or later / Windows 11
- Visual Studio 2022 version 17.0 or later with:
  - .NET Desktop Development workload
  - Universal Windows Platform development workload
  - Windows App SDK (installed via NuGet)
- .NET 8.0 SDK or later

## Building the Project

1. Clone the repository:
   ```bash
   git clone https://github.com/SpearedDevs-Works/WinUI-SFTP-Browser.git
   cd WinUI-SFTP-Browser
   ```

2. Open the solution in Visual Studio 2022:
   ```bash
   WinUI-SFTP-Browser.sln
   ```

3. Restore NuGet packages:
   - Right-click on the solution in Solution Explorer
   - Select "Restore NuGet Packages"

4. Build the solution:
   - Press `Ctrl+Shift+B` or
   - Select "Build > Build Solution" from the menu

5. Run the application:
   - Press `F5` or
   - Select "Debug > Start Debugging"

## Project Structure

```
WinUI-SFTP-Browser/
├── WinUI-SFTP-Browser/          # Main project directory
│   ├── App.xaml                 # Application definition
│   ├── App.xaml.cs              # Application code-behind
│   ├── MainWindow.xaml          # Main window UI
│   ├── MainWindow.xaml.cs       # Main window code-behind
│   ├── Assets/                  # Application assets (icons, images)
│   ├── Dialogs/                 # Dialog windows
│   │   ├── ConnectionDialog.*   # SFTP connection dialog
│   │   ├── TextInputDialog.*    # Generic text input dialog
│   │   └── PropertiesDialog.*   # File properties dialog
│   ├── Models/                  # Data models
│   │   └── SftpConnectionInfo.cs
│   ├── Services/                # Business logic services
│   │   └── SftpService.cs       # SFTP operations service
│   ├── ViewModels/              # MVVM view models
│   │   ├── MainWindowViewModel.cs
│   │   └── FileItemViewModel.cs
│   ├── Package.appxmanifest     # App package manifest
│   └── WinUI-SFTP-Browser.csproj # Project file
└── WinUI-SFTP-Browser.sln       # Solution file
```

## Usage

1. **Connect to SFTP Server**:
   - Click "New Connection" button in the toolbar
   - Enter server details (host, port, username, password)
   - Click "Connect"

2. **Browse Files**:
   - Navigate through folders by double-clicking on them
   - Use back/forward buttons to navigate history
   - Current path is shown in the address bar

3. **Upload Files**:
   - Click "Upload" button in the toolbar
   - Select files from your local machine
   - Files will be uploaded to the current remote directory

4. **Download Files**:
   - Select one or more files/folders
   - Click "Download" button or right-click and select "Download"
   - Choose a local folder to save the files

5. **Delete Files**:
   - Select files/folders to delete
   - Click "Delete" button or right-click and select "Delete"
   - Confirm the deletion

6. **Create New Folder**:
   - Click "New Folder" button
   - Enter the folder name
   - Folder will be created in the current directory

7. **Rename Files**:
   - Right-click on a file or folder
   - Select "Rename"
   - Enter the new name

8. **Sync Folder**:
   - Navigate to a remote folder
   - Click "Sync" button
   - Select a local folder
   - All files from the remote folder will be downloaded to the local folder

## Windows 11 Integration

The application mimics Windows 11 File Explorer's design:
- **Mica backdrop**: Translucent background that adapts to system theme
- **Rounded corners**: Modern UI with rounded window corners
- **Context menu**: Right-click menu with Windows 11-style icons
- **Command bar**: Toolbar with commonly used operations
- **Status bar**: Shows connection status and file operation progress
- **Column headers**: Sortable columns (Name, Date Modified, Type, Size)
- **File icons**: Different icons for various file types and folders

## Security Considerations

- **Password Storage**: This demo application does not persist connection credentials. For production use, implement secure credential storage using Windows Credential Manager
- **Connection Encryption**: SFTP protocol provides encrypted communication
- **SSL/TLS**: SSH.NET library handles secure connections

## Third-Party Libraries

- **SSH.NET** (2024.2.0): MIT License - SFTP/SSH protocol implementation
- **CommunityToolkit.Mvvm** (8.3.2): MIT License - MVVM helpers
- **Microsoft.WindowsAppSDK** (1.6.x): Windows App SDK for WinUI 3

## License

See LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

## Roadmap

Future enhancements may include:
- [ ] SSH key authentication support
- [ ] Drag-and-drop file uploads
- [ ] File preview functionality
- [ ] Search and filter capabilities
- [ ] Multiple tab support
- [ ] Bookmarks for frequently accessed servers
- [ ] Transfer queue and parallel uploads/downloads
- [ ] File comparison and conflict resolution
- [ ] Dark/Light theme switching
- [ ] Localization support


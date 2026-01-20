# Update Installation Feature

## Overview

The SFTP Browser application now includes an automated update installation system that allows users to check for updates, download them, and install them directly from the Settings page.

## Features

### 1. Check for Updates
- Click the "Check for Updates" button to query GitHub for the latest release
- The system displays whether a new version is available or if you're running the latest version
- Compares version numbers to determine if an update is necessary
- Ignores pre-release versions by default

### 2. Download Update
- Once a new version is detected, a "Download Update" button appears
- Click to download the latest installer
- A progress bar displays the download progress in real-time
- Files are downloaded to a temporary location: `%TEMP%\SFTP-Browser-Updates`

### 3. Install Update
- After downloading, the "Install Update" button becomes available
- Click to launch the installer with administrator privileges
- The installer will handle the installation process
- The application may require a restart after installation

## Technical Details

### UpdateCheckService
Located in `Services/UpdateCheckService.cs`, this service handles:
- **CheckForUpdatesAsync()**: Queries GitHub API for releases
- **DownloadReleaseAsync()**: Downloads the latest release installer
  - Supports .exe, .msi, and .msix file formats
  - Reports download progress via event
  - Stores files in system temp directory
- **InstallUpdateAsync()**: Executes the installer with admin rights
  - Waits for completion with 5-minute timeout
  - Returns success status based on exit code
- **CleanupDownloads()**: Removes old downloaded installers

### SettingsViewModel
Extended with update-related properties:
- `AppVersion`: Current application version
- `IsCheckingForUpdates`: Indicates active update check
- `UpdateCheckMessage`: User-facing status messages
- `IsDownloadingUpdate`: Indicates active download
- `UpdateDownloadProgress`: Download percentage (0-100)
- `UpdateAvailable`: True when newer version found
- `UpdateDownloaded`: True when installer is ready to install

### Commands
- `CheckForUpdatesCommand`: Initiates version check
- `DownloadAndInstallUpdateCommand`: Downloads the installer
- `InstallUpdateCommand`: Launches the installer

## User Interface

The Settings page displays:
1. **About Section**
   - Current application version
   - "Check for Updates" button

2. **Update Status**
   - Conditional buttons based on update availability
   - Progress bar during downloads
   - Status messages for user guidance

3. **Button Visibility**
   - "Check for Updates" - Always visible
   - "Download Update" - Visible when update available
   - "Install Update" - Visible when download complete

## Requirements

- Internet connection for checking updates
- Write permissions to system temp directory
- Administrator privileges for installation (requested automatically)
- GitHub repository: `SpearedDevs-Works/WinUI-SFTP-Browser`

## Error Handling

The feature includes comprehensive error handling:
- Network failures during download
- Missing or invalid installer assets
- Failed installation execution
- File system errors during cleanup

All errors are displayed to the user with descriptive messages.

## Automatic Cleanup

Downloaded installers are automatically cleaned up:
- When navigating away from the Settings page
- Periodically on application startup
- Manual cleanup via `CleanupOldUpdates()` method

## Notes

- The application does NOT auto-update without user confirmation
- Updates are only checked when explicitly requested
- No background update checking is performed
- Users have full control over when to download and install updates

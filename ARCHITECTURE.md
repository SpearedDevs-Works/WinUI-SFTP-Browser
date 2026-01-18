# WinUI-SFTP-Browser Architecture

This document describes the architecture and design decisions of the WinUI-SFTP-Browser application.

## Overview

WinUI-SFTP-Browser is a Windows 11-style SFTP file browser built using WinUI 3, following the MVVM (Model-View-ViewModel) architectural pattern.

## Architecture Layers

### 1. Presentation Layer (Views)

**Location**: `*.xaml` and `*.xaml.cs` files

The presentation layer consists of XAML-based user interfaces:

- **MainWindow.xaml**: Main application window with File Explorer-like UI
  - Command bar with common operations
  - Address bar with navigation controls
  - File list view with column headers
  - Status bar showing connection and operation status
  
- **Dialogs**:
  - `ConnectionDialog.xaml`: SFTP server connection configuration
  - `TextInputDialog.xaml`: Generic text input for rename, new folder operations
  - `PropertiesDialog.xaml`: Display file/folder properties

### 2. ViewModel Layer

**Location**: `ViewModels/` directory

ViewModels handle presentation logic and state management:

- **MainWindowViewModel**: 
  - Manages application state (connection status, current path, file list)
  - Handles navigation history (back/forward)
  - Coordinates file operations through the service layer
  - Implements INotifyPropertyChanged via CommunityToolkit.Mvvm
  
- **FileItemViewModel**:
  - Represents individual files/folders in the UI
  - Provides formatted display properties (icon, size, type, date)
  - Determines appropriate icons based on file type

### 3. Service Layer

**Location**: `Services/` directory

Services encapsulate business logic and external communication:

- **SftpService**:
  - Manages SFTP connection lifecycle
  - Implements file operations (upload, download, delete, rename)
  - Handles directory operations (list, create, delete recursive)
  - Wraps SSH.NET library functionality
  - Provides async APIs for all I/O operations

### 4. Model Layer

**Location**: `Models/` directory

Data models represent domain entities:

- **SftpConnectionInfo**: SFTP server connection parameters
  - Host, Port, Username, Password
  - Could be extended for SSH key authentication

## Design Patterns

### MVVM Pattern

The application strictly follows MVVM:
- **Views** contain no business logic, only UI structure
- **ViewModels** handle all presentation logic and state
- **Models** are pure data structures
- **Data Binding** connects Views to ViewModels

### Dependency Injection

While the current implementation uses direct instantiation, the architecture supports DI:
- Services are abstracted as classes with clear interfaces
- ViewModels could easily accept services via constructor injection

### Repository Pattern

The SftpService acts as a repository for remote file operations:
- Abstracts SFTP protocol details from ViewModels
- Provides a clean API for file operations
- Could be interfaced for testing or multiple implementations

### Command Pattern

UI commands use event handlers that delegate to ViewModel methods:
- Click handlers in code-behind invoke ViewModel operations
- Could be migrated to ICommand implementations for pure MVVM

## Data Flow

### Connection Flow
```
User clicks "New Connection"
  → MainWindow shows ConnectionDialog
  → User enters credentials
  → MainWindow.ConnectAsync() calls ViewModel.ConnectAsync()
  → ViewModel calls SftpService.ConnectAsync()
  → SftpService establishes SSH connection
  → ViewModel loads root directory
  → UI updates with file list
```

### File Operation Flow
```
User selects files and clicks "Download"
  → MainWindow.Download_Click() opens folder picker
  → Calls ViewModel.DownloadItemsAsync()
  → ViewModel calls SftpService.DownloadItemsAsync()
  → SftpService downloads each file/folder recursively
  → ViewModel updates status
  → UI shows completion
```

### Navigation Flow
```
User double-clicks folder
  → MainWindow.FileListView_DoubleTapped()
  → Calls ViewModel.NavigateToAsync()
  → ViewModel pushes current path to back history
  → Calls SftpService.ListDirectoryAsync()
  → ViewModel updates Items collection
  → ListView refreshes automatically via data binding
```

## Key Components

### SftpService

**Responsibilities**:
- SFTP connection management
- File transfer operations
- Directory navigation
- Error handling for network operations

**Key Methods**:
- `ConnectAsync()`: Establish SFTP connection
- `ListDirectoryAsync()`: Retrieve directory contents
- `UploadFilesAsync()`: Upload files to remote server
- `DownloadItemsAsync()`: Download files/folders recursively
- `DeleteItemsAsync()`: Delete remote items
- `CreateDirectoryAsync()`: Create remote directory
- `RenameItemAsync()`: Rename remote item
- `SyncFolderAsync()`: Synchronize remote to local

### MainWindowViewModel

**Responsibilities**:
- Application state management
- User interaction coordination
- Navigation history
- Status and progress updates

**Observable Properties**:
- `Items`: Observable collection of files/folders
- `CurrentPath`: Current directory path
- `IsConnected`: Connection status
- `IsLoading`: Operation in progress indicator
- `HasSelection`: File selection state
- `StatusText`: Status bar message

### FileItemViewModel

**Responsibilities**:
- File metadata presentation
- Type-based icon selection
- Size formatting
- Date formatting

**Properties**:
- `Name`, `FullPath`, `Type`, `Size`, `DateModified`
- `Icon`: Segoe UI icon glyph for file type
- `IsDirectory`: Directory flag

## Windows 11 UI Integration

### Mica Backdrop
- Applied via `SystemBackdrop` property in MainWindow
- Provides translucent, theme-aware background

### Title Bar Customization
- Extended content into title bar with `ExtendsContentIntoTitleBar`
- Custom title bar with app icon and title

### Fluent Design
- Command bar with intuitive icons
- Consistent spacing and sizing
- Windows 11 color scheme
- Hover and selection effects

### Context Menu
- Windows 11-style MenuFlyout
- Standard file operation commands
- Icon-labeled menu items

## Third-Party Dependencies

### SSH.NET (Renci.SshNet)
- Purpose: SFTP protocol implementation
- Why: Mature, well-maintained, widely used
- License: MIT

### CommunityToolkit.Mvvm
- Purpose: MVVM helpers (ObservableObject, ObservableProperty)
- Why: Reduces boilerplate code, Microsoft-supported
- License: MIT

### Microsoft.WindowsAppSDK
- Purpose: WinUI 3 framework
- Why: Modern Windows UI framework
- License: MIT

## Error Handling

### Service Layer
- Methods throw exceptions for unexpected errors
- Network errors, authentication failures propagate up

### ViewModel Layer
- Try-catch blocks wrap service calls
- Updates StatusText with user-friendly error messages
- Maintains application state on errors

### View Layer
- ContentDialog for user confirmations
- Visual feedback for loading states
- Disabled controls during operations

## Performance Considerations

### Async/Await
- All I/O operations are asynchronous
- Prevents UI freezing during file transfers
- Uses Task.Run for CPU-bound operations within async context

### Lazy Loading
- Directories loaded on-demand when navigated
- File list cleared and repopulated on navigation

### Observable Collections
- ObservableCollection auto-updates UI
- Efficient for moderate file counts (<1000 files)

## Security Considerations

### Credential Handling
- Passwords stored only in memory during session
- No credential persistence (future enhancement needed)
- Uses SSH.NET's secure connection implementation

### File Path Validation
- Should validate paths to prevent directory traversal
- Currently relies on SSH.NET's validation

## Future Architecture Enhancements

### Testing
- Extract interfaces from services for mocking
- Add unit tests for ViewModels
- Add integration tests for SftpService

### Dependency Injection
- Use Microsoft.Extensions.DependencyInjection
- Register services in App.xaml.cs
- Inject into ViewModels and Views

### Credential Storage
- Integrate Windows Credential Manager
- Support multiple saved connections
- Implement SSH key authentication

### Progress Tracking
- IProgress<T> for upload/download progress
- Real-time transfer speed display
- Cancellation token support

### Caching
- Cache directory listings
- Invalidate on operations
- Reduce network round-trips

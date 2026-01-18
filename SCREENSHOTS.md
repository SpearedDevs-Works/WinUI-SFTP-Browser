# UI Screenshots and Mockups

This document describes the expected UI appearance when the application is built and run on Windows 11.

## Main Window

### Layout Structure

The main window is divided into 5 main sections (from top to bottom):

1. **Title Bar** (48px height)
   - App icon (16x16) on the left
   - "SFTP Browser" title
   - Window controls (minimize, maximize, close) on the right
   - Uses Windows 11 Mica backdrop

2. **Command Bar** (auto height)
   - New Connection button (icon: ➕)
   - Refresh button (icon: 🔄)
   - Separator
   - Upload button (icon: ⬆️)
   - Download button (icon: ⬇️)
   - Delete button (icon: 🗑️)
   - Separator
   - New Folder button (icon: 📁)
   - Sync button (icon: 🔄)
   - All buttons show icon + label

3. **Address Bar** (auto height, ~8px padding)
   - Back button (◀)
   - Forward button (▶)
   - Path text box showing current remote path
   - Connect button

4. **File List Area** (fills remaining space)
   - Column headers:
     - Checkbox/Icon column (40px)
     - Name (2* width)
     - Date Modified (1* width)
     - Type (1* width)
     - Size (1* width)
   - List of files and folders:
     - Folder icon (📁) for directories
     - File type icons for files
     - Alternating row background (subtle)
     - Selection highlight (Windows 11 accent color)
     - Hover effect
   - Empty state (when not connected):
     - Large folder icon (64px) centered
     - "No connection" heading
     - "Connect to an SFTP server to browse files" subtext

5. **Status Bar** (32px height)
   - Status message on the left (e.g., "Ready", "12 item(s)")
   - Connection status on the right (e.g., "Connected to example.com", "Not connected")

### Color Scheme (Windows 11)

- **Background**: Mica material (translucent, adapts to wallpaper)
- **Foreground**: System text color (black in light mode, white in dark mode)
- **Accent**: System accent color (user's chosen color)
- **Borders**: Subtle gray (#E0E0E0 in light mode)
- **Command Bar**: Layer fill color
- **Selection**: Accent color with transparency

### Typography

- **Title**: Caption text style (12pt)
- **Command labels**: Body text style (14pt)
- **File names**: Body text style (14pt)
- **Column headers**: Body text style, SemiBold (14pt)
- **Secondary text**: TextFillColorSecondaryBrush (dates, sizes, types)
- **Status bar**: Small text (12pt)

## Connection Dialog

Centered modal dialog with:
- **Title**: "Connect to SFTP Server"
- **Content**:
  - Host text box (placeholder: "example.com or 192.168.1.1")
  - Port number box (default: 22, with spinner buttons)
  - Username text box
  - Password box (masked input)
- **Buttons**:
  - Primary: "Connect" (accent color)
  - Secondary: "Cancel"

## Context Menu

Right-click menu appearing near cursor with:
- Download (icon: ⬇️)
- Delete (icon: 🗑️)
- Rename (icon: ✏️)
- Separator
- Properties (icon: ℹ️)

Menu has:
- Rounded corners (Windows 11 style)
- Mica backdrop
- Smooth shadow
- Hover highlights

## Other Dialogs

### Delete Confirmation
- **Title**: "Delete items?"
- **Content**: "Are you sure you want to delete X item(s)?"
- **Buttons**: Delete (primary, destructive), Cancel (close button)

### New Folder
- **Title**: "New Folder"
- **Content**: 
  - Message: "Enter folder name:"
  - Text input box
- **Buttons**: OK (primary), Cancel

### Rename
- **Title**: "Rename"
- **Content**:
  - Message: "Enter new name:"
  - Text input box (pre-filled with current name)
- **Buttons**: OK (primary), Cancel

### Properties
- **Title**: "Properties"
- **Content**: Grid with labels and values:
  - Name: [file/folder name]
  - Type: [File folder / file type]
  - Path: [full remote path]
  - Size: [formatted size or "Directory"]
  - Modified: [formatted date]
- **Buttons**: Close

## File/Folder Icons

Uses Segoe MDL2 Assets font for icons:

### Folders
- 📁 (U+E8B7) - Standard folder

### Files by Type
- 📄 (U+E8A5) - Generic document (default)
- 🖼️ (U+EB9F) - Images (.jpg, .png, .gif, .bmp)
- 🎬 (U+E8B2) - Videos (.mp4, .avi, .mkv, .mov)
- 🎵 (U+E8D6) - Audio (.mp3, .wav, .flac)
- 📦 (U+E8B5) - Archives (.zip, .rar, .7z, .tar)
- ⚙️ (U+E756) - Executables (.exe, .msi)
- 💻 (U+E943) - Code files (.cs, .cpp, .java, .py, .js)

## Loading States

### While Loading Directory
- Progress ring (50x50px) centered
- "Loading [path]..." in status bar
- File list disabled/dimmed

### During File Operations
- Progress indication in status bar
- Relevant buttons disabled
- Loading message (e.g., "Uploading 3 file(s)...")

## Responsive Behavior

### Window Resizing
- Minimum width: 800px
- Minimum height: 600px
- File list scrolls vertically when content exceeds height
- Column widths proportional to window width

### Selection States
- No selection: Download/Delete buttons disabled
- Single selection: All operations enabled
- Multiple selection: Bulk operations enabled
- Folder double-click: Navigate into folder
- File double-click: (future: preview/download)

## Accessibility Features

- High contrast mode support
- Keyboard navigation support (Tab, Arrow keys, Enter)
- Screen reader support through XAML automation properties
- Focus indicators on all interactive elements

## Animation and Transitions

- Smooth fade-in when loading files
- Subtle hover animations on buttons
- Dialog open/close animations (Windows 11 standard)
- Context menu slide-in animation

## Expected Visual Appearance Notes

When built on Windows 11:
1. The Mica backdrop will show through the window background, creating a modern, translucent effect
2. Rounded corners on the window match Windows 11 style
3. Title bar blends seamlessly with window content
4. Icons use Windows 11's Segoe MDL2 Assets or Segoe Fluent Icons
5. Colors adapt to user's theme (light/dark mode)
6. Accent colors follow user's system preferences

This application should look nearly identical to Windows 11 File Explorer in terms of:
- Layout and structure
- Color scheme and theming
- Icon usage
- Control styling
- Animation and interactions

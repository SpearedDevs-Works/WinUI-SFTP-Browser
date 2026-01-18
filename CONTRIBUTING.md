# Contributing to WinUI-SFTP-Browser

Thank you for your interest in contributing to WinUI-SFTP-Browser! This document provides guidelines and instructions for contributing to the project.

## Code of Conduct

Please be respectful and constructive in all interactions with the project and community members.

## How to Contribute

### Reporting Bugs

If you find a bug, please create an issue with:
- A clear, descriptive title
- Steps to reproduce the issue
- Expected behavior
- Actual behavior
- Screenshots (if applicable)
- Environment details (Windows version, .NET version, etc.)

### Suggesting Enhancements

Enhancement suggestions are welcome! Please create an issue with:
- A clear description of the enhancement
- Use cases or examples
- Why this would be useful to users
- Any implementation ideas you have

### Pull Requests

1. **Fork the repository** and create your branch from `main`
2. **Make your changes** following the coding standards below
3. **Test your changes** thoroughly
4. **Update documentation** if needed
5. **Submit a pull request** with a clear description of the changes

## Development Setup

### Prerequisites

- Windows 10 (1809+) or Windows 11
- Visual Studio 2022 with:
  - .NET Desktop Development workload
  - Universal Windows Platform development workload
- .NET 8.0 SDK
- Git

### Setting Up the Development Environment

1. Clone your fork:
   ```bash
   git clone https://github.com/YOUR-USERNAME/WinUI-SFTP-Browser.git
   cd WinUI-SFTP-Browser
   ```

2. Open the solution in Visual Studio 2022:
   ```bash
   WinUI-SFTP-Browser.sln
   ```

3. Restore NuGet packages and build the solution

## Coding Standards

### C# Code Style

- Follow [Microsoft's C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and concise
- Use async/await for I/O operations

### XAML Style

- Follow WinUI 3 best practices
- Use proper indentation (4 spaces)
- Group related properties together
- Use meaningful names for UI elements
- Leverage data binding where appropriate

### Architecture

This project follows the MVVM (Model-View-ViewModel) pattern:

- **Models**: Data structures (e.g., `SftpConnectionInfo`)
- **Views**: XAML files (e.g., `MainWindow.xaml`)
- **ViewModels**: Business logic and state management (e.g., `MainWindowViewModel`)
- **Services**: Reusable business logic (e.g., `SftpService`)

### Naming Conventions

- **Classes**: PascalCase (e.g., `FileItemViewModel`)
- **Methods**: PascalCase (e.g., `ConnectAsync`)
- **Properties**: PascalCase (e.g., `CurrentPath`)
- **Private fields**: _camelCase with underscore prefix (e.g., `_sftpService`)
- **Parameters**: camelCase (e.g., `connectionInfo`)
- **XAML elements**: PascalCase with descriptive names (e.g., `FileListView`)

## Testing

While this project doesn't currently have automated tests, please manually test your changes:

1. **Connection**: Test connecting to an SFTP server
2. **Navigation**: Test navigating through directories
3. **File Operations**: Test upload, download, delete, rename
4. **Error Handling**: Test with invalid inputs and network issues
5. **UI**: Verify the UI looks correct on different window sizes and themes

## Commit Messages

Write clear, concise commit messages:
- Use present tense ("Add feature" not "Added feature")
- Use imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit the first line to 72 characters
- Reference issues and pull requests when relevant

Examples:
```
Add SSH key authentication support

- Implement private key loading
- Add UI for key file selection
- Update connection dialog
Fixes #123
```

## Pull Request Process

1. Ensure your code builds without warnings or errors
2. Update the README.md if you're adding features or changing behavior
3. Follow the PR template (if provided)
4. Link related issues in the PR description
5. Be responsive to feedback and questions

## Questions?

If you have questions about contributing, feel free to:
- Open an issue with the "question" label
- Reach out to the maintainers

Thank you for contributing to WinUI-SFTP-Browser!

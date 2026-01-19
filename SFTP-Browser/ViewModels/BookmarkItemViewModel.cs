using CommunityToolkit.Mvvm.ComponentModel;
using SFTP_Browser.Models;

namespace SFTP_Browser.ViewModels;

public sealed partial class BookmarkItemViewModel : ObservableObject
{
    public BookmarkItemViewModel(SftpBookmarkModel model)
    {
        Model = model;
    }

    public SftpBookmarkModel Model { get; }

    public string DisplayName => Model.ToString();
}

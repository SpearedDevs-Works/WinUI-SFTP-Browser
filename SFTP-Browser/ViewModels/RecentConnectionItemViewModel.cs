using CommunityToolkit.Mvvm.ComponentModel;
using SFTP_Browser.Models;

namespace SFTP_Browser.ViewModels;

public sealed partial class RecentConnectionItemViewModel : ObservableObject
{
    public RecentConnectionItemViewModel(SftpRecentConnectionModel model)
    {
        Model = model;
    }

    public SftpRecentConnectionModel Model { get; }

    public string DisplayName => Model.ToString();
}

using Microsoft.UI.Xaml.Controls;

namespace WinUI_SFTP_Browser;

public sealed partial class PropertiesDialog : ContentDialog
{
    public PropertiesDialog(FileItemViewModel item)
    {
        this.InitializeComponent();

        NameTextBlock.Text = item.Name;
        TypeTextBlock.Text = item.Type;
        PathTextBlock.Text = item.FullPath;
        SizeTextBlock.Text = item.IsDirectory ? "Directory" : item.Size;
        ModifiedTextBlock.Text = item.DateModified;
    }
}

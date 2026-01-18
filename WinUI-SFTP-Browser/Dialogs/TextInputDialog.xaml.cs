using Microsoft.UI.Xaml.Controls;

namespace WinUI_SFTP_Browser;

public sealed partial class TextInputDialog : ContentDialog
{
    public string InputText
    {
        get => InputTextBox.Text;
        set => InputTextBox.Text = value;
    }

    public string Message
    {
        get => MessageTextBlock.Text;
        set => MessageTextBlock.Text = value;
    }

    public TextInputDialog()
    {
        this.InitializeComponent();
    }
}

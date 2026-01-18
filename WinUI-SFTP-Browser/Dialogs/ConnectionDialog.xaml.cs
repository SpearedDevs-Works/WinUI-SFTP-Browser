using Microsoft.UI.Xaml.Controls;

namespace WinUI_SFTP_Browser;

public sealed partial class ConnectionDialog : ContentDialog
{
    public SftpConnectionInfo? ConnectionInfo { get; private set; }

    public ConnectionDialog()
    {
        this.InitializeComponent();
        this.PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(HostTextBox.Text))
        {
            args.Cancel = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
        {
            args.Cancel = true;
            return;
        }

        ConnectionInfo = new SftpConnectionInfo
        {
            Host = HostTextBox.Text.Trim(),
            Port = (int)PortNumberBox.Value,
            Username = UsernameTextBox.Text.Trim(),
            Password = PasswordBox.Password
        };
    }
}

namespace SFTP_Browser.Models;

public enum TransferStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Canceled = 4,
}

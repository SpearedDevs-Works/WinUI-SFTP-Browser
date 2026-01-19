using System;
using CommunityToolkit.Mvvm.ComponentModel;
using SFTP_Browser.Models;

namespace SFTP_Browser.ViewModels;

public sealed partial class TransferItemViewModel : ObservableObject
{
    public TransferItemViewModel(TransferItemModel model)
    {
        Model = model;
        DisplayName = System.IO.Path.GetFileName(model.SourcePath);
    }

    public TransferItemModel Model { get; }

    public string DisplayName { get; }

    [ObservableProperty]
    private TransferStatus _status = TransferStatus.Pending;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusText = "Pending";

    public void SetRunning()
    {
        Status = TransferStatus.Running;
        StatusText = "Running";
    }

    public void SetCompleted()
    {
        Progress = 1;
        Status = TransferStatus.Completed;
        StatusText = "Completed";
    }

    public void SetFailed(string message)
    {
        Status = TransferStatus.Failed;
        StatusText = message;
    }
}

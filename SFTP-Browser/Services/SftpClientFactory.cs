using System;
using System.IO;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using SFTP_Browser.Models;

#nullable enable

namespace SFTP_Browser.Services;

public static class SftpClientFactory
{
    public static SftpClient CreateAndConnect(SFTPConnectionModel model)
    {
        var info = CreateConnectionInfo(model);
        var client = new SftpClient(info)
        {
            OperationTimeout = TimeSpan.FromSeconds(60)
        };

        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(15);
        client.Connect();

        if (!client.IsConnected)
            throw new InvalidOperationException("Failed to connect.");

        return client;
    }

    private static ConnectionInfo CreateConnectionInfo(SFTPConnectionModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Host))
            throw new ArgumentException("Host is required.", nameof(model));
        if (string.IsNullOrWhiteSpace(model.Username))
            throw new ArgumentException("Username is required.", nameof(model));

        return model.AuthenticationMode switch
        {
            SftpAuthenticationMode.Password => CreatePasswordConnectionInfo(model),
            SftpAuthenticationMode.PrivateKey => CreatePrivateKeyConnectionInfo(model),
            _ => throw new NotSupportedException($"Authentication mode '{model.AuthenticationMode}' is not supported.")
        };
    }

    private static ConnectionInfo CreatePasswordConnectionInfo(SFTPConnectionModel model)
    {
        if (string.IsNullOrEmpty(model.Password))
            throw new ArgumentException("Password is required.", nameof(model));

        var auth = new PasswordAuthenticationMethod(model.Username, model.Password);
        return new ConnectionInfo(model.Host, model.Port, model.Username, auth);
    }

    private static ConnectionInfo CreatePrivateKeyConnectionInfo(SFTPConnectionModel model)
    {
        if (string.IsNullOrWhiteSpace(model.PrivateKeyPath))
            throw new ArgumentException("Private key path is required.", nameof(model));
        if (!File.Exists(model.PrivateKeyPath))
            throw new FileNotFoundException("Private key file not found.", model.PrivateKeyPath);

        using var keyStream = File.OpenRead(model.PrivateKeyPath);

        PrivateKeyFile keyFile;
        try
        {
            keyFile = string.IsNullOrEmpty(model.PrivateKeyPassphrase)
                ? new PrivateKeyFile(keyStream)
                : new PrivateKeyFile(keyStream, model.PrivateKeyPassphrase);
        }
        catch (SshException ex)
        {
            throw new InvalidOperationException("Failed to load private key. If it is encrypted, provide the passphrase.", ex);
        }

        var auth = new PrivateKeyAuthenticationMethod(model.Username, keyFile);
        return new ConnectionInfo(model.Host, model.Port, model.Username, auth);
    }
}

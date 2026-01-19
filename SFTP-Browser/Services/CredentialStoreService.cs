using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace SFTP_Browser.Services;

public sealed class CredentialStoreService
{
    private const string TargetPrefix = "SFTP-Browser";

    public Task SavePasswordAsync(string host, int port, string username, string password, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host is required.", nameof(host));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));

        var target = BuildTarget(host, port, username);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytes = Encoding.Unicode.GetBytes(password ?? string.Empty);

            var credential = new CREDENTIAL
            {
                Type = CRED_TYPE.GENERIC,
                TargetName = target,
                UserName = username,
                CredentialBlobSize = (uint)bytes.Length,
                Persist = CRED_PERSIST.LOCAL_MACHINE,
            };

            var blob = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, blob, bytes.Length);
                credential.CredentialBlob = blob;

                if (!CredWrite(ref credential, 0))
                    throw new InvalidOperationException($"CredWrite failed: {Marshal.GetLastWin32Error()}");
            }
            finally
            {
                Marshal.FreeHGlobal(blob);
            }
        }, cancellationToken);
    }

    public Task<string?> TryGetPasswordAsync(string host, int port, string username, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username))
            return Task.FromResult<string?>(null);

        var target = BuildTarget(host, port, username);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!CredRead(target, CRED_TYPE.GENERIC, 0, out var pcred))
                return (string?)null;

            try
            {
                var cred = Marshal.PtrToStructure<CREDENTIAL>(pcred);
                if (cred.CredentialBlob == IntPtr.Zero || cred.CredentialBlobSize == 0)
                    return string.Empty;

                var bytes = new byte[cred.CredentialBlobSize];
                Marshal.Copy(cred.CredentialBlob, bytes, 0, bytes.Length);
                return Encoding.Unicode.GetString(bytes);
            }
            finally
            {
                CredFree(pcred);
            }
        }, cancellationToken);
    }

    public Task DeleteAsync(string host, int port, string username, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username))
            return Task.CompletedTask;

        var target = BuildTarget(host, port, username);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            CredDelete(target, CRED_TYPE.GENERIC, 0);
        }, cancellationToken);
    }

    private static string BuildTarget(string host, int port, string username)
        => $"{TargetPrefix}:{username}@{host}:{port}";

    private enum CRED_TYPE : uint
    {
        GENERIC = 1,
    }

    private enum CRED_PERSIST : uint
    {
        SESSION = 1,
        LOCAL_MACHINE = 2,
        ENTERPRISE = 3,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public CRED_TYPE Type;
        public string TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public CRED_PERSIST Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, CRED_TYPE type, uint reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDelete(string target, CRED_TYPE type, uint flags);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern void CredFree([In] IntPtr cred);
}

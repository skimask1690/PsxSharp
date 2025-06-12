using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;

class Program
{
    // Token access rights
    private const uint TOKEN_QUERY = 0x8;
    private const uint TOKEN_DUPLICATE = 0x2;
    private const uint TOKEN_ASSIGN_PRIMARY = 0x1;
    private const uint TOKEN_ADJUST_DEFAULT = 0x80;
    private const uint TOKEN_ADJUST_SESSIONID = 0x100;

    // For DuplicateTokenEx
    private const int SecurityImpersonation = 2;
    private const int TokenPrimary = 1;

    [DllImport("advapi32", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32", SetLastError = true)]
    private static extern bool DuplicateTokenEx(
        IntPtr hExistingToken,
        uint dwDesiredAccess,
        IntPtr lpTokenAttributes,
        int ImpersonationLevel,
        int TokenType,
        out IntPtr phNewToken);

    [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessWithTokenW(
        IntPtr hToken,
        uint dwLogonFlags,
        string lpApplicationName,
        string lpCommandLine,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;
        public IntPtr lpReserved;
        public IntPtr lpDesktop;
        public IntPtr lpTitle;
        public uint dwX, dwY, dwXSize, dwYSize;
        public uint dwXCountChars, dwYCountChars;
        public uint dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    static void Main(string[] args)
    {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            MessageBox.Show("Run as administrator.", "Admin Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string command = args.Length > 0 ? string.Join(" ", args) : "cmd";

        IntPtr primaryToken = IntPtr.Zero;

        foreach (var proc in Process.GetProcesses())
        {
            IntPtr hToken = IntPtr.Zero;

            try
            {
                if (!OpenProcessToken(proc.Handle, TOKEN_QUERY | TOKEN_DUPLICATE, out hToken))
                    continue;

                using (var procIdentity = new WindowsIdentity(hToken))
                {
                    var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
                    if (procIdentity.User != null && procIdentity.User.Equals(systemSid))
                    {
                        DuplicateTokenEx(
                            hToken,
                            TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_QUERY | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID,
                            IntPtr.Zero,
                            SecurityImpersonation,
                            TokenPrimary,
                            out primaryToken);

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message + ".", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                if (hToken != IntPtr.Zero)
                    CloseHandle(hToken);
            }
        }

        if (primaryToken == IntPtr.Zero)
            return;

        var si = new STARTUPINFO
        {
            cb = Marshal.SizeOf(typeof(STARTUPINFO))
        };
        PROCESS_INFORMATION pi;

        if (CreateProcessWithTokenW(
            primaryToken,
            0,
            null,
            command,
            0,
            IntPtr.Zero,
            null,
            ref si,
            out pi))
        {
            if (pi.hProcess != IntPtr.Zero) CloseHandle(pi.hProcess);
            if (pi.hThread != IntPtr.Zero) CloseHandle(pi.hThread);
        }

        CloseHandle(primaryToken);
    }
}
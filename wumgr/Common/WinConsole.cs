#region

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

#endregion

namespace wumgr.Common;

internal static class WinConsole
{
    public static bool Initialize(bool alwaysCreateNewConsole = true)
    {
        if (AttachConsole(MF_ATTACH_PARRENT) != 0)
            return true;
        if (!alwaysCreateNewConsole)
            return false;
        if (AllocConsole() != 0)
        {
            InitializeOutStream();
            InitializeInStream();
            return true;
        }

        return false;
    }

    private static void InitializeOutStream()
    {
        FileStream fs = CreateFileStream("CONOUT$", MF_GENERIC_WRITE, MF_FILE_SHARE_WRITE, FileAccess.Write);
        if (fs != null)
        {
            StreamWriter writer = new(fs) { AutoFlush = true };
            Console.SetOut(writer);
            Console.SetError(writer);
        }
    }

    private static void InitializeInStream()
    {
        FileStream fs = CreateFileStream("CONIN$", MF_GENERIC_READ, MF_FILE_SHARE_READ, FileAccess.Read);
        if (fs != null) Console.SetIn(new StreamReader(fs));
    }

    private static FileStream CreateFileStream(string name, uint win32DesiredAccess, uint win32ShareMode,
        FileAccess dotNetFileAccess)
    {
        SafeFileHandle file = new(
            CreateFileW(name, win32DesiredAccess, win32ShareMode, IntPtr.Zero, MF_OPEN_EXISTING,
                MF_FILE_ATTRIBUTE_NORMAL,
                IntPtr.Zero), true);
        if (!file.IsInvalid)
        {
            FileStream fs = new(file, dotNetFileAccess);
            return fs;
        }

        return null;
    }

    #region Win API Functions and Constants

    [DllImport("kernel32.dll",
        EntryPoint = "AllocConsole",
        SetLastError = true,
        CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall)]
    private static extern int AllocConsole();

    [DllImport("kernel32.dll",
        EntryPoint = "AttachConsole",
        SetLastError = true,
        CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall)]
    private static extern uint AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll",
        EntryPoint = "CreateFileW",
        SetLastError = true,
        CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile
    );

    private const uint MF_GENERIC_WRITE = 0x40000000;
    private const uint MF_GENERIC_READ = 0x80000000;
    private const uint MF_FILE_SHARE_READ = 0x00000001;
    private const uint MF_FILE_SHARE_WRITE = 0x00000002;
    private const uint MF_OPEN_EXISTING = 0x00000003;
    private const uint MF_FILE_ATTRIBUTE_NORMAL = 0x80;
    private const uint MF_ERROR_ACCESS_DENIED = 5;
    private const uint MF_ATTACH_PARRENT = 0xFFFFFFFF;

    #endregion
}

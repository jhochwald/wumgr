#region

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.ServiceProcess;

#endregion

namespace wumgr.Common;

public static class ServiceHelper
{
    private const uint MF_SERVICE_NO_CHANGE = 0xFFFFFFFF;
    private const uint MF_SERVICE_QUERY_CONFIG = 0x00000001;
    private const uint MF_SERVICE_CHANGE_CONFIG = 0x00000002;
    private const uint MF_SC_MANAGER_ALL_ACCESS = 0x000F003F;
    private const uint MF_SC_MANAGER_CONNECT = 0x0001;
    private const uint MF_SC_MANAGER_ENUMERATE_SERVICE = 0x0004;

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool ChangeServiceConfig(IntPtr hService, uint nServiceType, uint nStartType,
        uint nErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId,
        [In] char[] lpDependencies, string lpServiceStartName, string lpPassword, string lpDisplayName);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr OpenService(IntPtr hScManager, string lpServiceName, uint dwDesiredAccess);

    [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode,
        SetLastError = true)]
    private static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

    [DllImport("advapi32.dll", EntryPoint = "CloseServiceHandle")]
    private static extern int CloseServiceHandle(IntPtr hScObject);

    public static void ChangeStartMode(ServiceController svc, ServiceStartMode mode)
    {
        //var scManagerHandle = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
        IntPtr scManagerHandle = OpenSCManager(null, null, MF_SC_MANAGER_CONNECT + MF_SC_MANAGER_ENUMERATE_SERVICE);
        if (scManagerHandle == IntPtr.Zero) throw new ExternalException("Open Service Manager Error");

        IntPtr serviceHandle = OpenService(
            scManagerHandle,
            svc.ServiceName,
            MF_SERVICE_QUERY_CONFIG | MF_SERVICE_CHANGE_CONFIG);

        if (serviceHandle == IntPtr.Zero) throw new ExternalException("Open Service Error");

        bool result = ChangeServiceConfig(serviceHandle, MF_SERVICE_NO_CHANGE, (uint)mode, MF_SERVICE_NO_CHANGE, null, null,
            IntPtr.Zero, null, null, null, null);

        if (result == false)
        {
            int nError = Marshal.GetLastWin32Error();
            Win32Exception win32Exception = new(nError);
            throw new ExternalException("Could not change service start type: " + win32Exception.Message);
        }

        CloseServiceHandle(serviceHandle);
        CloseServiceHandle(scManagerHandle);
    }
}
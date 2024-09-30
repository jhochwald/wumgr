#region

using System;
using System.Globalization;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using Microsoft.Win32;
using wumgr.Common;

#endregion

namespace wumgr;

internal abstract class Gpo
{
    public enum AuOptions
    {
        Default = 0, // Automatic
        Disabled = 1,
        Notification = 2,
        Download = 3,
        Scheduled = 4,
        ManagedByAdmin = 5
    }

    public enum Respect
    {
        Unknown = 0,
        Full, // Win 7, 8, 10 Ent/Edu/Svr
        Partial, // Win 10 Pro
        None // Win 10 Home
    }

    private static readonly string MWuGpo = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";

    public static void ConfigAU(AuOptions option, int day = -1, int time = -1)
    {
        try
        {
            RegistryKey subKey = Registry.LocalMachine.CreateSubKey(MWuGpo + @"\AU", true);
            switch (option)
            {
                case AuOptions.Default: //Automatic(default)
                    subKey.DeleteValue("NoAutoUpdate", false);
                    subKey.DeleteValue("AUOptions", false);
                    break;
                case AuOptions.Disabled: //Disabled
                    subKey.SetValue("NoAutoUpdate", 1);
                    subKey.DeleteValue("AUOptions", false);
                    break;
                case AuOptions.Notification: //Notification only
                    subKey.SetValue("NoAutoUpdate", 0);
                    subKey.SetValue("AUOptions", 2);
                    break;
                case AuOptions.Download: //Download only
                    subKey.SetValue("NoAutoUpdate", 0);
                    subKey.SetValue("AUOptions", 3);
                    break;
                case AuOptions.Scheduled: //Scheduled Installation
                    subKey.SetValue("NoAutoUpdate", 0);
                    subKey.SetValue("AUOptions", 4);
                    break;
                case AuOptions.ManagedByAdmin: //Managed by Admin
                    subKey.SetValue("NoAutoUpdate", 0);
                    subKey.SetValue("AUOptions", 5);
                    break;
            }

            if (option == AuOptions.Scheduled)
            {
                if (day != -1) subKey.SetValue("ScheduledInstallDay", day);
                if (time != -1) subKey.SetValue("ScheduledInstallTime", time);
            }
            else
            {
                subKey.DeleteValue("ScheduledInstallDay", false);
                subKey.DeleteValue("ScheduledInstallTime", false);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public static AuOptions GetAU(out int day, out int time)
    {
        AuOptions option = AuOptions.Default;
        try
        {
            RegistryKey subKey = Registry.LocalMachine.OpenSubKey(MWuGpo + @"\AU", false);
            object valueNo = subKey?.GetValue("NoAutoUpdate");
            if (valueNo == null || (int)valueNo == 0)
            {
                object valueAu = subKey?.GetValue("AUOptions");
                switch (valueAu == null ? 0 : (int)valueAu)
                {
                    case 0:
                        option = AuOptions.Default;
                        break;
                    case 2:
                        option = AuOptions.Notification;
                        break;
                    case 3:
                        option = AuOptions.Download;
                        break;
                    case 4:
                        option = AuOptions.Scheduled;
                        break;
                    case 5:
                        option = AuOptions.ManagedByAdmin;
                        break;
                }
            }
            else
            {
                option = AuOptions.Disabled;
            }

            object valueDay = subKey!.GetValue("ScheduledInstallDay");
            day = valueDay != null ? (int)valueDay : 0;
            object valueTime = subKey.GetValue("ScheduledInstallTime");
            time = valueTime != null ? (int)valueTime : 0;
        }
        catch
        {
            day = 0;
            time = 0;
        }

        return option;
    }

    public static void ConfigDriverAu(int option)
    {
        try
        {
            RegistryKey subKey = Registry.LocalMachine.CreateSubKey(MWuGpo, true);
            switch (option)
            {
                case 0: // CheckState.Unchecked:
                    subKey.SetValue("ExcludeWUDriversInQualityUpdate", 1);
                    break;
                case 2: // CheckState.Indeterminate:
                    subKey.DeleteValue("ExcludeWUDriversInQualityUpdate", false);
                    break;
                case 1: // CheckState.Checked:
                    subKey.SetValue("ExcludeWUDriversInQualityUpdate", 0);
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public static int GetDriverAu()
    {
        try
        {
            RegistryKey subKey = Registry.LocalMachine.OpenSubKey(MWuGpo, false);
            object valueDrv = subKey?.GetValue("ExcludeWUDriversInQualityUpdate");

            if (valueDrv == null)
                return 2; // CheckState.Indeterminate;
            if ((int)valueDrv == 1)
                return 0; // CheckState.Unchecked;
            //if ((int)value_drv == 0)
            return 1; // CheckState.Checked
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return 2;
    }

    public static void HideUpdatePage(bool hide = true)
    {
        try
        {
            RegistryKey subKey =
                Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                    true);
            if (hide)
                subKey.SetValue("SettingsPageVisibility", "hide:windowsupdate");
            else
                subKey.DeleteValue("SettingsPageVisibility", false);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public static bool IsUpdatePageHidden()
    {
        try
        {
            RegistryKey subKey =
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer");
            string value = subKey?.GetValue("SettingsPageVisibility", "").ToString();
            return value!.Contains("hide:windowsupdate");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return false;
    }

    public static void BlockMs(bool block = true)
    {
        try
        {
            if (block)
            {
                RegistryKey subKey = Registry.LocalMachine.CreateSubKey(MWuGpo, true);
                subKey.SetValue("DoNotConnectToWindowsUpdateInternetLocations", 1);
                subKey.SetValue("WUServer", "\" \"");
                subKey.SetValue("WUStatusServer", "\" \"");
                subKey.SetValue("UpdateServiceUrlAlternate", "\" \"");

                RegistryKey subKey2 = Registry.LocalMachine.CreateSubKey(MWuGpo + @"\AU", true);
                subKey2.SetValue("UseWUServer", 1);
            }
            else
            {
                RegistryKey subKey = Registry.LocalMachine.CreateSubKey(MWuGpo, true);
                subKey.DeleteValue("DoNotConnectToWindowsUpdateInternetLocations", false);
                subKey.DeleteValue("WUServer", false);
                subKey.DeleteValue("WUStatusServer", false);
                subKey.DeleteValue("UpdateServiceUrlAlternate", false);

                RegistryKey subKey2 = Registry.LocalMachine.CreateSubKey(MWuGpo + @"\AU", true);
                subKey2.DeleteValue("UseWUServer", false);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public static int GetBlockMs()
    {
        try
        {
            RegistryKey subKey = Registry.LocalMachine.OpenSubKey(MWuGpo, false);

            object valueBlock =
                subKey?.GetValue("DoNotConnectToWindowsUpdateInternetLocations");

            RegistryKey subKey2 = Registry.LocalMachine.OpenSubKey(MWuGpo + @"\AU", false);
            object valueWsus = subKey2?.GetValue("UseWUServer");

            if (valueBlock != null && (int)valueBlock == 1 && valueWsus != null && (int)valueWsus == 1)
                return 1; // CheckState.Checked;
            if ((valueBlock == null || (int)valueBlock == 0) && (valueWsus == null || (int)valueWsus == 0))
                return 0; // CheckState.Unchecked;
            return 2; // CheckState.Indeterminate;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return 2;
    }

    public static void SetStoreAu(bool disable)
    {
        try
        {
            RegistryKey subKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\WindowsStore", true);
            //var subKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsStore\WindowsUpdate", true);
            if (disable)
                subKey.SetValue("AutoDownload", 2);
            else
                subKey.DeleteValue("AutoDownload", false); // subKey.SetValue("AutoDownload", 4);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public static bool GetStoreAu()
    {
        try
        {
            RegistryKey subKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\WindowsStore", false);
            //var subKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsStore\WindowsUpdate");
            object valueBlock = subKey?.GetValue("AutoDownload");
            return valueBlock != null && (int)valueBlock == 2;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return false;
    }

    public static void DisableAu(bool disable)
    {
        try
        {
            if (disable)
            {
                ConfigSvc("UsoSvc", ServiceStartMode.Disabled); // Update Orchestrator Service
                ConfigSvc("WaaSMedicSvc", ServiceStartMode.Disabled); // Windows Update Medic Service
            }
            else
            {
                ConfigSvc("UsoSvc", ServiceStartMode.Automatic); // Update Orchestrator Service
                ConfigSvc("WaaSMedicSvc", ServiceStartMode.Manual); // Windows Update Medic Service
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private static void ConfigSvc(string name, ServiceStartMode mode)
    {
        ServiceController svc = new(name);
        bool showErr = false;
        try
        {
            if (mode == ServiceStartMode.Disabled && svc.Status == ServiceControllerStatus.Running)
            {
                svc.Stop();
                showErr = false;
            }

            // Note: for UsoSvc and for WaaSMedicSvc this call fails with an access error so we have to set the registry
            //ServiceHelper.ChangeStartMode(svc, mode);
        }
        catch
        {
            if (false)
                AppLog.Line("Error Stoping Service: {0}", name);
        }

        svc.Close();

        RegistryKey subKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name,
            RegistryKeyPermissionCheck.ReadWriteSubTree,
            RegistryRights.SetValue | RegistryRights.ChangePermissions | RegistryRights.TakeOwnership);
        if (subKey == null)
        {
            AppLog.Line("Service {0} does not exist", name);
            return;
        }

        subKey.SetValue("Start", (int)mode);

        RegistrySecurity ac = subKey.GetAccessControl();
        AuthorizationRuleCollection
            rules = ac.GetAccessRules(true, true, typeof(SecurityIdentifier)); // get as SID not string
        // cleanup old roule
        foreach (RegistryAccessRule rule in rules)
            if (rule.IdentityReference.Value.Equals(FileOps.SidSystem))
                ac.RemoveAccessRule(rule);
        // Note: windows tryes to re-enable this services so we need to remove system write access
        if (mode == ServiceStartMode.Disabled) // add new rule
            ac.AddAccessRule(new RegistryAccessRule(new SecurityIdentifier(FileOps.SidSystem),
                RegistryRights.FullControl, AccessControlType.Deny));
        subKey.SetAccessControl(ac);
    }

    public static bool GetDisableAu()
    {
        return IsSvcDisabled("UsoSvc") && IsSvcDisabled("WaaSMedicSvc");
    }

    private static bool IsSvcDisabled(string name)
    {
        try
        {
            RegistryKey subKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name, false);
            return subKey == null || MiscFunc.ParseInt(subKey.GetValue("Start", "-1").ToString()) ==
                (int)ServiceStartMode.Disabled;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return false;
    }

    public static Respect GetRespect()
    {
        try
        {
            RegistryKey subKey =
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
            if (subKey == null)
                return Respect.Unknown;
            //string edition = subKey.GetValue("EditionID", "").ToString();
            string name = subKey.GetValue("ProductName", "").ToString();
            string type = subKey.GetValue("InstallationType", "").ToString();

            if (GetWinVersion() < 10.0f || type.Equals("Server", StringComparison.CurrentCultureIgnoreCase) ||
                name.Contains("Education") || name.Contains("Enterprise"))
                return Respect.Full;

            if (type.Equals("Client", StringComparison.CurrentCultureIgnoreCase))
            {
                if (name.Contains("Pro"))
                    return Respect.Partial;
                if (name.Contains("Home"))
                    return Respect.None;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return Respect.Unknown;
    }

    public static float GetWinVersion()
    {
        try
        {
            RegistryKey subKey =
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
            if (subKey == null)
                return 0.0f;
            //string Majorversion = subKey.GetValue("CurrentMajorVersionNumber", "0").ToString(); // this is 10 on 10 but not present on earlier editions
            string version = subKey.GetValue("CurrentVersion", "0").ToString();
            float versionNum = float.Parse(version, CultureInfo.InvariantCulture.NumberFormat);
            //string name = subKey.GetValue("ProductName", "").ToString();

            /*
                Operating system              Version number
                ----------------------------  --------------
                Windows 10                      6.3 WTF why not 10
                Windows Server 2016             6.3 WTF why not 10
                Windows 8.1                     6.3
                Windows Server 2012 R2          6.3
                Windows 8                       6.2
                Windows Server 2012             6.2
                Windows 7                       6.1
                Windows Server 2008 R2          6.1
                Windows Server 2008             6.0
                Windows Vista                   6.0
                Windows Server 2003 R2          5.2
                Windows Server 2003             5.2
                Windows XP 64-Bit Edition       5.2
                Windows XP                      5.1
                Windows 2000                    5.0
                Windows ME                      4.9
                Windows 98                      4.10
             */

            if (versionNum >= 6.3)
            {
                //!name.Contains("8.1") && !name.Contains("2012 R2");
                int build = MiscFunc.ParseInt(subKey.GetValue("CurrentBuildNumber", "0").ToString());
                if (build >= 10000) // 1507 RTM release
                    return 10.0f;
            }

            return versionNum;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return 0.0f;
    }
}

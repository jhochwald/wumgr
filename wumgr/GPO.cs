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

/// <summary>
///     Abstract class for managing Group Policy Objects (GPO) related to Windows Update settings.
/// </summary>
internal abstract class Gpo
{
    /// <summary>
    ///     Enumeration for Automatic Update options.
    /// </summary>
    public enum AuOptions
    {
        Default = 0, // Automatic
        Disabled = 1,
        Notification = 2,
        Download = 3,
        Scheduled = 4,
        ManagedByAdmin = 5
    }

    /// <summary>
    ///     Enumeration for the level of respect for GPO settings.
    /// </summary>
    public enum Respect
    {
        Unknown = 0,
        Full, // Win 7, 8, 10, 11 Ent/Edu/Svr
        Partial, // Win 10, 11 Pro
        None // Win 10, 11 Home
    }

    private static readonly string MWuGpo = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";

    /// <summary>
    ///     Configures Automatic Update settings.
    /// </summary>
    /// <param name="option">The Automatic Update option to set.</param>
    /// <param name="day">The scheduled install day (optional).</param>
    /// <param name="time">The scheduled install time (optional).</param>
    public static void ConfigAU(AuOptions option, int day = -1, int time = -1)
    {
        try
        {
            using RegistryKey subKey = Registry.LocalMachine.CreateSubKey(MWuGpo + @"\AU", true);
            subKey.SetValue("NoAutoUpdate", option == AuOptions.Disabled ? 1 : 0);

            if (option == AuOptions.Default)
                subKey.DeleteValue("AUOptions", false);
            else
                subKey.SetValue("AUOptions", (int)option);

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

    /// <summary>
    ///     Retrieves the current Automatic Update settings.
    /// </summary>
    /// <param name="day">The scheduled install day.</param>
    /// <param name="time">The scheduled install time.</param>
    /// <returns>The current Automatic Update option.</returns>
    public static AuOptions GetAu(out int day, out int time)
    {
        AuOptions option = AuOptions.Default;
        try
        {
            using RegistryKey subKey = Registry.LocalMachine.OpenSubKey(MWuGpo + @"\AU", false);
            object valueNo = subKey?.GetValue("NoAutoUpdate");
            if (valueNo == null || (int)valueNo == 0)
            {
                object valueAu = subKey?.GetValue("AUOptions");
                option = valueAu switch
                {
                    2 => AuOptions.Notification,
                    3 => AuOptions.Download,
                    4 => AuOptions.Scheduled,
                    5 => AuOptions.ManagedByAdmin,
                    _ => AuOptions.Default
                };
            }
            else
            {
                option = AuOptions.Disabled;
            }

            day = subKey?.GetValue("ScheduledInstallDay") as int? ?? 0;
            time = subKey?.GetValue("ScheduledInstallTime") as int? ?? 0;
        }
        catch
        {
            day = 0;
            time = 0;
        }

        return option;
    }

    /// <summary>
    ///     Configures driver Automatic Update settings.
    /// </summary>
    /// <param name="option">The option to set for driver updates.</param>
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

    /// <summary>
    ///     Retrieves the current driver Automatic Update settings.
    /// </summary>
    /// <returns>The current driver Automatic Update option.</returns>
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

    /// <summary>
    ///     Hides or shows the Windows Update settings page.
    /// </summary>
    /// <param name="hide">True to hide the page, false to show it.</param>
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

    /// <summary>
    ///     Checks if the Windows Update settings page is hidden.
    /// </summary>
    /// <returns>True if the page is hidden, false otherwise.</returns>
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

    /// <summary>
    ///     Blocks or unblocks connections to Microsoft Update servers.
    /// </summary>
    /// <param name="block">True to block, false to unblock.</param>
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

    /// <summary>
    ///     Retrieves the current block status for Microsoft Update servers.
    /// </summary>
    /// <returns>The current block status.</returns>
    public static int GetBlockMs()
    {
        try
        {
            RegistryKey subKey = Registry.LocalMachine.OpenSubKey(MWuGpo, false);

            object valueBlock =
                subKey?.GetValue("DoNotConnectToWindowsUpdateInternetLocations");

            RegistryKey subKey2 = Registry.LocalMachine.OpenSubKey(MWuGpo + @"\AU", false);
            object valueWsus = subKey2?.GetValue("UseWUServer");

            if (valueBlock as int? == 1 && valueWsus as int? == 1)
                return 1; // CheckState.Checked;
            if (valueBlock as int? == 0 && valueWsus as int? == 0)
                return 0; // CheckState.Unchecked;
            return 2; // CheckState.Indeterminate;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return 2;
    }

    /// <summary>
    ///     Configures the Windows Store Automatic Update settings.
    /// </summary>
    /// <param name="disable">True to disable, false to enable.</param>
    public static void SetStoreAu(bool disable)
    {
        try
        {
            RegistryKey subKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\WindowsStore", true);
            if (disable)
                subKey.SetValue("AutoDownload", 2);
            else
                subKey.DeleteValue("AutoDownload", false);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    /// <summary>
    ///     Retrieves the current Windows Store Automatic Update settings.
    /// </summary>
    /// <returns>True if disabled, false otherwise.</returns>
    public static bool GetStoreAu()
    {
        try
        {
            RegistryKey subKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\WindowsStore", false);
            object valueBlock = subKey?.GetValue("AutoDownload");
            return valueBlock != null && (int)valueBlock == 2;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return false;
    }

    /// <summary>
    ///     Disables or enables Automatic Updates.
    /// </summary>
    /// <param name="disable">True to disable, false to enable.</param>
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

    /// <summary>
    ///     Configures the start mode of a service.
    /// </summary>
    /// <param name="name">The name of the service.</param>
    /// <param name="mode">The start mode to set.</param>
    private static void ConfigSvc(string name, ServiceStartMode mode)
    {
        ServiceController svc = new(name);
        bool showErr = false;
        try
        {
            if (mode == ServiceStartMode.Disabled && svc.Status == ServiceControllerStatus.Running) svc.Stop();
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
        foreach (RegistryAccessRule rule in rules)
            if (rule.IdentityReference.Value.Equals(FileOps.SidSystem))
                ac.RemoveAccessRule(rule);
        if (mode == ServiceStartMode.Disabled)
            ac.AddAccessRule(new RegistryAccessRule(new SecurityIdentifier(FileOps.SidSystem),
                RegistryRights.FullControl, AccessControlType.Deny));
        subKey.SetAccessControl(ac);
    }

    /// <summary>
    ///     Checks if Automatic Updates are disabled.
    /// </summary>
    /// <returns>True if disabled, false otherwise.</returns>
    public static bool GetDisableAu()
    {
        return IsSvcDisabled("UsoSvc") && IsSvcDisabled("WaaSMedicSvc");
    }

    /// <summary>
    ///     Checks if a service is disabled.
    /// </summary>
    /// <param name="name">The name of the service.</param>
    /// <returns>True if disabled, false otherwise.</returns>
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

    /// <summary>
    ///     Retrieves the level of respect for GPO settings.
    /// </summary>
    /// <returns>The level of respect.</returns>
    public static Respect GetRespect()
    {
        try
        {
            RegistryKey subKey =
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
            if (subKey == null)
                return Respect.Unknown;
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

    /// <summary>
    ///     Retrieves the Windows version.
    /// </summary>
    /// <returns>The Windows version as a float.</returns>
    public static float GetWinVersion()
    {
        try
        {
            RegistryKey subKey =
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
            if (subKey == null)
                return 0.0f;
            string version = subKey.GetValue("CurrentVersion", "0").ToString();
            float versionNum = float.Parse(version, CultureInfo.InvariantCulture.NumberFormat);

            if (versionNum >= 6.3)
            {
                int build = MiscFunc.ParseInt(subKey.GetValue("CurrentBuildNumber", "0").ToString());
                if (build >= 10000)
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

#region

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

#endregion

namespace wumgr;

public static class Translate
{
    private static readonly SortedDictionary<string, string> MStrings = new();

    public static void Load(string lang = "")
    {
        if (lang == "")
        {
            CultureInfo ci = CultureInfo.InstalledUICulture;

            /*Console.WriteLine("Default Language Info:");
            Console.WriteLine("* Name: {0}", ci.Name);
            Console.WriteLine("* Display Name: {0}", ci.DisplayName);
            Console.WriteLine("* English Name: {0}", ci.EnglishName);
            Console.WriteLine("* 2-letter ISO Name: {0}", ci.TwoLetterISOLanguageName);
            Console.WriteLine("* 3-letter ISO Name: {0}", ci.ThreeLetterISOLanguageName);
            Console.WriteLine("* 3-letter Win32 API Name: {0}", ci.ThreeLetterWindowsLanguageName);*/

            lang = ci.TwoLetterISOLanguageName;
        }


        MStrings.Add("msg_running", "Application is already running.");
        MStrings.Add("msg_admin_req", "The {0} requires Administrator privileges in order to install updates");
        MStrings.Add("msg_ro_wrk_dir", "Can't write to working directory: {0}");
        MStrings.Add("cap_chk_upd", "Please Check For Updates");
        MStrings.Add("msg_chk_upd",
            "{0} couldn't check for updates for {1} days, please check for updates manually and resolve possible issues");
        MStrings.Add("cap_new_upd", "New Updates found");
        MStrings.Add("msg_new_upd", "{0} has found {1} new updates, please review the updates and install them");
        MStrings.Add("lbl_fnd_upd", "Windows Update ({0})");
        MStrings.Add("lbl_inst_upd", "Installed Updates ({0})");
        MStrings.Add("lbl_block_upd", "Hidden Updates ({0})");
        MStrings.Add("lbl_old_upd", "Update History ({0})");
        MStrings.Add("msg_tool_err", "Failed to start tool");
        MStrings.Add("msg_admin_dl",
            "Administrator privileges are required in order to download updates using windows update services. Use 'Manual' download instead.");
        MStrings.Add("msg_admin_inst", "Administrator privileges are required in order to install updates.");
        MStrings.Add("msg_admin_rem", "Administrator privileges are required in order to remove updates.");
        MStrings.Add("msg_dl_done", "Updates downloaded to {0}, ready to be installed by the user.");
        MStrings.Add("msg_dl_err", "Updates downloaded to {0}, some updates failed to download.");
        MStrings.Add("msg_inst_done", "Updates successfully installed, however, a reboot is required.");
        MStrings.Add("msg_inst_err", "Installation of some Updates has failed, also a reboot is required.");
        MStrings.Add("err_admin", "Required privileges are not available");
        MStrings.Add("err_busy", "Another operation is already in progress");
        MStrings.Add("err_dl", "Download failed");
        MStrings.Add("err_inst", "Installation failed");
        MStrings.Add("err_no_sel", "No selected updates or no updates eligible for the operation");
        MStrings.Add("err_int", "Internal error");
        MStrings.Add("err_file", "Required file(s) could not be found");
        MStrings.Add("msg_err", "{0} failed: {1}.");
        MStrings.Add("msg_wuau", "Windows Update Service is not available, try to start it?");
        MStrings.Add("menu_tools", "&Tools");
        MStrings.Add("menu_about", "&About");
        MStrings.Add("menu_exit", "E&xit");
        MStrings.Add("stat_not_start", "Not Started");
        MStrings.Add("stat_in_prog", "In Progress");
        MStrings.Add("stat_success", "Succeeded");
        MStrings.Add("stat_success_2", "Succeeded with Errors");
        MStrings.Add("stat_failed", "Failed");
        MStrings.Add("stat_abbort", "Aborted");
        MStrings.Add("stat_beta", "Beta");
        MStrings.Add("stat_install", "Installed");
        MStrings.Add("stat_rem", "Removable");
        MStrings.Add("stat_block", "Hidden");
        MStrings.Add("stat_dl", "Downloaded");
        MStrings.Add("stat_pending", "Pending");
        MStrings.Add("stat_sel", "(!)");
        MStrings.Add("stat_mand", "Mandatory");
        MStrings.Add("stat_excl", "Exclusive");
        MStrings.Add("stat_reboot", "Needs Reboot");
        MStrings.Add("menu_wuau", "Windows Update Service");
        MStrings.Add("menu_refresh", "&Refresh");
        MStrings.Add("op_check", "Checking for Updates");
        MStrings.Add("op_prep", "Preparing Check");
        MStrings.Add("op_dl", "Downloading Updates");
        MStrings.Add("op_inst", "Installing Updates");
        MStrings.Add("op_rem", "Removing Updates");
        MStrings.Add("op_cancel", "Cancelling Operation");
        MStrings.Add("op_unk", "Unknown Operation");
        MStrings.Add("msg_gpo",
            "Your version of Windows does not respect the standard GPO's, to keep automatic Windows updates blocked, update facilitation services must be disabled.");
        MStrings.Add("col_title", "Title");
        MStrings.Add("col_cat", "Category");
        MStrings.Add("col_kb", "KB Article");
        MStrings.Add("col_app_id", "Application ID");
        MStrings.Add("col_date", "Date");
        MStrings.Add("col_site", "Size");
        MStrings.Add("col_stat", "State");
        MStrings.Add("lbl_support", "Support Url");
        MStrings.Add("lbl_search", "Search filter:");
        MStrings.Add("tip_search", "Search");
        MStrings.Add("tip_inst", "Install");
        MStrings.Add("tip_dl", "Download");
        MStrings.Add("tip_hide", "Hide");
        MStrings.Add("tip_lnk", "Get Links");
        MStrings.Add("tip_rem", "Uninstall");
        MStrings.Add("tip_cancel", "Cancel");
        MStrings.Add("lbl_opt", "Options");
        MStrings.Add("lbl_au", "Auto Update");
        MStrings.Add("lbl_off", "Offline Mode");
        MStrings.Add("lbl_dl", "Download wsusscn2.cab");
        MStrings.Add("lbl_man", "'Manual' Download/Install");
        MStrings.Add("lbl_old", "Include superseded");
        MStrings.Add("lbl_ms", "Register Microsoft Update");
        MStrings.Add("lbl_start", "Startup");
        MStrings.Add("lbl_auto", "Run in background");
        MStrings.Add("lbl_ac_no", "No auto search for updates");
        MStrings.Add("lbl_ac_day", "Search for updates every day");
        MStrings.Add("lbl_ac_week", "Search for updates once a week");
        MStrings.Add("lbl_ac_month", "Search for updates every month");
        MStrings.Add("lbl_uac", "Always run as Administrator");
        MStrings.Add("lbl_block_ms", "Block Access to WU Servers");
        MStrings.Add("lbl_au_off", "Disable Automatic Update");
        MStrings.Add("lbl_au_dissable", "Disable Update Facilitators");
        MStrings.Add("lbl_au_notify", "Notification Only");
        MStrings.Add("lbl_au_dl", "Download Only");
        MStrings.Add("lbl_au_time", "Scheduled & Installation");
        MStrings.Add("lbl_au_def", "Automatic Update (default)");
        MStrings.Add("lbl_hide", "Hide WU Settings Page");
        MStrings.Add("lbl_store", "Disable Store Auto Update");
        MStrings.Add("lbl_drv", "Include Drivers");
        MStrings.Add("msg_disable_au", "For the new configuration to fully take effect a reboot is required.");
        MStrings.Add("lbl_all", "Select All");
        MStrings.Add("lbl_group", "Group Updates");
        MStrings.Add("lbl_patreon", "Original project site");
        MStrings.Add("lbl_github", "Visit WuMgr on GitHub");

        string langIni = Program.AppPath + @"\Translation.ini";

        if (!File.Exists(langIni))
        {
            foreach (string key in MStrings.Keys)
                Program.IniWriteValue("en", key, MStrings[key], langIni);
            return;
        }

        if (lang != "en")
            foreach (string key in MStrings.Keys.ToList())
            {
                string str = Program.IniReadValue(lang, key, "", langIni);
                if (str.Length == 0)
                    continue;

                MStrings.Remove(key);
                MStrings.Add(key, str);
            }
    }

    public static string Fmt(string id, params object[] args)
    {
        try
        {
            MStrings.TryGetValue(id, out string str);
            return string.Format(str!, args);
        }
        catch
        {
            return "err on " + id;
        }
    }
}

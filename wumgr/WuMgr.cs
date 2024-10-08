﻿#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WUApiLib;
using wumgr.Common;
using wumgr.Properties;

#endregion

namespace wumgr;

public partial class WuMgr : Form
{
    private const int WM_SYSCOMMAND = 0x112;

    public const int MF_BITMAP = 0x00000004;
    public const int MF_CHECKED = 0x00000008;
    public const int MF_DISABLED = 0x00000002;
    public const int MF_ENABLED = 0x00000000;
    public const int MF_GRAYED = 0x00000001;
    public const int MF_MENUBARBREAK = 0x00000020;
    public const int MF_MENUBREAK = 0x00000040;
    public const int MF_OWNERDRAW = 0x00000100;
    private const int MF_POPUP = 0x00000010;
    private const int MF_SEPARATOR = 0x00000800;
    public const int MF_STRING = 0x00000000;
    public const int MF_UNCHECKED = 0x00000000;
    private const int MF_BYPOSITION = 0x400;

    public const int MF_BYCOMMAND = 0x000;

    //public const Int32 MF_REMOVE = 0x1000;
    private const int MYMENU_ABOUT = 1000;
    private static Timer mTimer;
    private readonly WuAgent agent;
    private readonly int IdleDelay;
    private readonly Gpo.Respect mGPORespect = Gpo.Respect.Unknown;
    private readonly float mSearchBoxHeight;
    private readonly MenuItem mToolsMenu;
    private readonly float mWinVersion;
    private bool allowshowdisplay = true;
    private AutoUpdateOptions AutoUpdate = AutoUpdateOptions.No;
    private bool bUpdateList;
    private bool checkChecks;
    private UpdateLists CurrentList = UpdateLists.UpdateHistory;
    private bool doUpdte;
    private bool ignoreChecks;
    private DateTime LastBaloon = DateTime.MinValue;
    private DateTime LastCheck = DateTime.MaxValue;
    private string mSearchFilter;
    private bool mSuspendUpdate;
    private bool ResultShown;
    private bool suspendChange;
    private MenuItem wuauMenu;

    public WuMgr()
    {
        InitializeComponent();

        //notifyIcon1.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
        notifyIcon.Text = Program.MName;

        if (Program.TestArg("-tray"))
        {
            allowshowdisplay = false;
            notifyIcon.Visible = true;
        }

        if (!MiscFunc.IsRunningAsUwp())
            Text = $"{Program.MName} v{Program.MVersion} by David Xanatos";

        Localize();

        btnSearch.Image = new Bitmap(Resources.icons8_available_updates_32, new Size(25, 25));
        btnInstall.Image = new Bitmap(Resources.icons8_software_installer_32, new Size(25, 25));
        btnDownload.Image = new Bitmap(Resources.icons8_downloading_updates_32, new Size(25, 25));
        btnUnInstall.Image = new Bitmap(Resources.icons8_trash_32, new Size(25, 25));
        btnHide.Image = new Bitmap(Resources.icons8_hide_32, new Size(25, 25));
        btnGetLink.Image = new Bitmap(Resources.icons8_link_32, new Size(25, 25));
        btnCancel.Image = new Bitmap(Resources.icons8_cancel_32, new Size(25, 25));

        AppLog.Logger += LineLogger;

        foreach (string line in AppLog.GetLog())
            logBox.AppendText(line + Environment.NewLine);
        logBox.ScrollToCaret();


        agent = WuAgent.GetInstance();
        agent.Progress += OnProgress;
        agent.UpdatesChanged += OnUpdates;
        agent.Finished += OnFinished;

        if (!agent.IsActive())
            if (MessageBox.Show(Translate.Fmt("msg_wuau"), Program.MName, MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                agent.EnableWuAuServ();
                agent.Init();
            }

        mSuspendUpdate = true;
        chkDrivers.CheckState = (CheckState)Gpo.GetDriverAu();

        mGPORespect = Gpo.GetRespect();
        mWinVersion = Gpo.GetWinVersion();

        if (mWinVersion < 10) // 8.1 or below
            chkHideWU.Enabled = false;
        chkHideWU.Checked = Gpo.IsUpdatePageHidden();

        if (mGPORespect == Gpo.Respect.Partial || mGPORespect == Gpo.Respect.None)
            radSchedule.Enabled = radDownload.Enabled = radNotify.Enabled = false;
        else if (mGPORespect == Gpo.Respect.Unknown)
            AppLog.Line("Unrecognized Windows Edition, respect for GPO settings is unknown.");

        if (mGPORespect == Gpo.Respect.None)
            chkBlockMS.Enabled = false;
        chkBlockMS.CheckState = (CheckState)Gpo.GetBlockMs();

        int day, time;
        switch (Gpo.GetAu(out day, out time))
        {
            case Gpo.AuOptions.Default:
                radDefault.Checked = true;
                break;
            case Gpo.AuOptions.Disabled:
                radDisable.Checked = true;
                break;
            case Gpo.AuOptions.Notification:
                radNotify.Checked = true;
                break;
            case Gpo.AuOptions.Download:
                radDownload.Checked = true;
                break;
            case Gpo.AuOptions.Scheduled:
                radSchedule.Checked = true;
                break;
        }

        try
        {
            dlShDay.SelectedIndex = day;
            dlShTime.SelectedIndex = time;
        }
        catch
        {
        }

        if (mWinVersion >= 10) // 10 or abive
            chkDisableAU.Checked = Gpo.GetDisableAu();

        if (mWinVersion < 6.2) // win 7 or below
            chkStore.Enabled = false;
        chkStore.Checked = Gpo.GetStoreAu();

        try
        {
            dlAutoCheck.SelectedIndex = MiscFunc.ParseInt(GetConfig("AutoUpdate", "0"));
        }
        catch
        {
        }

        chkAutoRun.Checked = Program.IsAutoStart();
        if (MiscFunc.IsRunningAsUwp() && chkAutoRun.CheckState == CheckState.Checked)
            chkAutoRun.Enabled = false;
        IdleDelay = MiscFunc.ParseInt(GetConfig("IdleDelay", "20"));
        chkNoUAC.Checked = Program.IsSkipUacRun();
        chkNoUAC.Enabled = MiscFunc.IsAdministrator();
        chkNoUAC.Visible = chkNoUAC.Enabled || chkNoUAC.Checked || !MiscFunc.IsRunningAsUwp();


        chkOffline.Checked = MiscFunc.ParseInt(GetConfig("Offline", "0")) != 0;
        chkDownload.Checked = MiscFunc.ParseInt(GetConfig("Download", "1")) != 0;
        chkManual.Checked = MiscFunc.ParseInt(GetConfig("Manual", "0")) != 0;
        if (!MiscFunc.IsAdministrator())
        {
            if (MiscFunc.IsRunningAsUwp())
            {
                chkOffline.Enabled = false;
                chkOffline.Checked = false;

                chkManual.Enabled = false;
                chkManual.Checked = true;
            }

            chkMsUpd.Enabled = false;
        }

        chkMsUpd.Checked = agent.IsActive() && agent.TestService(WuAgent.MsUpdGuid);

        // Note: when running in the UWP sandbox we cant write the real registry even as admins
        if (!MiscFunc.IsAdministrator() || MiscFunc.IsRunningAsUwp())
            foreach (Control ctl in tabAU.Controls)
                ctl.Enabled = false;

        chkOld.Checked = MiscFunc.ParseInt(GetConfig("IncludeOld", "0")) != 0;
        string source = GetConfig("Source", "Windows Update");

        string Online = Program.GetArg("-online");
        if (Online != null)
        {
            chkOffline.Checked = false;
            if (Online.Length > 0)
                source = agent.GetServiceName(Online, true);
        }

        string Offline = Program.GetArg("-offline");
        if (Offline != null)
        {
            chkOffline.Checked = true;
            if (Offline.Equals("download", StringComparison.CurrentCultureIgnoreCase))
                chkDownload.Checked = true;
            else if (Offline.Equals("no_download", StringComparison.CurrentCultureIgnoreCase))
                chkDownload.Checked = false;
        }

        if (Program.TestArg("-manual"))
            chkManual.Checked = true;

        try
        {
            LastCheck = DateTime.Parse(GetConfig("LastCheck"));
            AppLog.Line("Last Checked for updates: {0}",
                LastCheck.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern));
        }
        catch
        {
            LastCheck = DateTime.Now;
        }

        LoadProviders(source);

        mSearchBoxHeight = panelList.RowStyles[2].Height;
        panelList.RowStyles[2].Height = 0;

        chkGrupe.Checked = MiscFunc.ParseInt(GetConfig("GroupUpdates", "1")) != 0;
        updateView.ShowGroups = chkGrupe.Checked;

        mSuspendUpdate = false;


        if (Program.TestArg("-provisioned"))
            tabs.Enabled = false;


        mToolsMenu = new MenuItem();
        mToolsMenu.Text = Translate.Fmt("menu_tools");

        BuildToolsMenu();

        notifyIcon.ContextMenu = new ContextMenu();

        MenuItem menuAbout = new();
        menuAbout.Text = Translate.Fmt("menu_about");
        menuAbout.Click += menuAbout_Click;

        MenuItem menuExit = new();
        menuExit.Text = Translate.Fmt("menu_exit");
        menuExit.Click += menuExit_Click;

        notifyIcon.ContextMenu.MenuItems.AddRange([mToolsMenu, menuAbout, new MenuItem("-"), menuExit]);


        IntPtr MenuHandle = GetSystemMenu(Handle, false); // Note: to restore default set true
        InsertMenu(MenuHandle, 5, MF_BYPOSITION | MF_SEPARATOR, 0, string.Empty); // <-- Add a menu seperator
        InsertMenu(MenuHandle, 6, MF_BYPOSITION | MF_POPUP, (int)mToolsMenu.Handle, mToolsMenu.Text);
        InsertMenu(MenuHandle, 7, MF_BYPOSITION, MYMENU_ABOUT, menuAbout.Text);


        UpdateCounts();
        SwitchList(UpdateLists.UpdateHistory);

        doUpdte = Program.TestArg("-update");

        mTimer = new Timer();
        mTimer.Interval = 250; // 4 times per second
        mTimer.Tick += OnTimedEvent;
        mTimer.Enabled = true;

        Program.Ipc.PipeMessage += PipesMessageHandler;
        Program.Ipc.Listen();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("user32.dll")]
    private static extern bool InsertMenu(IntPtr hMenu, int wPosition, int wFlags, int wIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern int AppendMenu(IntPtr hMenu, int Flags, int NewID, string Item);

    [DllImport("user32.dll")]
    private static extern int GetMenuItemCount(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern bool RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

    protected override void WndProc(ref Message msg)
    {
        switch (msg.Msg)
        {
            case WM_SYSCOMMAND:
            {
                switch (msg.WParam.ToInt32())
                {
                    case MYMENU_ABOUT:
                        menuAbout_Click(null, null);
                        return;
                }
            }
                break;
        }

        base.WndProc(ref msg);
    }

    private void LineLogger(object sender, AppLog.LogEventArgs args)
    {
        logBox.AppendText(args.line + Environment.NewLine);
        logBox.ScrollToCaret();
    }

    protected override void SetVisibleCore(bool value)
    {
        base.SetVisibleCore(allowshowdisplay ? value : allowshowdisplay);
    }

    private void PipesMessageHandler(PipeIpc.PipeServer pipe, string data)
    {
        if (data.Equals("show", StringComparison.CurrentCultureIgnoreCase))
        {
            notifyIcon_BalloonTipClicked(null, null);
            pipe.Send("ok");
        }
        else
        {
            pipe.Send("unknown");
        }
    }

    private void OnTimedEvent(object source, EventArgs e)
    {
        bool updateNow = false;
        if (notifyIcon.Visible)
        {
            int daysDue = GetAutoUpdateDue();
            if (daysDue != 0 && !agent.IsBusy())
            {
                // ensure we only start a check when user is not doing anything
                uint idleTime = MiscFunc.GetIdleTime();
                if (IdleDelay * 60 < idleTime)
                {
                    AppLog.Line("Starting automatic search for updates.");
                    updateNow = true;
                }
                else if (daysDue > GetGraceDays())
                {
                    if (LastBaloon < DateTime.Now.AddHours(-4))
                    {
                        LastBaloon = DateTime.Now;
                        notifyIcon.ShowBalloonTip(int.MaxValue, Translate.Fmt("cap_chk_upd"),
                            Translate.Fmt("msg_chk_upd", Program.MName, daysDue), ToolTipIcon.Warning);
                    }
                }
            }

            if (agent.MPendingUpdates.Count > 0)
                if (LastBaloon < DateTime.Now.AddHours(-4))
                {
                    LastBaloon = DateTime.Now;
                    notifyIcon.ShowBalloonTip(int.MaxValue, Translate.Fmt("cap_new_upd"),
                        Translate.Fmt("msg_new_upd", Program.MName,
                            string.Join(Environment.NewLine, agent.MPendingUpdates.Select(x => $"- {x.Title}"))),
                        ToolTipIcon.Info);
                }
        }

        if ((doUpdte || updateNow && !ResultShown) && agent.IsActive())
        {
            doUpdte = false;
            if (chkOffline.Checked)
                agent.SearchForUpdates(chkDownload.Checked, chkOld.Checked);
            else
                agent.SearchForUpdates(dlSource.Text, chkOld.Checked);
        }

        if (bUpdateList)
        {
            bUpdateList = false;
            LoadList();
        }

        if (checkChecks)
            UpdateState();
    }

    private void WuMgr_Load(object sender, EventArgs e)
    {
        Width = 900;
    }

    private int GetAutoUpdateDue()
    {
        try
        {
            DateTime NextUpdate = DateTime.MaxValue;
            switch (AutoUpdate)
            {
                case AutoUpdateOptions.EveryDay:
                    NextUpdate = LastCheck.AddDays(1);
                    break;
                case AutoUpdateOptions.EveryWeek:
                    NextUpdate = LastCheck.AddDays(7);
                    break;
                case AutoUpdateOptions.EveryMonth:
                    NextUpdate = LastCheck.AddMonths(1);
                    break;
            }

            if (NextUpdate >= DateTime.Now)
                return 0;
            return (int)Math.Ceiling((DateTime.Now - NextUpdate).TotalDays);
        }
        catch
        {
            LastCheck = DateTime.Now;
            return 0;
        }
    }

    private int GetGraceDays()
    {
        switch (AutoUpdate)
        {
            case AutoUpdateOptions.EveryMonth: return 15;
            default: return 3;
        }
    }

    private void WuMgr_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (notifyIcon.Visible && allowshowdisplay)
        {
            e.Cancel = true;
            allowshowdisplay = false;
            Hide();
            return;
        }

        agent.Progress -= OnProgress;
        agent.UpdatesChanged -= OnUpdates;
        agent.Finished -= OnFinished;
    }

    private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        if (allowshowdisplay)
        {
            allowshowdisplay = false;
            Hide();
        }
        else
        {
            allowshowdisplay = true;
            Show();
        }
    }

    private void LoadProviders(string source = null)
    {
        dlSource.Items.Clear();
        for (int i = 0; i < agent.MServiceList.Count; i++)
        {
            string service = agent.MServiceList[i];
            dlSource.Items.Add(service);

            if (source != null && service.Equals(source, StringComparison.CurrentCultureIgnoreCase))
                dlSource.SelectedIndex = i;
        }
    }

    private void UpdateCounts()
    {
        btnWinUpd.Text = Translate.Fmt("lbl_fnd_upd", agent.MPendingUpdates.Count);
        btnInstalled.Text = Translate.Fmt("lbl_inst_upd", agent.MInstalledUpdates.Count);
        btnHidden.Text = Translate.Fmt("lbl_block_upd", agent.MHiddenUpdates.Count);
        btnHistory.Text = Translate.Fmt("lbl_old_upd", agent.MUpdateHistory.Count);
    }

    private void LoadList()
    {
        ignoreChecks = true;
        updateView.CheckBoxes = CurrentList != UpdateLists.UpdateHistory;
        ignoreChecks = false;
        updateView.ForeColor = updateView.CheckBoxes && !agent.IsValid() ? Color.Gray : Color.Black;

        switch (CurrentList)
        {
            case UpdateLists.PendingUpdates:
                LoadList(agent.MPendingUpdates);
                break;
            case UpdateLists.InstaledUpdates:
                LoadList(agent.MInstalledUpdates);
                break;
            case UpdateLists.HiddenUpdates:
                LoadList(agent.MHiddenUpdates);
                break;
            case UpdateLists.UpdateHistory:
                LoadList(agent.MUpdateHistory);
                break;
        }
    }

    private void LoadList(List<MsUpdate> List)
    {
        string INIPath = Program.WrkPath + @"\Updates.ini";

        updateView.Items.Clear();
        List<ListViewItem> items = [];
        for (int i = 0; i < List.Count; i++)
        {
            MsUpdate Update = List[i];
            string State = "";
            switch (Update.State)
            {
                case MsUpdate.UpdateState.History:
                    switch ((OperationResultCode)Update.ResultCode)
                    {
                        case OperationResultCode.orcNotStarted:
                            State = Translate.Fmt("stat_not_start");
                            break;
                        case OperationResultCode.orcInProgress:
                            State = Translate.Fmt("stat_in_prog");
                            break;
                        case OperationResultCode.orcSucceeded:
                            State = Translate.Fmt("stat_success");
                            break;
                        case OperationResultCode.orcSucceededWithErrors:
                            State = Translate.Fmt("stat_success_2");
                            break;
                        case OperationResultCode.orcFailed:
                            State = Translate.Fmt("stat_failed");
                            break;
                        case OperationResultCode.orcAborted:
                            State = Translate.Fmt("stat_abbort");
                            break;
                    }

                    State += " (0x" + string.Format("{0:X8}", Update.HResult) + ")";
                    break;

                default:
                    if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Beta) != 0)
                        State = Translate.Fmt("stat_beta" + " ");

                    if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Installed) != 0)
                    {
                        State += Translate.Fmt("stat_install");
                        if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Uninstallable) != 0)
                            State += " " + Translate.Fmt("stat_rem");
                    }
                    else if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Hidden) != 0)
                    {
                        State += Translate.Fmt("stat_block");
                        if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Downloaded) != 0)
                            State += " " + Translate.Fmt("stat_dl");
                    }
                    else
                    {
                        if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Downloaded) != 0)
                            State += Translate.Fmt("stat_dl");
                        else
                            State += Translate.Fmt("stat_pending");
                        if ((Update.Attributes & (int)MsUpdate.UpdateAttr.AutoSelect) != 0)
                            State += " " + Translate.Fmt("stat_sel");
                        if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Mandatory) != 0)
                            State += " " + Translate.Fmt("stat_mand");
                    }

                    if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Exclusive) != 0)
                        State += ", " + Translate.Fmt("stat_excl");

                    if ((Update.Attributes & (int)MsUpdate.UpdateAttr.Reboot) != 0)
                        State += ", " + Translate.Fmt("stat_reboot");
                    break;
            }


            string[] strings =
            [
                Update.Title,
                Update.Category,
                CurrentList == UpdateLists.UpdateHistory ? Update.ApplicationId : Update.Kb,
                Update.Date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern),
                FileOps.FormatSize(Update.Size),
                State
            ];

            if (mSearchFilter != null)
            {
                bool match = false;
                foreach (string str in strings)
                    if (str.IndexOf(mSearchFilter, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        match = true;
                        break;
                    }

                if (!match)
                    continue;
            }

            ListViewItem item = new(strings);
            item.SubItems[3].Tag = Update.Date;
            item.SubItems[4].Tag = Update.Size;


            item.Tag = Update;

            if (CurrentList == UpdateLists.PendingUpdates)
            {
                if (MiscFunc.ParseInt(Program.IniReadValue(Update.Kb, "BlackList", "0", INIPath)) != 0)
                    item.Font = new Font(item.Font.FontFamily, item.Font.Size, FontStyle.Strikeout);
                else if (MiscFunc.ParseInt(Program.IniReadValue(Update.Kb, "Select", "0", INIPath)) != 0)
                    item.Checked = true;
            }
            else if (CurrentList == UpdateLists.InstaledUpdates)
            {
                if (MiscFunc.ParseInt(Program.IniReadValue(Update.Kb, "Remove", "0", INIPath)) != 0)
                    item.Checked = true;
            }

            string colorStr = Program.IniReadValue(Update.Kb, "Color", "", INIPath);
            if (colorStr.Length > 0)
            {
                Color? color = MiscFunc.ParseColor(colorStr);
                if (color != null)
                    item.BackColor = (Color)color;
            }

            ListViewGroup lvg = updateView.Groups[Update.Category];
            if (lvg == null)
            {
                lvg = updateView.Groups.Add(Update.Category, Update.Category);
                ListViewExtended.setGrpState(lvg, ListViewGroupState.Collapsible);
            }

            item.Group = lvg;
            items.Add(item);
        }

        updateView.Items.AddRange(items.ToArray());

        // Note: this has caused issues in the past
        //updateView.SetGroupState(ListViewGroupState.Collapsible);
    }

    public List<MsUpdate> GetUpdates()
    {
        List<MsUpdate> updates = [];
        foreach (ListViewItem item in updateView.CheckedItems)
            updates.Add((MsUpdate)item.Tag);
        return updates;
    }

    private void SwitchList(UpdateLists List)
    {
        if (suspendChange)
            return;

        suspendChange = true;
        btnWinUpd.CheckState = List == UpdateLists.PendingUpdates ? CheckState.Checked : CheckState.Unchecked;
        btnInstalled.CheckState = List == UpdateLists.InstaledUpdates ? CheckState.Checked : CheckState.Unchecked;
        btnHidden.CheckState = List == UpdateLists.HiddenUpdates ? CheckState.Checked : CheckState.Unchecked;
        btnHistory.CheckState = List == UpdateLists.UpdateHistory ? CheckState.Checked : CheckState.Unchecked;
        suspendChange = false;

        CurrentList = List;

        updateView.Columns[2].Text = CurrentList == UpdateLists.UpdateHistory
            ? Translate.Fmt("col_app_id")
            : Translate.Fmt("col_kb");

        LoadList();

        UpdateState();

        lblSupport.Visible = false;
    }

    private void UpdateState()
    {
        checkChecks = false;

        bool isChecked = updateView.CheckedItems.Count > 0;

        bool busy = agent.IsBusy();
        btnCancel.Visible = busy;
        progTotal.Visible = busy;
        lblStatus.Visible = busy;

        bool isValid = agent.IsValid();
        bool isValid2 = isValid || chkManual.Checked;

        bool admin = MiscFunc.IsAdministrator() || !MiscFunc.IsRunningAsUwp();

        bool enable = agent.IsActive() && !busy;
        btnSearch.Enabled = enable;
        btnDownload.Enabled = isChecked && enable && isValid2 && CurrentList == UpdateLists.PendingUpdates;
        btnInstall.Enabled = isChecked && admin && enable && isValid2 && CurrentList == UpdateLists.PendingUpdates;
        btnUnInstall.Enabled = isChecked && admin && enable && CurrentList == UpdateLists.InstaledUpdates;
        btnHide.Enabled = isChecked && enable && isValid &&
                          (CurrentList == UpdateLists.PendingUpdates || CurrentList == UpdateLists.HiddenUpdates);
        btnGetLink.Enabled = isChecked && CurrentList != UpdateLists.UpdateHistory;
    }

    private void BuildToolsMenu()
    {
        wuauMenu = new MenuItem();
        wuauMenu.Text = Translate.Fmt("menu_wuau");
        wuauMenu.Checked = agent.TestWuAuServ();
        wuauMenu.Click += menuWuAu_Click;
        mToolsMenu.MenuItems.Add(wuauMenu);
        mToolsMenu.MenuItems.Add(new MenuItem("-"));

        if (Directory.Exists(Program.GetToolsPath()))
        {
            foreach (string subDir in Directory.GetDirectories(Program.GetToolsPath()))
            {
                string Name = Path.GetFileName(subDir);
                string INIPath = subDir + @"\" + Name + ".ini";

                MenuItem toolMenu = new();
                toolMenu.Text = Program.IniReadValue("Root", "Name", Name, INIPath);

                string Exec = Program.IniReadValue("Root", "Exec", "", INIPath);
                bool Silent = MiscFunc.ParseInt(Program.IniReadValue("Root", "Silent", "0", INIPath)) != 0;
                if (Exec.Length > 0)
                {
                    toolMenu.Click += delegate(object sender, EventArgs e)
                    {
                        menuExec_Click(sender, e, Exec, subDir, Silent);
                    };
                }
                else
                {
                    int count = MiscFunc.ParseInt(Program.IniReadValue("Root", "Entries", "", INIPath), 99);
                    for (int i = 1; i <= count; i++)
                    {
                        string name = Program.IniReadValue("Entry" + i, "Name", "", INIPath);
                        if (name.Length == 0)
                        {
                            if (count != 99)
                                continue;
                            break;
                        }

                        MenuItem subMenu = new();
                        subMenu.Text = name;

                        string exec = Program.IniReadValue("Entry" + i, "Exec", "", INIPath);
                        bool silent = MiscFunc.ParseInt(Program.IniReadValue("Entry" + i, "Silent", "0", INIPath)) != 0;
                        subMenu.Click += delegate(object sender, EventArgs e)
                        {
                            menuExec_Click(sender, e, exec, subDir, silent);
                        };

                        toolMenu.MenuItems.Add(subMenu);
                    }
                }

                mToolsMenu.MenuItems.Add(toolMenu);
            }

            mToolsMenu.MenuItems.Add(new MenuItem("-"));
        }

        MenuItem refreshMenu = new();
        refreshMenu.Text = Translate.Fmt("menu_refresh");
        refreshMenu.Click += menuRefresh_Click;
        mToolsMenu.MenuItems.Add(refreshMenu);
    }

    private void menuExec_Click(object Sender, EventArgs e, string exec, string dir, bool silent = false)
    {
        ProcessStartInfo startInfo = Program.PrepExec(exec, silent);
        startInfo.WorkingDirectory = dir;
        if (!Program.DoExec(startInfo))
            MessageBox.Show(Translate.Fmt("msg_tool_err"), Program.MName);
    }

    private void menuExit_Click(object Sender, EventArgs e)
    {
        Application.Exit();
    }

    private void menuAbout_Click(object Sender, EventArgs e)
    {
        string About = "";
        About += "Author: \tDavid Xanatos\r\n";
        About += "Licence: \tGNU General Public License v3\r\n";
        About += string.Format("Version: \t{0}\r\n", Program.MVersion);
        About += "\r\n";
        About += "Source: \thttps://github.com/DavidXanatos/wumgr\r\n";
        About += "\r\n";
        About += "Icons from: https://icons8.com/";
        MessageBox.Show(About, Program.MName);
    }

    private void menuWuAu_Click(object Sender, EventArgs e)
    {
        wuauMenu.Checked = !wuauMenu.Checked;
        if (wuauMenu.Checked)
        {
            agent.EnableWuAuServ();
            agent.Init();
        }
        else
        {
            agent.UnInit();
            agent.EnableWuAuServ(false);
        }

        UpdateState();
    }

    private void menuRefresh_Click(object Sender, EventArgs e)
    {
        IntPtr MenuHandle = GetSystemMenu(Handle, false); // Note: to restore default set true
        RemoveMenu(MenuHandle, 6, MF_BYPOSITION);
        mToolsMenu.MenuItems.Clear();
        BuildToolsMenu();
        InsertMenu(MenuHandle, 6, MF_BYPOSITION | MF_POPUP, (int)mToolsMenu.Handle, Translate.Fmt("menu_tools"));
    }

    private void btnWinUpd_CheckedChanged(object sender, EventArgs e)
    {
        SwitchList(UpdateLists.PendingUpdates);
    }

    private void btnInstalled_CheckedChanged(object sender, EventArgs e)
    {
        SwitchList(UpdateLists.InstaledUpdates);
    }

    private void btnHidden_CheckedChanged(object sender, EventArgs e)
    {
        SwitchList(UpdateLists.HiddenUpdates);
    }

    private void btnHistory_CheckedChanged(object sender, EventArgs e)
    {
        if (agent.IsActive())
            agent.UpdateHistory();
        SwitchList(UpdateLists.UpdateHistory);
    }

    private void btnSearch_Click(object sender, EventArgs e)
    {
        if (!agent.IsActive() || agent.IsBusy())
            return;
        WuAgent.RetCodes ret = WuAgent.RetCodes.Undefined;
        if (chkOffline.Checked)
            ret = agent.SearchForUpdates(chkDownload.Checked, chkOld.Checked);
        else
            ret = agent.SearchForUpdates(dlSource.Text, chkOld.Checked);
        ShowResult(WuAgent.AgentOperation.CheckingUpdates, ret);
    }

    private void btnDownload_Click(object sender, EventArgs e)
    {
        if (!chkManual.Checked && !MiscFunc.IsAdministrator())
        {
            MessageBox.Show(Translate.Fmt("msg_admin_dl"), Program.MName);
            return;
        }

        if (!agent.IsActive() || agent.IsBusy())
            return;
        WuAgent.RetCodes ret = WuAgent.RetCodes.Undefined;
        if (chkManual.Checked)
            ret = agent.DownloadUpdatesManually(GetUpdates());
        else
            ret = agent.DownloadUpdates(GetUpdates());
        ShowResult(WuAgent.AgentOperation.DownloadingUpdates, ret);
    }

    private void btnInstall_Click(object sender, EventArgs e)
    {
        if (!MiscFunc.IsAdministrator())
        {
            MessageBox.Show(Translate.Fmt("msg_admin_inst"), Program.MName);
            return;
        }

        if (!agent.IsActive() || agent.IsBusy())
            return;
        WuAgent.RetCodes ret = WuAgent.RetCodes.Undefined;
        if (chkManual.Checked)
            ret = agent.DownloadUpdatesManually(GetUpdates(), true);
        else
            ret = agent.DownloadUpdates(GetUpdates(), true);
        ShowResult(WuAgent.AgentOperation.InstallingUpdates, ret);
    }

    private void btnUnInstall_Click(object sender, EventArgs e)
    {
        if (!MiscFunc.IsAdministrator())
        {
            MessageBox.Show(Translate.Fmt("msg_admin_rem"), Program.MName);
            return;
        }

        if (!agent.IsActive() || agent.IsBusy())
            return;
        WuAgent.RetCodes ret = WuAgent.RetCodes.Undefined;
        ret = agent.UnInstallUpdatesManually(GetUpdates());
        ShowResult(WuAgent.AgentOperation.RemovingUpdates, ret);
    }

    private void btnHide_Click(object sender, EventArgs e)
    {
        if (!agent.IsActive() || agent.IsBusy())
            return;
        switch (CurrentList)
        {
            case UpdateLists.PendingUpdates:
                agent.HideUpdates(GetUpdates(), true);
                break;
            case UpdateLists.HiddenUpdates:
                agent.HideUpdates(GetUpdates(), false);
                break;
        }
    }

    private void btnGetLink_Click(object sender, EventArgs e)
    {
        string Links = "";
        foreach (MsUpdate Update in GetUpdates())
        {
            Links += Update.Title + "\r\n";
            foreach (string url in Update.Downloads)
                Links += url + "\r\n";
            Links += "\r\n";
        }

        if (Links.Length != 0)
        {
            Clipboard.SetText(Links);
            AppLog.Line("Update Download Links copyed to clipboard");
        }
        else
        {
            AppLog.Line("No updates selected");
        }
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        agent.CancelOperations();
    }

    private string GetOpStr(WuAgent.AgentOperation op)
    {
        switch (op)
        {
            case WuAgent.AgentOperation.CheckingUpdates: return Translate.Fmt("op_check");
            case WuAgent.AgentOperation.PreparingCheck: return Translate.Fmt("op_prep");
            case WuAgent.AgentOperation.PreparingUpdates:
            case WuAgent.AgentOperation.DownloadingUpdates: return Translate.Fmt("op_dl");
            case WuAgent.AgentOperation.InstallingUpdates: return Translate.Fmt("op_inst");
            case WuAgent.AgentOperation.RemovingUpdates: return Translate.Fmt("op_rem");
            case WuAgent.AgentOperation.CancelingOperation: return Translate.Fmt("op_cancel");
        }

        return Translate.Fmt("op_unk");
    }

    private void OnProgress(object sender, WuAgent.ProgressArgs args)
    {
        string Status = GetOpStr(agent.CurOperation());

        if (args.TotalCount == -1)
        {
            progTotal.Style = ProgressBarStyle.Marquee;
            progTotal.MarqueeAnimationSpeed = 30;
            Status += "...";
        }
        else
        {
            progTotal.Style = ProgressBarStyle.Continuous;
            progTotal.MarqueeAnimationSpeed = 0;

            if (args.TotalPercent >= 0 && args.TotalPercent <= 100)
                progTotal.Value = args.TotalPercent;

            if (args.TotalCount > 1)
                Status += " " + args.CurrentIndex + "/" + args.TotalCount + " ";

            //if (args.UpdatePercent != 0)
            //    Status += " " + args.UpdatePercent + "%";
        }

        lblStatus.Text = Status;
        toolTip.SetToolTip(lblStatus, args.Info);

        UpdateState();
    }

    private void OnUpdates(object sender, WuAgent.UpdatesArgs args)
    {
        UpdateCounts();
        if (args.Found) // if (agent.CurOperation() == WuAgent.AgentOperation.CheckingUpdates)
        {
            LastCheck = DateTime.Now;
            SetConfig("LastCheck", LastCheck.ToString());
            SwitchList(UpdateLists.PendingUpdates);
        }
        else
        {
            LoadList();

            if (MiscFunc.ParseInt(Program.IniReadValue("Options", "Refresh", "0")) == 1 &&
                (agent.CurOperation() == WuAgent.AgentOperation.InstallingUpdates ||
                 agent.CurOperation() == WuAgent.AgentOperation.RemovingUpdates))
                doUpdte = true;
        }
    }

    private void OnFinished(object sender, WuAgent.FinishedArgs args)
    {
        UpdateState();
        lblStatus.Text = "";
        toolTip.SetToolTip(lblStatus, "");

        ShowResult(args.Op, args.Ret, args.RebootNeeded);
    }

    private void ShowResult(WuAgent.AgentOperation op, WuAgent.RetCodes ret, bool reboot = false)
    {
        if (op == WuAgent.AgentOperation.DownloadingUpdates && chkManual.Checked)
        {
            if (ret == WuAgent.RetCodes.Success)
            {
                MessageBox.Show(Translate.Fmt("msg_dl_done", agent.DlPath), Program.MName, MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (ret == WuAgent.RetCodes.DownloadFailed)
            {
                MessageBox.Show(Translate.Fmt("msg_dl_err", agent.DlPath), Program.MName, MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }
        }

        if (op == WuAgent.AgentOperation.InstallingUpdates && reboot)
        {
            if (ret == WuAgent.RetCodes.Success)
            {
                MessageBox.Show(Translate.Fmt("msg_inst_done", agent.DlPath), Program.MName, MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (ret == WuAgent.RetCodes.DownloadFailed)
            {
                MessageBox.Show(Translate.Fmt("msg_inst_err", agent.DlPath), Program.MName, MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }
        }

        string status = "";
        switch (ret)
        {
            case WuAgent.RetCodes.Success:
            case WuAgent.RetCodes.Aborted:
            case WuAgent.RetCodes.InProgress: return;
            case WuAgent.RetCodes.AccessError:
                status = Translate.Fmt("err_admin");
                break;
            case WuAgent.RetCodes.Busy:
                status = Translate.Fmt("err_busy");
                break;
            case WuAgent.RetCodes.DownloadFailed:
                status = Translate.Fmt("err_dl");
                break;
            case WuAgent.RetCodes.InstallFailed:
                status = Translate.Fmt("err_inst");
                break;
            case WuAgent.RetCodes.NoUpdated:
                status = Translate.Fmt("err_no_sel");
                break;
            case WuAgent.RetCodes.InternalError:
                status = Translate.Fmt("err_int");
                break;
            case WuAgent.RetCodes.FileNotFound:
                status = Translate.Fmt("err_file");
                break;
        }

        string action = GetOpStr(op);

        ResultShown = true;
        MessageBox.Show(Translate.Fmt("msg_err", action, status), Program.MName, MessageBoxButtons.OK,
            MessageBoxIcon.Error);
        ResultShown = false;
    }

    private void dlSource_SelectedIndexChanged(object sender, EventArgs e)
    {
        SetConfig("Source", dlSource.Text);
    }

    private void chkOffline_CheckedChanged(object sender, EventArgs e)
    {
        dlSource.Enabled = !chkOffline.Checked;
        chkDownload.Enabled = chkOffline.Checked;

        SetConfig("Offline", chkOffline.Checked ? "1" : "0");
    }

    private void chkDownload_CheckedChanged(object sender, EventArgs e)
    {
        SetConfig("Download", chkDownload.Checked ? "1" : "0");
    }

    private void chkOld_CheckedChanged(object sender, EventArgs e)
    {
        SetConfig("IncludeOld", chkOld.Checked ? "1" : "0");
    }

    private void chkDrivers_CheckStateChanged(object sender, EventArgs e)
    {
        if (mSuspendUpdate)
            return;
        Gpo.ConfigDriverAu((int)chkDrivers.CheckState);
    }

    private void dlShDay_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (mSuspendUpdate)
            return;
        Gpo.ConfigAU(Gpo.AuOptions.Scheduled, dlShDay.SelectedIndex, dlShTime.SelectedIndex);
    }

    private void dlShTime_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (mSuspendUpdate)
            return;
        Gpo.ConfigAU(Gpo.AuOptions.Scheduled, dlShDay.SelectedIndex, dlShTime.SelectedIndex);
    }

    private void radGPO_CheckedChanged(object sender, EventArgs e)
    {
        dlShDay.Enabled = dlShTime.Enabled = radSchedule.Checked;

        if (radDisable.Checked)
            switch (mGPORespect)
            {
                case Gpo.Respect.Partial:
                    if (chkBlockMS.Checked)
                    {
                        chkDisableAU.Enabled = true;
                        break;
                    }

                    goto case Gpo.Respect.None;
                case Gpo.Respect.None:
                    chkDisableAU.Enabled = false;
                    chkDisableAU.Checked = true;
                    break;
                case Gpo.Respect.Full: // we can do whatever we want
                    chkDisableAU.Enabled = mWinVersion >= 10;
                    break;
            }
        else
            chkDisableAU.Enabled = false;

        if (mSuspendUpdate)
            return;

        if (radDisable.Checked)
        {
            if (chkDisableAU.Checked)
            {
                bool test = Gpo.GetDisableAu();
                Gpo.DisableAu(true);
                if (!test)
                    MessageBox.Show(Translate.Fmt("msg_disable_au"));
            }

            Gpo.ConfigAU(Gpo.AuOptions.Disabled);
        }
        else
        {
            chkDisableAU.Checked = false; // Note: this triggers chkDisableAU_CheckedChanged

            if (radNotify.Checked)
                Gpo.ConfigAU(Gpo.AuOptions.Notification);
            else if (radDownload.Checked)
                Gpo.ConfigAU(Gpo.AuOptions.Download);
            else if (radSchedule.Checked)
                Gpo.ConfigAU(Gpo.AuOptions.Scheduled, dlShDay.SelectedIndex, dlShTime.SelectedIndex);
            else //if (radDefault.Checked)
                Gpo.ConfigAU(Gpo.AuOptions.Default);
        }
    }

    private void chkBlockMS_CheckedChanged(object sender, EventArgs e)
    {
        if (mSuspendUpdate)
            return;

        if (radDisable.Checked && mGPORespect == Gpo.Respect.Partial)
        {
            if (chkBlockMS.Checked)
            {
                chkDisableAU.Enabled = true;
            }
            else
            {
                if (!chkDisableAU.Checked)
                    switch (MessageBox.Show(Translate.Fmt("msg_gpo"), Program.MName, MessageBoxButtons.YesNoCancel))
                    {
                        case DialogResult.Yes:
                            chkDisableAU.Checked = true; // Note: this triggers chkDisableAU_CheckedChanged
                            break;
                        case DialogResult.No:
                            radDefault.Checked = true;
                            break;
                        case DialogResult.Cancel:
                            mSuspendUpdate = true;
                            chkBlockMS.Checked = true;
                            mSuspendUpdate = false;
                            return;
                    }

                chkDisableAU.Enabled = false;
            }
        }

        Gpo.BlockMs(chkBlockMS.Checked);
    }

    private void chkDisableAU_CheckedChanged(object sender, EventArgs e)
    {
        if (chkDisableAU.Checked)
        {
            chkHideWU.Checked = true;
            chkHideWU.Enabled = false;
        }
        else
        {
            //chkHideWU.Checked = false;
            chkHideWU.Enabled = true;
        }

        if (mSuspendUpdate)
            return;
        bool test = Gpo.GetDisableAu();
        Gpo.DisableAu(chkDisableAU.Checked);
        if (test != chkDisableAU.Checked)
            MessageBox.Show(Translate.Fmt("msg_disable_au"));
    }

    private void chkAutoRun_CheckedChanged(object sender, EventArgs e)
    {
        notifyIcon.Visible = dlAutoCheck.Enabled = chkAutoRun.Checked;
        AutoUpdate = chkAutoRun.Checked ? (AutoUpdateOptions)dlAutoCheck.SelectedIndex : AutoUpdateOptions.No;
        if (mSuspendUpdate)
            return;
        if (chkAutoRun.CheckState == CheckState.Indeterminate)
            return;
        if (MiscFunc.IsRunningAsUwp())
        {
            if (chkAutoRun.CheckState == CheckState.Checked)
            {
                mSuspendUpdate = true;
                chkAutoRun.CheckState = CheckState.Indeterminate;
                mSuspendUpdate = false;
            }

            return;
        }

        Program.AutoStart(chkAutoRun.Checked);
    }

    private void dlAutoCheck_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (mSuspendUpdate)
            return;
        SetConfig("AutoUpdate", dlAutoCheck.SelectedIndex.ToString());
        AutoUpdate = (AutoUpdateOptions)dlAutoCheck.SelectedIndex;
    }

    private void chkNoUAC_CheckedChanged(object sender, EventArgs e)
    {
        if (mSuspendUpdate)
            return;
        Program.SkipUacEnable(chkNoUAC.Checked);
    }

    private void chkMsUpd_CheckedChanged(object sender, EventArgs e)
    {
        if (mSuspendUpdate)
            return;
        string source = dlSource.Text;
        agent.EnableService(WuAgent.MsUpdGuid, chkMsUpd.Checked);
        LoadProviders(source);
    }

    private void chkManual_CheckedChanged(object sender, EventArgs e)
    {
        UpdateState();
        SetConfig("Manual", chkManual.Checked ? "1" : "0");
    }

    private void chkHideWU_CheckedChanged(object sender, EventArgs e)
    {
        if (mSuspendUpdate)
            return;
        Gpo.HideUpdatePage(chkHideWU.Checked);
    }

    private void chkStore_CheckedChanged(object sender, EventArgs e)
    {
        if (mSuspendUpdate)
            return;
        Gpo.SetStoreAu(chkStore.Checked);
    }

    private void updateView_SelectedIndexChanged(object sender, EventArgs e)
    {
        lblSupport.Visible = false;
        if (updateView.SelectedItems.Count == 1)
        {
            MsUpdate Update = (MsUpdate)updateView.SelectedItems[0].Tag;
            if (Update.Kb != null && Update.Kb.Length > 2)
            {
                lblSupport.Links[0].LinkData = "https://support.microsoft.com/help/" + Update.Kb.Substring(2);
                lblSupport.Links[0].Visited = false;
                lblSupport.Visible = true;
                toolTip.SetToolTip(lblSupport, lblSupport.Links[0].LinkData.ToString());
            }
        }
    }

    private void lblSupport_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        string target = e.Link.LinkData as string;
        Process.Start(target);
    }


    public string GetConfig(string name, string def = "")
    {
        return Program.IniReadValue("Options", name, def);
        //var subKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Xanatos\Windows Update Manager", true);
        //return subKey.GetValue(name, def).ToString();
    }

    public void SetConfig(string name, string value)
    {
        if (mSuspendUpdate)
            return;
        Program.IniWriteValue("Options", name, value);
        //var subKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Xanatos\Windows Update Manager", true);
        //subKey.SetValue(name, value);
    }

    [DllImport("User32.dll")]
    public static extern int SetForegroundWindow(int hWnd);

    private void notifyIcon_BalloonTipClicked(object sender, EventArgs e)
    {
        if (!allowshowdisplay)
        {
            allowshowdisplay = true;
            Show();
        }

        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;
        SetForegroundWindow(Handle.ToInt32());
    }

    private void updateView_ColumnClick(object sender, ColumnClickEventArgs e)
    {
        if (updateView.ListViewItemSorter == null)
            updateView.ListViewItemSorter = new ListViewItemComparer();
        ((ListViewItemComparer)updateView.ListViewItemSorter).Update(e.Column);
        updateView.Sort();
    }


    private void Localize()
    {
        btnWinUpd.Text = Translate.Fmt("lbl_fnd_upd", 0);
        btnInstalled.Text = Translate.Fmt("lbl_inst_upd", 0);
        btnHidden.Text = Translate.Fmt("lbl_block_upd", 0);
        btnHistory.Text = Translate.Fmt("lbl_old_upd", 0);

        toolTip.SetToolTip(btnSearch, Translate.Fmt("tip_search"));
        toolTip.SetToolTip(btnInstall, Translate.Fmt("tip_inst"));
        toolTip.SetToolTip(btnDownload, Translate.Fmt("tip_dl"));
        toolTip.SetToolTip(btnHide, Translate.Fmt("tip_hide"));
        toolTip.SetToolTip(btnGetLink, Translate.Fmt("tip_lnk"));
        toolTip.SetToolTip(btnUnInstall, Translate.Fmt("tip_rem"));
        toolTip.SetToolTip(btnCancel, Translate.Fmt("tip_cancel"));

        updateView.Columns[0].Text = Translate.Fmt("col_title");
        updateView.Columns[1].Text = Translate.Fmt("col_cat");
        updateView.Columns[2].Text = Translate.Fmt("col_kb");
        updateView.Columns[3].Text = Translate.Fmt("col_date");
        updateView.Columns[4].Text = Translate.Fmt("col_site");
        updateView.Columns[5].Text = Translate.Fmt("col_stat");

        chkGrupe.Text = Translate.Fmt("lbl_group");
        chkAll.Text = Translate.Fmt("lbl_all");

        lblSupport.Text = Translate.Fmt("lbl_support");
        lblPatreon.Text = Translate.Fmt("lbl_patreon");
        //string cc = "";
        //toolTip.SetToolTip(lblPatreon, cc);

        lblSearch.Text = Translate.Fmt("lbl_search");

        tabOptions.Text = Translate.Fmt("lbl_opt");

        chkOffline.Text = Translate.Fmt("lbl_off");
        chkDownload.Text = Translate.Fmt("lbl_dl");
        chkManual.Text = Translate.Fmt("lbl_man");
        chkOld.Text = Translate.Fmt("lbl_old");
        chkMsUpd.Text = Translate.Fmt("lbl_ms");

        gbStartup.Text = Translate.Fmt("lbl_start");
        chkAutoRun.Text = Translate.Fmt("lbl_auto");
        dlAutoCheck.Items.Clear();
        dlAutoCheck.Items.Add(Translate.Fmt("lbl_ac_no"));
        dlAutoCheck.Items.Add(Translate.Fmt("lbl_ac_day"));
        dlAutoCheck.Items.Add(Translate.Fmt("lbl_ac_week"));
        dlAutoCheck.Items.Add(Translate.Fmt("lbl_ac_month"));
        chkNoUAC.Text = Translate.Fmt("lbl_uac");


        tabAU.Text = Translate.Fmt("lbl_au");

        chkBlockMS.Text = Translate.Fmt("lbl_block_ms");
        radDisable.Text = Translate.Fmt("lbl_au_off");
        chkDisableAU.Text = Translate.Fmt("lbl_au_dissable");
        radNotify.Text = Translate.Fmt("lbl_au_notify");
        radDownload.Text = Translate.Fmt("lbl_au_dl");
        radSchedule.Text = Translate.Fmt("lbl_au_time");
        radDefault.Text = Translate.Fmt("lbl_au_def");
        chkHideWU.Text = Translate.Fmt("lbl_hide");
        chkStore.Text = Translate.Fmt("lbl_store");
        chkDrivers.Text = Translate.Fmt("lbl_drv");
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.F))
        {
            panelList.RowStyles[2].Height = mSearchBoxHeight;
            txtFilter.SelectAll();
            txtFilter.Focus();
            return true;
        }

        if (keyData == (Keys.Control | Keys.C))
        {
            string Info = "";
            foreach (ListViewItem item in updateView.SelectedItems)
            {
                if (Info.Length != 0)
                    Info += "\r\n";
                Info += item.Text;
                for (int i = 1; i < item.SubItems.Count; i++)
                    Info += "; " + item.SubItems[i].Text;
            }

            if (Info.Length != 0)
                Clipboard.SetText(Info);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void btnSearchOff_Click(object sender, EventArgs e)
    {
        panelList.RowStyles[2].Height = 0;
        mSearchFilter = null;
        LoadList();
    }

    private void txtFilter_TextChanged(object sender, EventArgs e)
    {
        mSearchFilter = txtFilter.Text;
        bUpdateList = true;
    }

    private void chkGrupe_CheckedChanged(object sender, EventArgs e)
    {
        if (mSuspendUpdate)
            return;

        updateView.ShowGroups = chkGrupe.Checked;
        SetConfig("GroupUpdates", chkGrupe.Checked ? "1" : "0");
    }

    private void chkAll_CheckedChanged(object sender, EventArgs e)
    {
        if (ignoreChecks)
            return;

        ignoreChecks = true;

        foreach (ListViewItem item in updateView.Items)
            item.Checked = chkAll.Checked;

        ignoreChecks = false;

        checkChecks = true;
    }

    private void updateView_ItemChecked(object sender, ItemCheckedEventArgs e)
    {
        if (ignoreChecks)
            return;

        ignoreChecks = true;

        if (updateView.CheckedItems.Count == 0)
            chkAll.CheckState = CheckState.Unchecked;
        else if (updateView.CheckedItems.Count == updateView.Items.Count)
            chkAll.CheckState = CheckState.Checked;
        else
            chkAll.CheckState = CheckState.Indeterminate;

        ignoreChecks = false;

        checkChecks = true;
    }

    private void lblPatreon_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start("https://github.com/DavidXanatos/wumgr");
    }

    private enum AutoUpdateOptions
    {
        No = 0,
        EveryDay,
        EveryWeek,
        EveryMonth
    }

    private enum UpdateLists
    {
        PendingUpdates,
        InstaledUpdates,
        HiddenUpdates,
        UpdateHistory
    }

    // Implements the manual sorting of items by columns.
    private class ListViewItemComparer : IComparer
    {
        private int col;
        private int inv;

        public ListViewItemComparer()
        {
            col = 0;
            inv = 1;
        }

        public int Compare(object x, object y)
        {
            if (col == 3) // date
                return ((DateTime)((ListViewItem)y).SubItems[col].Tag).CompareTo(
                    (DateTime)((ListViewItem)x).SubItems[col].Tag) * inv;
            if (col == 4) // size
                return ((decimal)((ListViewItem)y).SubItems[col].Tag).CompareTo(
                    (decimal)((ListViewItem)x).SubItems[col].Tag) * inv;
            return string.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text) * inv;
        }

        public void Update(int column)
        {
            if (col == column)
                inv = -inv;
            else
                inv = 1;
            col = column;
        }
    }
}

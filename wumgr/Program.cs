#region

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using TaskScheduler;

#endregion

namespace wumgr;

internal static class Program
{
    private static string[] args;
    private static bool mConsole;
    public static string mVersion = "0.0";
    public static string mName = "Update Manager for Windows";
    private static readonly string nTaskName = "WuMgrNoUAC";
    public static string appPath = "";
    public static string wrkPath = "";
    private static WuAgent Agent;
    public static PipeIPC ipc;

    private static string GetINIPath()
    {
        return wrkPath + @"\wumgr.ini";
    }

    public static string GetToolsPath()
    {
        return appPath + @"\Tools";
    }


    /// <summary>
    ///     Der Haupteinstiegspunkt für die Anwendung.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        Program.args = args;

        mConsole = WinConsole.Initialize(TestArg("-console"));

        if (TestArg("-help") || TestArg("/?"))
        {
            ShowHelp();
            return;
        }

        if (TestArg("-dbg_wait"))
            MessageBox.Show(@"Waiting for debugger. (press ok when attached)");

        Console.WriteLine(@"Starting...");

        appPath = Path.GetDirectoryName(Application.ExecutablePath);
        Assembly assembly = Assembly.GetExecutingAssembly();
        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
        mVersion = fvi.FileMajorPart + "." + fvi.FileMinorPart;
        if (fvi.FileBuildPart != 0)
            mVersion += (char)('a' + (fvi.FileBuildPart - 1));

        wrkPath = appPath;

        Translate.Load(IniReadValue("Options", "Lang"));

        AppLog Log = new();
        AppLog.Line("{0}, Version v{1} by David Xanatos", mName, mVersion);
        AppLog.Line("This Tool is Open Source under the GNU General Public License, Version 3\r\n");

        ipc = new PipeIPC("wumgr_pipe");

        PipeIPC.PipeClient client = ipc.Connect(100);
        if (client != null)
        {
            AppLog.Line("Application is already running.");
            client.Send("show");
            string ret = client.Read(1000);
            if (!ret.Equals("ok", StringComparison.CurrentCultureIgnoreCase))
                MessageBox.Show(Translate.fmt("msg_running"));
            return;
        }

        if (!MiscFunc.IsAdministrator() && !MiscFunc.IsDebugging())
        {
            Console.WriteLine(@"Trying to get admin privileges...");

            if (SkipUacRun())
            {
                Application.Exit();
                return;
            }

            if (!MiscFunc.IsRunningAsUwp())
            {
                Console.WriteLine(@"Trying to start with 'runas'...");
                // Restart program and run as admin
                string exeName = Process.GetCurrentProcess().MainModule!.FileName;
                string arguments = "\"" + string.Join("\" \"", args) + "\"";
                ProcessStartInfo startInfo = new(exeName, arguments);
                startInfo.UseShellExecute = true;
                startInfo.Verb = "runas";
                try
                {
                    Process.Start(startInfo);
                    Application.Exit();
                    return;
                }
                catch
                {
                    //MessageBox.Show(Translate.fmt("msg_admin_req", mName), mName);
                    AppLog.Line("Administrator privileges are required in order to install updates.");
                }
            }
        }

        if (!FileOps.TestWrite(GetINIPath()))
        {
            Console.WriteLine("Can't write to default working directory.");

            string downloadFolder = KnownFolders.GetPath(KnownFolder.Downloads) ??
                                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";

            wrkPath = downloadFolder + @"\WuMgr";
            try
            {
                if (!Directory.Exists(wrkPath))
                    Directory.CreateDirectory(wrkPath);
            }
            catch
            {
                MessageBox.Show(Translate.fmt("msg_ro_wrk_dir", wrkPath), mName);
            }
        }

        AppLog.Line("Working Directory: {0}", wrkPath);
        Agent = new WuAgent();
        ExecOnStart();
        Agent.Init();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new WuMgr());
        Agent.UnInit();
        ExecOnClose();
    }

    private static void ExecOnStart()
    {
        string ToolsINI = GetToolsPath() + @"\Tools.ini";

        if (int.Parse(IniReadValue("OnStart", "EnableWuAuServ", "0", ToolsINI)) != 0)
            Agent.EnableWuAuServ();

        string OnStart = IniReadValue("OnStart", "Exec", "", ToolsINI);
        if (OnStart.Length > 0)
            DoExec(PrepExec(OnStart, MiscFunc.parseInt(IniReadValue("OnStart", "Silent", "1", ToolsINI)) != 0), true);
    }

    private static void ExecOnClose()
    {
        string ToolsINI = GetToolsPath() + @"\Tools.ini";

        string OnClose = IniReadValue("OnClose", "Exec", "", ToolsINI);
        if (OnClose.Length > 0)
            DoExec(PrepExec(OnClose, MiscFunc.parseInt(IniReadValue("OnClose", "Silent", "1", ToolsINI)) != 0), true);

        if (int.Parse(IniReadValue("OnClose", "DisableWuAuServ", "0", ToolsINI)) != 0)
            Agent.EnableWuAuServ(false);

        // Note: With the UAC bypass the onclose parameter can be used for a local privilege escalation exploit
        if (TestArg("-NoUAC")) return;
        for (int i = 0; i < args.Length; i++)
            if (args[i].Equals("-onclose", StringComparison.CurrentCultureIgnoreCase))
                DoExec(PrepExec(args[++i]));
    }

    public static ProcessStartInfo PrepExec(string command, bool silent = true)
    {
        // -onclose """cm d.exe"" /c ping 10.70.0.1" -test
        int pos = -1;
        if (command.Length > 0 && command.Substring(0, 1) == "\"")
        {
            command = command.Remove(0, 1).Trim();
            pos = command.IndexOf("\"", StringComparison.Ordinal);
        }
        else
        {
            pos = command.IndexOf(" ", StringComparison.Ordinal);
        }

        string exec;
        string arguments = "";
        if (pos != -1)
        {
            exec = command.Substring(0, pos);
            arguments = command.Substring(pos + 1).Trim();
        }
        else
        {
            exec = command;
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = exec,
            Arguments = arguments
        };
        if (!silent) return startInfo;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        return startInfo;
    }

    public static bool DoExec(ProcessStartInfo startInfo, bool wait = false)
    {
        try
        {
            Process proc = new();
            proc.StartInfo = startInfo;
            proc.EnableRaisingEvents = true;
            proc.Start();
            if (wait)
                proc.WaitForExit();
        }
        catch
        {
            return false;
        }

        return true;
    }

    [DllImport("kernel32")]
    private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

    public static void IniWriteValue(string Section, string Key, string Value, string INIPath = null)
    {
        WritePrivateProfileString(Section, Key, Value, INIPath ?? GetINIPath());
    }

    [DllImport("kernel32")]
    private static extern int GetPrivateProfileString(string section, string key, string def, [In] [Out] char[] retVal,
        int size, string filePath);

    public static string IniReadValue(string Section, string Key, string Default = "", string INIPath = null)
    {
        char[] chars = new char[8193];
        int size = GetPrivateProfileString(Section, Key, Default, chars, 8193,
            INIPath ?? GetINIPath());
        return new string(chars, 0, size);
    }

    public static string[] IniEnumSections(string INIPath = null)
    {
        char[] chars = new char[8193];
        int size = GetPrivateProfileString(null, null, null, chars, 8193, INIPath ?? GetINIPath());
        return new string(chars, 0, size).Split('\0');
    }

    public static bool TestArg(string name)
    {
        for (int i = 0; i < args.Length; i++)
            if (args[i].Equals(name, StringComparison.CurrentCultureIgnoreCase))
                return true;
        return false;
    }

    public static string GetArg(string name)
    {
        for (int i = 0; i < args.Length; i++)
            if (args[i].Equals(name, StringComparison.CurrentCultureIgnoreCase))
            {
                string temp = args[i + 1];
                if (temp.Length > 0 && temp[0] != '-')
                    return temp;
                return "";
            }

        return null;
    }

    public static void AutoStart(bool enable)
    {
        RegistryKey subKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (enable)
        {
            string value = "\"" + Assembly.GetExecutingAssembly().Location + "\"" + " -tray";
            subKey.SetValue("wumgr", value);
        }
        else
        {
            subKey.DeleteValue("wumgr", false);
        }
    }

    public static bool IsAutoStart()
    {
        RegistryKey subKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
        return subKey?.GetValue("wumgr") != null;
    }

    public static bool IsSkipUacRun()
    {
        try
        {
            TaskScheduler.TaskScheduler service = new();
            service.Connect();
            ITaskFolder folder = service.GetFolder(@"\"); // root
            IRegisteredTask task = folder.GetTask(nTaskName);
            return task != null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return false;
    }

    public static bool SkipUacEnable(bool is_enable)
    {
        try
        {
            TaskScheduler.TaskScheduler service = new();
            service.Connect();
            ITaskFolder folder = service.GetFolder(@"\"); // root
            if (is_enable)
            {
                string exePath = Assembly.GetExecutingAssembly().Location;
                ITaskDefinition task = service.NewTask(0);
                task.RegistrationInfo.Author = "WuMgr";
                task.Principal.RunLevel = _TASK_RUNLEVEL.TASK_RUNLEVEL_HIGHEST;
                task.Settings.AllowHardTerminate = false;
                task.Settings.StartWhenAvailable = false;
                task.Settings.DisallowStartIfOnBatteries = false;
                task.Settings.StopIfGoingOnBatteries = false;
                task.Settings.MultipleInstances = _TASK_INSTANCES_POLICY.TASK_INSTANCES_PARALLEL;
                task.Settings.ExecutionTimeLimit = "PT0S";
                IExecAction action = (IExecAction)task.Actions.Create(_TASK_ACTION_TYPE.TASK_ACTION_EXEC);
                action.Path = exePath;
                action.WorkingDirectory = appPath;
                action.Arguments = "-NoUAC $(Arg0)";

                IRegisteredTask registered_task = folder.RegisterTaskDefinition(nTaskName, task,
                    (int)_TASK_CREATION.TASK_CREATE_OR_UPDATE, null, null,
                    _TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN);

                if (registered_task == null)
                    return false;

                // Note: if we run as UWP we need to adjust the file permissions for this workaround to work
                if (MiscFunc.IsRunningAsUwp())
                {
                    if (!FileOps.TakeOwn(exePath))
                        return false;

                    FileSecurity ac = File.GetAccessControl(exePath);
                    ac.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(FileOps.SID_Worls),
                        FileSystemRights.ReadAndExecute, AccessControlType.Allow));
                    File.SetAccessControl(exePath, ac);
                }
            }
            else
            {
                folder.DeleteTask(nTaskName, 0);
            }
        }
        catch (Exception err)
        {
            AppLog.Line("Enable SkipUAC Error {0}", err.ToString());
            return false;
        }

        return true;
    }

    public static bool SkipUacRun()
    {
        bool silent = true;
        try
        {
            TaskScheduler.TaskScheduler service = new();
            service.Connect();
            ITaskFolder folder = service.GetFolder(@"\"); // root
            IRegisteredTask task = folder.GetTask(nTaskName);
            silent = false;
            AppLog.Line("Trying to SkipUAC ...");
            IExecAction action = (IExecAction)task.Definition.Actions[1];
            if (action.Path.Equals(Assembly.GetExecutingAssembly().Location, StringComparison.CurrentCultureIgnoreCase))
            {
                string arguments = "\"" + string.Join("\" \"", args) + "\"";
                IRunningTask running_Task = task.RunEx(arguments, (int)_TASK_RUN_FLAGS.TASK_RUN_NO_FLAGS, 0, null);

                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(250);
                    running_Task.Refresh();
                    _TASK_STATE state = running_Task.State;
                    if (state == _TASK_STATE.TASK_STATE_RUNNING || state == _TASK_STATE.TASK_STATE_READY ||
                        state == _TASK_STATE.TASK_STATE_DISABLED)
                    {
                        if (state == _TASK_STATE.TASK_STATE_RUNNING || state == _TASK_STATE.TASK_STATE_READY)
                            return true;
                        break;
                    }
                }
            }
        }
        catch (Exception err)
        {
            if (!silent)
                AppLog.Line("SkipUAC Error {0}", err.ToString());
        }

        return false;
    }

    private static void ShowHelp()
    {
        string Message = "Available command line options\r\n";
        string[] Help =
        {
            "-tray\t\tStart in Tray",
            "-onclose [cmd]\tExecute commands when closing",
            "-update\t\tSearch for updates on start",
            "-console\t\tshow console (for debugging)",
            "-help\t\tShow this help message"
        };
        if (!mConsole)
        {
            MessageBox.Show(Message + string.Join("\r\n", Help));
        }
        else
        {
            Console.WriteLine(Message);
            for (int j = 0; j < Help.Length; j++)
                Console.WriteLine(" " + Help[j]);
        }
    }
}

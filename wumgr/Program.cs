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
using wumgr.Common;

#endregion

namespace wumgr;

internal static class Program
{
    private const string MF_N_TASK_NAME = "WuMgrNoUAC";
    private static string[] _args;
    private static bool _mConsole;
    public static string MVersion = "0.0";
    public static readonly string MName = "Update Manager for Windows";
    public static string AppPath = "";
    public static string WrkPath = "";
    private static WuAgent _agent;
    public static PipeIpc Ipc;

    private static string GetIniPath()
    {
        return WrkPath + @"\wumgr.ini";
    }

    public static string GetToolsPath()
    {
        return AppPath + @"\Tools";
    }


    /// <summary>
    ///     Der Haupteinstiegspunkt für die Anwendung.
    /// </summary>
    [STAThread]
    private static void Main(string[] mainArgs)
    {
        _args = mainArgs;

        _mConsole = WinConsole.Initialize(TestArg("-console"));

        if (TestArg("-help") || TestArg("/?"))
        {
            ShowHelp();
            return;
        }

        if (TestArg("-dbg_wait"))
            MessageBox.Show(@"Waiting for debugger. (press ok when attached)");

        Console.WriteLine(@"Starting...");

        AppPath = Path.GetDirectoryName(Application.ExecutablePath);
        Assembly assembly = Assembly.GetExecutingAssembly();
        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
        MVersion = fvi.FileMajorPart + "." + fvi.FileMinorPart;
        if (fvi.FileBuildPart != 0)
            MVersion += (char)('a' + (fvi.FileBuildPart - 1));

        WrkPath = AppPath;

        Translate.Load(IniReadValue("Options", "Lang"));

        AppLog log = new();
        AppLog.Line("{0}, Version v{1} by David Xanatos", MName, MVersion);
        AppLog.Line("This Tool is Open Source under the GNU General Public License, Version 3\r\n");

        Ipc = new PipeIpc("wumgr_pipe");

        PipeIpc.PipeClient client = Ipc.Connect(100);
        if (client != null)
        {
            AppLog.Line("Application is already running.");
            client.Send("show");
            string ret = client.Read(1000);
            if (!ret.Equals("ok", StringComparison.CurrentCultureIgnoreCase))
                MessageBox.Show(Translate.Fmt("msg_running"));
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
                string exeName = Process.GetCurrentProcess().MainModule?.FileName;
                string arguments = "\"" + string.Join("\" \"", mainArgs) + "\"";
                ProcessStartInfo startInfo = new(exeName, arguments)
                {
                    UseShellExecute = true,
                    Verb = "runas"
                };
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

        if (!FileOps.TestWrite(GetIniPath()))
        {
            Console.WriteLine(@"Can't write to default working directory.");

            string downloadFolder = KnownFolders.GetPath(KnownFolder.Downloads) ??
                                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";

            WrkPath = downloadFolder + @"\WuMgr";
            try
            {
                if (!Directory.Exists(WrkPath))
                    Directory.CreateDirectory(WrkPath);
            }
            catch
            {
                MessageBox.Show(Translate.Fmt("msg_ro_wrk_dir", WrkPath), MName);
            }
        }

        AppLog.Line("Working Directory: {0}", WrkPath);
        _agent = new WuAgent();
        ExecOnStart();
        _agent.Init();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new WuMgr());
        _agent.UnInit();
        ExecOnClose();
    }

    private static void ExecOnStart()
    {
        string toolsIni = GetToolsPath() + @"\Tools.ini";

        if (int.Parse(IniReadValue("OnStart", "EnableWuAuServ", "0", toolsIni)) != 0)
            _agent.EnableWuAuServ();

        string onStart = IniReadValue("OnStart", "Exec", "", toolsIni);
        if (onStart.Length > 0)
            DoExec(PrepExec(onStart, MiscFunc.ParseInt(IniReadValue("OnStart", "Silent", "1", toolsIni)) != 0), true);
    }

    private static void ExecOnClose()
    {
        string toolsIni = GetToolsPath() + @"\Tools.ini";

        string onClose = IniReadValue("OnClose", "Exec", "", toolsIni);
        if (onClose.Length > 0)
            DoExec(PrepExec(onClose, MiscFunc.ParseInt(IniReadValue("OnClose", "Silent", "1", toolsIni)) != 0), true);

        if (int.Parse(IniReadValue("OnClose", "DisableWuAuServ", "0", toolsIni)) != 0)
            _agent.EnableWuAuServ(false);

        // Note: With the UAC bypass the onclose parameter can be used for a local privilege escalation exploit
        if (TestArg("-NoUAC")) return;
        for (int i = 0; i < _args.Length; i++)
            if (_args[i].Equals("-onclose", StringComparison.CurrentCultureIgnoreCase))
                DoExec(PrepExec(_args[++i]));
    }

    public static ProcessStartInfo PrepExec(string command, bool silent = true)
    {
        // -onclose """cm d.exe"" /c ping 10.70.0.1" -test
        int pos;
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

    public static void IniWriteValue(string section, string key, string value, string iniPath = null)
    {
        WritePrivateProfileString(section, key, value, iniPath ?? GetIniPath());
    }

    [DllImport("kernel32")]
    private static extern int GetPrivateProfileString(string section, string key, string def, [In] [Out] char[] retVal,
        int size, string filePath);

    public static string IniReadValue(string section, string key, string @default = "", string iniPath = null)
    {
        char[] chars = new char[8193];
        int size = GetPrivateProfileString(section, key, @default, chars, chars.Length, iniPath ?? GetIniPath());
        return new string(chars, 0, size);
    }

    public static string[] IniEnumSections(string iniPath = null)
    {
        char[] chars = new char[8193];
        int size = GetPrivateProfileString(null, null, null, chars, 8193, iniPath ?? GetIniPath());
        return new string(chars, 0, size).Split('\0');
    }

    public static bool TestArg(string name)
    {
        foreach (string t in _args)
            if (t.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                return true;

        return false;
    }

    public static string GetArg(string name)
    {
        for (int i = 0; i < _args.Length; i++)
            if (_args[i].Equals(name, StringComparison.CurrentCultureIgnoreCase))
            {
                string temp = _args[i + 1];
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
            IRegisteredTask task = folder.GetTask(MF_N_TASK_NAME);
            return task != null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        return false;
    }

    public static bool SkipUacEnable(bool isEnable)
    {
        try
        {
            TaskScheduler.TaskScheduler service = new();
            service.Connect();
            ITaskFolder folder = service.GetFolder(@"\"); // root
            if (isEnable)
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
                action.WorkingDirectory = AppPath;
                action.Arguments = "-NoUAC $(Arg0)";

                IRegisteredTask registeredTask = folder.RegisterTaskDefinition(MF_N_TASK_NAME, task,
                    (int)_TASK_CREATION.TASK_CREATE_OR_UPDATE, null, null,
                    _TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN);

                if (registeredTask == null)
                    return false;

                // Note: if we run as UWP we need to adjust the file permissions for this workaround to work
                if (MiscFunc.IsRunningAsUwp())
                {
                    if (!FileOps.TakeOwn(exePath))
                        return false;

                    FileSecurity ac = File.GetAccessControl(exePath);
                    ac.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(FileOps.SidWorls),
                        FileSystemRights.ReadAndExecute, AccessControlType.Allow));
                    File.SetAccessControl(exePath, ac);
                }
            }
            else
            {
                folder.DeleteTask(MF_N_TASK_NAME, 0);
            }
        }
        catch (Exception err)
        {
            AppLog.Line("Enable SkipUAC Error {0}", err.ToString());
            return false;
        }

        return true;
    }

    private static bool SkipUacRun()
    {
        try
        {
            TaskScheduler.TaskScheduler service = new();
            service.Connect();
            ITaskFolder folder = service.GetFolder(@"\");
            IRegisteredTask task = folder.GetTask(MF_N_TASK_NAME);
            AppLog.Line("Trying to SkipUAC ...");
            IExecAction action = (IExecAction)task.Definition.Actions[1];
            if (action.Path.Equals(Assembly.GetExecutingAssembly().Location, StringComparison.CurrentCultureIgnoreCase))
            {
                string arguments = "\"" + string.Join("\" \"", _args) + "\"";
                IRunningTask runningTask = task.RunEx(arguments, (int)_TASK_RUN_FLAGS.TASK_RUN_NO_FLAGS, 0, null);

                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(250);
                    runningTask.Refresh();
                    _TASK_STATE state = runningTask.State;
                    if (state is _TASK_STATE.TASK_STATE_RUNNING or _TASK_STATE.TASK_STATE_READY)
                        return true;
                    if (state == _TASK_STATE.TASK_STATE_DISABLED)
                        break;
                }
            }
        }
        catch (Exception err)
        {
            AppLog.Line("SkipUAC Error {0}", err.ToString());
        }

        return false;
    }

    private static void ShowHelp()
    {
        string message = "Available command line options\r\n";
        string[] help =
        [
            "-tray\t\tStart in Tray",
            "-onclose [cmd]\tExecute commands when closing",
            "-update\t\tSearch for updates on start",
            "-console\t\tshow console (for debugging)",
            "-help\t\tShow this help message"
        ];
        if (!_mConsole)
        {
            MessageBox.Show(message + string.Join("\r\n", help));
        }
        else
        {
            Console.WriteLine(message);
            foreach (string t in help)
                Console.WriteLine(@" " + t);
        }
    }
}

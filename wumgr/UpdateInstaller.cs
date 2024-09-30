#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using wumgr.Common;

#endregion

namespace wumgr;

internal class UpdateInstaller
{
    private readonly Dispatcher _mDispatcher = Dispatcher.CurrentDispatcher;
    private bool _canceled;
    private bool _doInstall = true;
    private int _errorCount;
    private MultiValueDictionary<string, string> _mAllFiles;
    private int _mCurrentTask;
    private Thread _mThread;
    private List<MsUpdate> _mUpdates;
    private bool _rebootRequired;

    private void Reset()
    {
        _errorCount = 0;
        _rebootRequired = false;
        _canceled = false;
        _mCurrentTask = 0;
    }

    public bool Install(List<MsUpdate> updates, MultiValueDictionary<string, string> allFiles)
    {
        Reset();
        _mUpdates = updates;
        _mAllFiles = allFiles;
        _doInstall = true;

        NextUpdate();
        return true;
    }

    public bool UnInstall(List<MsUpdate> updates)
    {
        Reset();
        _mUpdates = updates;
        _doInstall = false;

        NextUpdate();
        return true;
    }

    public bool IsBusy()
    {
        return _mUpdates != null;
    }

    public void CancelOperations()
    {
        _canceled = true;
    }

    private void NextUpdate()
    {
        if (!_canceled && _mUpdates.Count > _mCurrentTask)
        {
            int percent = 0; // Note: there does not seam to be an easy way to get this value
            Progress?.Invoke(this,
                new WuAgent.ProgressArgs(_mUpdates.Count,
                    _mUpdates.Count == 0 ? 0 : (100 * _mCurrentTask + percent) / _mUpdates.Count, _mCurrentTask + 1,
                    percent, _mUpdates[_mCurrentTask].Title));

            if (_doInstall)
            {
                List<string> files = _mAllFiles.GetValues(_mUpdates[_mCurrentTask].Kb);

                _mThread = new Thread(RunInstall);
                _mThread.Start(files);
            }
            else
            {
                string kb = _mUpdates[_mCurrentTask].Kb;

                _mThread = new Thread(RunUnInstall);
                _mThread.Start(kb);
            }

            return;
        }

        FinishedEventArgs args = new(_errorCount, _rebootRequired)
        {
            //args.AllFiles = mAllFiles;
            Updates = _mUpdates
        };
        _mAllFiles = null;
        _mUpdates = null;
        Finished?.Invoke(this, args);
    }

    private void OnFinished(bool success, bool reboot)
    {
        if (!success)
            _errorCount++;
        if (reboot)
            _rebootRequired = true;

        _mThread.Join();
        _mThread = null;

        _mCurrentTask++;
        NextUpdate();
    }

    private void RunInstall(object parameters)
    {
        List<string> files = (List<string>)parameters;

        bool ok = true;
        bool reboot = false;

        foreach (string curFile in files)
        {
            if (_canceled)
                break;

            string file = curFile;

            AppLog.Line("Installing: {0}", file);

            try
            {
                string ext = Path.GetExtension(file);

                if (ext.Equals(".zip", StringComparison.CurrentCultureIgnoreCase))
                {
                    string path = Path.GetDirectoryName(file) + @"\files"; // + Path.GetFileNameWithoutExtension(File);

                    if (!Directory.Exists(path)) // is it already unpacked?
                        ZipFile.ExtractToDirectory(file, path);

                    string supportedExtensions = "*.msu,*.msi,*.cab,*.exe";
                    IEnumerable<string> foundFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                        .Where(s => supportedExtensions.Contains(Path.GetExtension(s).ToLower()));
                    if (!foundFiles.Any())
                        throw new FileNotFoundException("Expected file not found in zip");

                    file = foundFiles.First();
                    ext = Path.GetExtension(file);
                }

                if (_canceled)
                    break;

                int exitCode = 0;

                if (ext.Equals(".exe", StringComparison.CurrentCultureIgnoreCase))
                    exitCode = InstallExe(file);
                else if (ext.Equals(".msi", StringComparison.CurrentCultureIgnoreCase))
                    exitCode = InstallMsi(file);
                else if (ext.Equals(".msu", StringComparison.CurrentCultureIgnoreCase))
                    exitCode = InstallMsu(file);
                else if (ext.Equals(".cab", StringComparison.CurrentCultureIgnoreCase))
                    exitCode = InstallCab(file);
                else
                    throw new FileFormatException("Unknown Update format: " + ext);

                if (exitCode == 3010)
                {
                    reboot = true; // reboot requires
                }
                else if (exitCode == 1641)
                {
                    AppLog.Line("Error, reboot got initiated: {0}", file);
                    reboot = true; // reboot in initiated, WTF !!!!
                    ok = false;
                }
                else if (exitCode != 1 && exitCode != 0)
                {
                    ok = false; // some error
                }
            }
            catch (Exception e)
            {
                ok = false;
                Console.WriteLine(@"Error installing update: {0}", e.Message);
            }
        }

        _mDispatcher.BeginInvoke(new Action(() => { OnFinished(ok, reboot); }));
    }

    private int InstallExe(string fileName)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = fileName
        };

        // ToDo: load from file or make it less complex
        string name = Path.GetFileNameWithoutExtension(fileName);
        if (name.IndexOf("ndp", StringComparison.CurrentCultureIgnoreCase) == 0 ||
            name.IndexOf("OFV", StringComparison.CurrentCultureIgnoreCase) == 0 ||
            name.IndexOf("2553065", StringComparison.CurrentCultureIgnoreCase) == 0)
            startInfo.Arguments = "/q /norestart";
        else
            startInfo.Arguments = "/q /z";

        return ExecTask(startInfo);
    }

    private int InstallMsi(string fileName)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = @"%SystemRoot%\System32\msiexec.exe",
            Arguments = "/i \"" + fileName + "\" /qn /norestart"
        };

        return ExecTask(startInfo);
    }

    private int InstallMsu(string fileName)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = @"%SystemRoot%\System32\wusa.exe",
            Arguments = "\"" + fileName + "\" /quiet /norestart"
        };

        return ExecTask(startInfo);
    }

    private bool CheckCab(string fileName)
    {
        try
        {
            Process proc = new();
            proc.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\Dism.exe");
            proc.StartInfo.Arguments = "/Online /Get-PackageInfo /PackagePath:\"" + fileName + "\" /English";
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.EnableRaisingEvents = true;
            proc.Start();
            proc.WaitForExit();
            while (!proc.StandardOutput.EndOfStream)
            {
                string[] line = proc.StandardOutput.ReadLine()!.Split(':');
                if (line.Length != 2)
                    continue;

                if (!line[0].Trim().Equals("Applicable", StringComparison.CurrentCultureIgnoreCase))
                    continue;

                return line[1].Trim().Equals("Yes", StringComparison.CurrentCultureIgnoreCase);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(@"Dism error: {0}", e.Message);
        }

        return false;
    }

    private int InstallCab(string fileName)
    {
        if (!CheckCab(fileName) || _canceled)
            return 0; // update not aplicable or user canceled

        ProcessStartInfo startInfo = new()
        {
            FileName = @"%SystemRoot%\System32\Dism.exe",
            Arguments = "/Online /Quiet /NoRestart /Add-Package /PackagePath:\"" + fileName + "\" /IgnoreCheck"
        };

        return ExecTask(startInfo);
    }

    private int ExecTask(ProcessStartInfo startInfo, bool silent = true)
    {
        startInfo.FileName = Environment.ExpandEnvironmentVariables(startInfo.FileName);

        if (silent)
        {
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
        }

        Process proc = new();
        proc.StartInfo = startInfo;
        proc.EnableRaisingEvents = true;
        proc.Start();
        proc.WaitForExit();

        return proc.ExitCode;
    }

    private void RunUnInstall(object parameters)
    {
        string kb = (string)parameters;

        AppLog.Line("Uninstalling: {0}", kb);

        bool ok = true;
        bool reboot = false;

        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = @"%SystemRoot%\System32\wusa.exe",
                Arguments = "/uninstall /kb:" + kb.Substring(2) + " /norestart" // /quiet 
            };

            int exitCode = ExecTask(startInfo);

            if (exitCode == 3010 || exitCode == 1641)
            {
                reboot = true;
            }
            else if (exitCode != 1 && exitCode != 0)
            {
                AppLog.Line("Error, exit coded: {0}", exitCode);
                ok = false; // some error
            }
        }
        catch (Exception e)
        {
            ok = false;
            Console.WriteLine(@"Error removing update: {0}", e.Message);
        }

        _mDispatcher.BeginInvoke(new Action(() => { OnFinished(ok, reboot); }));
    }

    public event EventHandler<FinishedEventArgs> Finished;

    public event EventHandler<WuAgent.ProgressArgs> Progress;

    public class FinishedEventArgs(int errorCount, bool reboot) : EventArgs
    {
        public readonly bool Reboot = reboot;
        public List<MsUpdate> Updates;

        //public MultiValueDictionary<string, string> AllFiles;
        public bool Success => errorCount == 0;
    }
}

#region

using System;
using System.Collections.Generic;
using System.Windows.Threading;

#endregion

namespace wumgr.Common;

internal class AppLog
{
    private static AppLog _mInstance;
    private readonly Dispatcher _mDispatcher;
    private readonly List<string> _mLogList = new();

    public AppLog()
    {
        _mInstance = this;

        _mDispatcher = Dispatcher.CurrentDispatcher;

        Logger += LineLogger;
    }

    public static void Line(string str, params object[] args)
    {
        Line(string.Format(str, args));
    }

    public static void Line(string line)
    {
        if (_mInstance != null)
            _mInstance.LogLine(line);
    }

    private void LogLine(string line)
    {
        _mDispatcher.BeginInvoke(new Action(() =>
        {
            _mLogList.Add(line);
            while (_mLogList.Count > 100)
                _mLogList.RemoveAt(0);

            Logger?.Invoke(this, new LogEventArgs(line));
        }));
    }

    public static List<string> GetLog()
    {
        return _mInstance._mLogList;
    }

    public static event EventHandler<LogEventArgs> Logger;

    private static void LineLogger(object sender, LogEventArgs args)
    {
        Console.WriteLine(@"LOG: " + args.line);
    }

    public static AppLog GetInstance()
    {
        return _mInstance;
    }

    public class LogEventArgs(string line) : EventArgs
    {
        public string line { get; set; } = line;
    }
}
#region

using System;
using System.Collections.Generic;
using System.Windows.Threading;

#endregion

internal class AppLog
{
    private static AppLog mInstance;
    private readonly Dispatcher mDispatcher;
    private readonly List<string> mLogList = new();

    public AppLog()
    {
        mInstance = this;

        mDispatcher = Dispatcher.CurrentDispatcher;

        Logger += LineLogger;
    }

    public static void Line(string str, params object[] args)
    {
        Line(string.Format(str, args));
    }

    public static void Line(string line)
    {
        if (mInstance != null)
            mInstance.logLine(line);
    }

    public void logLine(string line)
    {
        mDispatcher.BeginInvoke(new Action(() =>
        {
            mLogList.Add(line);
            while (mLogList.Count > 100)
                mLogList.RemoveAt(0);

            Logger?.Invoke(this, new LogEventArgs(line));
        }));
    }

    public static List<string> GetLog()
    {
        return mInstance.mLogList;
    }

    public static event EventHandler<LogEventArgs> Logger;

    private static void LineLogger(object sender, LogEventArgs args)
    {
        Console.WriteLine("LOG: " + args.line);
    }

    public static AppLog GetInstance()
    {
        return mInstance;
    }

    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(string _line)
        {
            line = _line;
        }

        public string line { get; set; }
    }
}

#region

using System;
using System.Collections.Generic;
using System.IO;
using wumgr.Common;

#endregion

namespace wumgr;

internal class UpdateDownloader
{
    private bool _canceled;
    private int _mCurrentTask;
    private HttpTask _mCurTask;
    private List<Task> _mDownloads;
    private string _mInfo = "";
    private List<MsUpdate> _mUpdates;

    public bool Download(List<Task> downloads, List<MsUpdate> updates = null)
    {
        if (_mDownloads != null)
            return false;

        _canceled = false;
        _mDownloads = downloads;
        _mCurrentTask = 0;
        _mInfo = "";
        _mUpdates = updates;

        DownloadNextFile();
        return true;
    }

    public bool IsBusy()
    {
        return _mDownloads != null;
    }

    public void CancelOperations()
    {
        if (_mCurTask != null)
            _mCurTask.Cancel();
    }

    private void DownloadNextFile()
    {
        while (!_canceled && _mDownloads.Count > _mCurrentTask)
        {
            Task download = _mDownloads[_mCurrentTask];

            if (_mUpdates != null)
                foreach (MsUpdate update in _mUpdates)
                    if (update.Kb.Equals(download.Kb))
                    {
                        _mInfo = update.Title;
                        break;
                    }

            _mCurTask = new HttpTask(download.Url, download.Path, download.FileName, true); // todo update flag
            _mCurTask.Progress += OnProgress;
            _mCurTask.Finished += OnFinished;
            if (_mCurTask.Start())
                return;
            // Failed to start this task lets try another one
            _mCurrentTask++;
        }

        FinishedEventArgs args = new()
        {
            Downloads = _mDownloads
        };
        _mDownloads = null;
        args.Updates = _mUpdates;
        _mUpdates = null;
        Finished?.Invoke(this, args);
    }

    private void OnProgress(object sender, HttpTask.ProgressEventArgs args)
    {
        Progress?.Invoke(this,
            new WuAgent.ProgressArgs(_mDownloads.Count,
                _mDownloads.Count == 0 ? 0 : (100 * _mCurrentTask + args.Percent) / _mDownloads.Count, _mCurrentTask + 1,
                args.Percent, _mInfo));
    }

    private void OnFinished(object sender, HttpTask.FinishedEventArgs args)
    {
        if (!args.Cancelled)
        {
            Task download = _mDownloads[_mCurrentTask];
            if (!args.Success)
            {
                AppLog.Line("Download failed: {0}", args.GetError());
                if (_mCurTask.DlName != null && File.Exists(_mCurTask.DlPath + @"\" + _mCurTask.DlName))
                    AppLog.Line("An older version is present and will be used.");
                else
                    download.Failed = true;
            }

            download.FileName = _mCurTask.DlName;
            _mDownloads[_mCurrentTask] = download;
            _mCurTask = null;

            _mCurrentTask++;
        }
        else
        {
            _canceled = true;
        }

        DownloadNextFile();
    }

    public event EventHandler<FinishedEventArgs> Finished;

    public event EventHandler<WuAgent.ProgressArgs> Progress;

    public struct Task
    {
        public string Url;
        public string Path;
        public string FileName;
        public bool Failed;
        public string Kb;
    }

    public class FinishedEventArgs : EventArgs
    {
        public List<Task> Downloads;
        public List<MsUpdate> Updates;

        public bool Success
        {
            get
            {
                foreach (Task task in Downloads)
                    if (task.Failed)
                        return false;
                return true;
            }
        }
    }
}

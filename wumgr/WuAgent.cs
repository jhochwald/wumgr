#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Threading;
using WUApiLib;
using wumgr.Common;
using StringCollection = System.Collections.Specialized.StringCollection;

//this is required to use the Interfaces given by microsoft. 

#endregion

namespace wumgr;

internal class WuAgent
{
    public enum AgentOperation
    {
        None = 0,
        CheckingUpdates,
        PreparingCheck,
        DownloadingUpdates,
        InstallingUpdates,
        PreparingUpdates,
        RemoveingUpdates,
        CancelingOperation
    }

    public enum RetCodes
    {
        InProgress = 2,
        Success = 1,

        Undefined = 0,

        AccessError = -1,
        Busy = -2,
        DownloadFailed = -3,
        InstallFailed = -4,
        NoUpdated = -5,
        InternalError = -6,
        FileNotFound = -7,

        Abborted = -99
    }

    private static WuAgent _mInstance;
    public static string MsUpdGuid = "7971f918-a847-4430-9279-4a52d1efe18d"; // Microsoft Update
    public static string WinUpdUid = "9482f4b4-e343-43b6-b170-9a65bc822c77"; // Windows Update
    public static string WsUsUid = "3da21691-e39d-4da6-8a4b-b43877bcb1b7"; // Windows Server Update Service
    public static string
        DCatGuid = "8b24b027-1dee-babb-9a95-3517dfb9c552"; // DCat Flighting Prod - Windows Insider Program
    public static string WinStorGuid = "117cab2d-82b1-4b5a-a08c-4d62dbee7782 "; // Windows Store
    public static string
        WinStorDCat2Guid =
            "855e8a7c-ecb4-4ca3-b045-1dfa50104289"; // Windows Store (DCat Prod) - Insider Updates for Store Apps
    private readonly UpdateDownloader _mUpdateDownloader;
    private readonly UpdateInstaller _mUpdateInstaller;
    private readonly UpdateServiceManager _mUpdateServiceManager;
    private readonly UpdateSession _mUpdateSession;
    public readonly string DlPath;
    private UpdateCallback _mCallback;
    private AgentOperation _mCurOperation = AgentOperation.None;
    private readonly Dispatcher _mDispatcher;
    private WUApiLib.UpdateDownloader _mDownloader;
    private IDownloadJob _mDownloadJob;
    public readonly List<MsUpdate> MHiddenUpdates = new();
    private IInstallationJob _mInstalationJob;
    public readonly List<MsUpdate> MInstalledUpdates = new();
    private IUpdateInstaller _mInstaller;
    private bool _mIsValid;
    private readonly string _mMyOfflineSvc = "Offline Sync Service";
    private IUpdateService _mOfflineService;
    public readonly List<MsUpdate> MPendingUpdates = new();
    private ISearchJob _mSearchJob;
    public readonly StringCollection MServiceList = new();
    public readonly List<MsUpdate> MUpdateHistory = new();
    private IUpdateSearcher _mUpdateSearcher;

    public WuAgent()
    {
        _mInstance = this;
        _mDispatcher = Dispatcher.CurrentDispatcher;

        _mUpdateDownloader = new UpdateDownloader();
        _mUpdateDownloader.Finished += DownloadsFinished;
        _mUpdateDownloader.Progress += DownloadProgress;


        _mUpdateInstaller = new UpdateInstaller();
        _mUpdateInstaller.Finished += InstallFinished;
        _mUpdateInstaller.Progress += InstallProgress;

        DlPath = Program.WrkPath + @"\Updates";

        WindowsUpdateAgentInfo info = new();
        dynamic currentVersion = info.GetInfo("ApiMajorVersion").ToString().Trim() + "." +
                                 info.GetInfo("ApiMinorVersion").ToString().Trim() + " (" +
                                 info.GetInfo("ProductVersionString").ToString().Trim() + ")";
        AppLog.Line("Windows Update Agent Version: {0}", currentVersion);

        _mUpdateSession = new UpdateSession();
        _mUpdateSession.ClientApplicationID = Program.MName;
        //mUpdateSession.UserLocale = 1033; // alwys show strings in englisch

        _mUpdateServiceManager = new UpdateServiceManager();

        if (MiscFunc.ParseInt(Program.IniReadValue("Options", "LoadLists", "0")) != 0)
            LoadUpdates();
    }

    public static WuAgent GetInstance()
    {
        return _mInstance;
    }

    public bool Init()
    {
        if (!LoadServices(true))
            return false;

        _mUpdateSearcher = _mUpdateSession.CreateUpdateSearcher();

        UpdateHistory();
        return true;
    }

    public void UnInit()
    {
        ClearOffline();

        _mUpdateSearcher = null;
    }

    public bool IsActive()
    {
        return _mUpdateSearcher != null;
    }

    public bool IsBusy()
    {
        return _mCurOperation != AgentOperation.None;
    }

    private bool LoadServices(bool cleanUp = false)
    {
        try
        {
            Console.WriteLine(@"Update Services:");
            MServiceList.Clear();
            foreach (IUpdateService service in _mUpdateServiceManager.Services)
            {
                if (service.Name == _mMyOfflineSvc)
                {
                    if (cleanUp)
                        try
                        {
                            _mUpdateServiceManager.RemoveService(service.ServiceID);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }

                    continue;
                }

                Console.WriteLine(service.Name + @": " + service.ServiceID);
                //AppLog.Line(service.Name + ": " + service.ServiceID);
                MServiceList.Add(service.Name);
            }

            return true;
        }
        catch (Exception err)
        {
            if ((uint)err.HResult != 0x80070422)
                LogError(err);
            return false;
        }
    }

    private void LogError(Exception error)
    {
        uint errCode = (uint)error.HResult;
        AppLog.Line("Error 0x{0}: {1}", errCode.ToString("X").PadLeft(8, '0'), UpdateErrors.GetErrorStr(errCode));
    }

    public void EnableService(string guid, bool enable = true)
    {
        if (enable)
            AddService(guid);
        else
            RemoveService(guid);
        LoadServices();
    }

    private void AddService(string id)
    {
        _mUpdateServiceManager.AddService2(id,
            (int)(tagAddServiceFlag.asfAllowOnlineRegistration | tagAddServiceFlag.asfAllowPendingRegistration |
                  tagAddServiceFlag.asfRegisterServiceWithAU), "");
    }

    private void RemoveService(string id)
    {
        _mUpdateServiceManager.RemoveService(id);
    }

    public bool TestService(string id)
    {
        foreach (IUpdateService service in _mUpdateServiceManager.Services)
            if (service.ServiceID.Equals(id))
                return true;
        return false;
    }

    public string GetServiceName(string id, bool bAdd = false)
    {
        foreach (IUpdateService service in _mUpdateServiceManager.Services)
            if (service.ServiceID.Equals(id))
                return service.Name;
        if (bAdd == false)
            return null;
        AddService(id);
        LoadServices();
        return GetServiceName(id);
    }

    public void UpdateHistory()
    {
        MUpdateHistory.Clear();
        int count = _mUpdateSearcher.GetTotalHistoryCount();
        if (count == 0) // sanity check
            return;
        foreach (IUpdateHistoryEntry2 update in _mUpdateSearcher.QueryHistory(0, count))
        {
            if (update.Title == null) // sanity check
                continue;
            MUpdateHistory.Add(new MsUpdate(update));
        }
    }

    private RetCodes SetupOffline()
    {
        try
        {
            if (_mOfflineService == null)
            {
                AppLog.Line("Setting up 'Offline Sync Service'");

                // http://go.microsoft.com/fwlink/p/?LinkID=74689
                _mOfflineService = _mUpdateServiceManager.AddScanPackageService(_mMyOfflineSvc, DlPath + @"\wsusscn2.cab");
            }

            _mUpdateSearcher.ServerSelection = ServerSelection.ssOthers;
            _mUpdateSearcher.ServiceID = _mOfflineService.ServiceID;
            //mUpdateSearcher.Online = false;
        }
        catch (Exception err)
        {
            AppLog.Line(err.Message);
            RetCodes ret = RetCodes.InternalError;
            if (err.GetType() == typeof(FileNotFoundException))
                ret = RetCodes.FileNotFound;
            if (err.GetType() == typeof(UnauthorizedAccessException))
                ret = RetCodes.AccessError;
            return ret;
        }

        return RetCodes.Success;
    }

    public bool IsValid()
    {
        return _mIsValid;
    }

    private RetCodes ClearOffline()
    {
        if (_mOfflineService != null)
        {
            // note: if we keep references to updates reffering to an removed service we may got a crash
            foreach (MsUpdate update in MUpdateHistory)
                update.Invalidate();
            foreach (MsUpdate update in MPendingUpdates)
                update.Invalidate();
            foreach (MsUpdate update in MInstalledUpdates)
                update.Invalidate();
            foreach (MsUpdate update in MHiddenUpdates)
                update.Invalidate();
            _mIsValid = false;

            OnUpdatesChanged();

            try
            {
                _mUpdateServiceManager.RemoveService(_mOfflineService.ServiceID);
                _mOfflineService = null;
            }
            catch (Exception err)
            {
                AppLog.Line(err.Message);
                return RetCodes.InternalError;
            }
        }

        return RetCodes.Success;
    }

    private void SetOnline(string serviceName)
    {
        foreach (IUpdateService service in _mUpdateServiceManager.Services)
            if (service.Name.Equals(serviceName, StringComparison.CurrentCultureIgnoreCase))
            {
                _mUpdateSearcher.ServerSelection = ServerSelection.ssDefault;
                _mUpdateSearcher.ServiceID = service.ServiceID;
                //mUpdateSearcher.Online = true;
            }
    }

    public AgentOperation CurOperation()
    {
        return _mCurOperation;
    }

    public RetCodes SearchForUpdates(string source = "", bool includePotentiallySupersededUpdates = false)
    {
        if (_mCallback != null)
            return RetCodes.Busy;

        _mUpdateSearcher.IncludePotentiallySupersededUpdates = includePotentiallySupersededUpdates;

        SetOnline(source);

        return SearchForUpdates();
    }

    public RetCodes SearchForUpdates(bool Download, bool includePotentiallySupersededUpdates = false)
    {
        if (_mCallback != null)
            return RetCodes.Busy;

        _mUpdateSearcher.IncludePotentiallySupersededUpdates = includePotentiallySupersededUpdates;

        if (Download)
        {
            _mCurOperation = AgentOperation.PreparingCheck;
            OnProgress(-1, 0, 0, 0);

            AppLog.Line("downloading wsusscn2.cab");

            List<UpdateDownloader.Task> downloads = new();
            UpdateDownloader.Task download = new();
            download.Url =
                Program.IniReadValue("Options", "OfflineCab", "https://go.microsoft.com/fwlink/p/?LinkID=74689");
            download.Path = DlPath;
            download.FileName = "wsusscn2.cab";
            downloads.Add(download);
            if (!_mUpdateDownloader.Download(downloads))
                OnFinished(RetCodes.DownloadFailed);
            return RetCodes.InProgress;
        }

        RetCodes ret = SetupOffline();
        if (ret < 0)
            return ret;

        return SearchForUpdates();
    }

    private RetCodes OnWuError(Exception err)
    {
        bool access = err.GetType() == typeof(UnauthorizedAccessException);
        RetCodes ret = access ? RetCodes.AccessError : RetCodes.InternalError;

        _mCallback = null;
        AppLog.Line(err.Message);
        OnFinished(ret);
        return ret;
    }

    private RetCodes SearchForUpdates()
    {
        _mCurOperation = AgentOperation.CheckingUpdates;
        OnProgress(-1, 0, 0, 0);

        _mCallback = new UpdateCallback(this);

        AppLog.Line("Searching for updates");
        //for the above search criteria refer to 
        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa386526(v=VS.85).aspx
        try
        {
            //string query = "(IsInstalled = 0 and IsHidden = 0) or (IsInstalled = 1 and IsHidden = 0) or (IsHidden = 1)";
            //string query = "(IsInstalled = 0 and IsHidden = 0) or (IsInstalled = 1 and IsHidden = 0) or (IsHidden = 1) or (IsInstalled = 0 and IsHidden = 0 and DeploymentAction='OptionalInstallation') or (IsInstalled = 1 and IsHidden = 0 and DeploymentAction='OptionalInstallation') or (IsHidden = 1 and DeploymentAction='OptionalInstallation')";
            string query;
            if (MiscFunc.IsWindows7OrLower)
                query = "(IsInstalled = 0 and IsHidden = 0) or (IsInstalled = 1 and IsHidden = 0) or (IsHidden = 1)";
            else
                query =
                    "(IsInstalled = 0 and IsHidden = 0 and DeploymentAction=*) or (IsInstalled = 1 and IsHidden = 0 and DeploymentAction=*) or (IsHidden = 1 and DeploymentAction=*)";
            _mSearchJob = _mUpdateSearcher.BeginSearch(query, _mCallback, null);
        }
        catch (Exception err)
        {
            return OnWuError(err);
        }

        return RetCodes.InProgress;
    }

    public IUpdate FindUpdate(string uuid)
    {
        if (_mUpdateSearcher == null)
            return null;
        try
        {
            // Note: this is sloooow!
            ISearchResult result = _mUpdateSearcher.Search("UpdateID = '" + uuid + "'");
            if (result.Updates.Count > 0)
                return result.Updates[0];
        }
        catch (Exception err)
        {
            AppLog.Line(err.Message);
        }

        return null;
    }

    public void CancelOperations()
    {
        if (IsBusy())
            _mCurOperation = AgentOperation.CancelingOperation;

        // Note: at any given time only one (or none) of the 3 conditions can be true
        if (_mCallback != null)
        {
            if (_mSearchJob != null)
                _mSearchJob.RequestAbort();

            if (_mDownloadJob != null)
                _mDownloadJob.RequestAbort();

            if (_mInstalationJob != null)
                _mInstalationJob.RequestAbort();
        }
        else if (_mUpdateDownloader.IsBusy())
        {
            _mUpdateDownloader.CancelOperations();
        }
        else if (_mUpdateInstaller.IsBusy())
        {
            _mUpdateInstaller.CancelOperations();
        }
    }

    public RetCodes DownloadUpdatesManually(List<MsUpdate> updates, bool install = false)
    {
        if (_mUpdateDownloader.IsBusy())
            return RetCodes.Busy;

        _mCurOperation = install ? AgentOperation.PreparingUpdates : AgentOperation.DownloadingUpdates;
        OnProgress(-1, 0, 0, 0);

        List<UpdateDownloader.Task> downloads = new();
        foreach (MsUpdate update in updates)
        {
            if (update.Downloads.Count == 0)
            {
                AppLog.Line("Error: No Download Url's found for update {0}", update.Title);
                continue;
            }

            foreach (string url in update.Downloads)
            {
                UpdateDownloader.Task download = new();
                download.Url = url;
                download.Path = DlPath + @"\" + update.Kb;
                download.Kb = update.Kb;
                downloads.Add(download);
            }
        }

        if (!_mUpdateDownloader.Download(downloads, updates))
            OnFinished(RetCodes.DownloadFailed);

        return RetCodes.InProgress;
    }

    private RetCodes InstallUpdatesManually(List<MsUpdate> updates, MultiValueDictionary<string, string> allFiles)
    {
        if (_mUpdateInstaller.IsBusy())
            return RetCodes.Busy;

        _mCurOperation = AgentOperation.InstallingUpdates;
        OnProgress(-1, 0, 0, 0);

        if (!_mUpdateInstaller.Install(updates, allFiles))
        {
            OnFinished(RetCodes.InstallFailed);
            return RetCodes.InstallFailed;
        }

        return RetCodes.InProgress;
    }


    public RetCodes UnInstallUpdatesManually(List<MsUpdate> updates)
    {
        if (_mUpdateInstaller.IsBusy())
            return RetCodes.Busy;

        List<MsUpdate> filteredUpdates = new();
        foreach (MsUpdate update in updates)
        {
            if ((update.Attributes & (int)MsUpdate.UpdateAttr.Uninstallable) == 0)
            {
                AppLog.Line("Update can not be uninstalled: {0}", update.Title);
                continue;
            }

            filteredUpdates.Add(update);
        }

        if (filteredUpdates.Count == 0)
        {
            AppLog.Line("No updates selected or eligible for uninstallation");
            return RetCodes.NoUpdated;
        }

        _mCurOperation = AgentOperation.RemoveingUpdates;
        OnProgress(-1, 0, 0, 0);

        if (!_mUpdateInstaller.UnInstall(filteredUpdates))
            OnFinished(RetCodes.InstallFailed);

        return RetCodes.InProgress;
    }

    private void DownloadsFinished(object sender, UpdateDownloader.FinishedEventArgs args) // "manuall" mode
    {
        if (_mCurOperation == AgentOperation.CancelingOperation)
        {
            OnFinished(RetCodes.Abborted);
            return;
        }

        if (_mCurOperation == AgentOperation.PreparingCheck)
        {
            AppLog.Line("wsusscn2.cab downloaded");

            RetCodes ret = ClearOffline();
            if (ret == RetCodes.Success)
                ret = SetupOffline();
            if (ret == RetCodes.Success)
                ret = SearchForUpdates();
            if (ret <= 0)
                OnFinished(ret);
        }
        else
        {
            MultiValueDictionary<string, string> allFiles = new();
            foreach (UpdateDownloader.Task task in args.Downloads)
            {
                if (task.Failed && task.FileName != null)
                    continue;
                allFiles.Add(task.Kb, task.Path + @"\" + task.FileName);
            }

            // TODO
            /*string INIPath = dlPath + @"\updates.ini";
            foreach (string KB in AllFiles.Keys)
            {
                string Files = "";
                foreach (string FileName in AllFiles.GetValues(KB))
                {
                    if (Files.Length > 0)
                        Files += "|";
                    Files += FileName;
                }
                Program.IniWriteValue(KB, "Files", Files, INIPath);
            }*/

            AppLog.Line("Downloaded {0} out of {1} to {2}", allFiles.GetCount(), args.Downloads.Count, DlPath);

            if (_mCurOperation == AgentOperation.PreparingUpdates)
            {
                RetCodes ret = InstallUpdatesManually(args.Updates, allFiles);
                if (ret <= 0)
                    OnFinished(ret);
            }
            else
            {
                RetCodes ret = allFiles.GetCount() == args.Downloads.Count ? RetCodes.Success : RetCodes.DownloadFailed;
                if (_mCurOperation == AgentOperation.CancelingOperation)
                    ret = RetCodes.Abborted;
                OnFinished(ret);
            }
        }
    }

    private void DownloadProgress(object sender, ProgressArgs args)
    {
        OnProgress(args.TotalCount, args.TotalPercent, args.CurrentIndex, args.CurrentPercent, args.Info);
    }

    private void InstallFinished(object sender, UpdateInstaller.FinishedEventArgs args) // "manuall" mode
    {
        if (args.Success)
        {
            AppLog.Line("Updates (Un)Installed succesfully");

            foreach (MsUpdate update in args.Updates)
                if (_mCurOperation == AgentOperation.InstallingUpdates)
                {
                    if (RemoveFrom(MPendingUpdates, update))
                    {
                        update.Attributes |= (int)MsUpdate.UpdateAttr.Installed;
                        MInstalledUpdates.Add(update);
                    }
                }
                else if (_mCurOperation == AgentOperation.RemoveingUpdates)
                {
                    if (RemoveFrom(MInstalledUpdates, update))
                    {
                        update.Attributes &= ~(int)MsUpdate.UpdateAttr.Installed;
                        MPendingUpdates.Add(update);
                    }
                }
        }
        else
        {
            AppLog.Line("Updates failed to (Un)Install");
        }

        if (args.Reboot)
            AppLog.Line("Reboot is required for one or more updates");

        OnUpdatesChanged();

        RetCodes ret = args.Success ? RetCodes.Success : RetCodes.InstallFailed;
        if (_mCurOperation == AgentOperation.CancelingOperation)
            ret = RetCodes.Abborted;
        OnFinished(ret, args.Reboot);
    }

    private void InstallProgress(object sender, ProgressArgs args)
    {
        OnProgress(args.TotalCount, args.TotalPercent, args.CurrentIndex, args.CurrentPercent, args.Info);
    }

    public RetCodes DownloadUpdates(List<MsUpdate> updates, bool install = false)
    {
        if (_mCallback != null)
            return RetCodes.Busy;

        if (_mDownloader == null)
            _mDownloader = _mUpdateSession.CreateUpdateDownloader();

        _mDownloader.Updates = new UpdateCollection();
        foreach (MsUpdate Update in updates)
        {
            IUpdate update = Update.GetUpdate();
            if (update == null)
                continue;

            if (update.EulaAccepted == false) update.AcceptEula();
            _mDownloader.Updates.Add(update);
        }

        if (_mDownloader.Updates.Count == 0)
        {
            AppLog.Line("No updates selected for download");
            return RetCodes.NoUpdated;
        }

        _mCurOperation = install ? AgentOperation.PreparingUpdates : AgentOperation.DownloadingUpdates;
        OnProgress(-1, 0, 0, 0);

        _mCallback = new UpdateCallback(this);

        AppLog.Line("Downloading Updates... This may take several minutes.");
        try
        {
            _mDownloadJob = _mDownloader.BeginDownload(_mCallback, _mCallback, updates);
        }
        catch (Exception err)
        {
            return OnWuError(err);
        }

        return RetCodes.InProgress;
    }

    private RetCodes InstallUpdates(List<MsUpdate> Updates)
    {
        if (_mCallback != null)
            return RetCodes.Busy;

        if (_mInstaller == null)
            _mInstaller = _mUpdateSession.CreateUpdateInstaller();

        _mInstaller.Updates = new UpdateCollection();
        foreach (MsUpdate Update in Updates)
        {
            IUpdate update = Update.GetUpdate();
            if (update == null)
                continue;

            _mInstaller.Updates.Add(update);
        }

        if (_mInstaller.Updates.Count == 0)
        {
            AppLog.Line("No updates selected for installation");
            return RetCodes.NoUpdated;
        }

        _mCurOperation = AgentOperation.InstallingUpdates;
        OnProgress(-1, 0, 0, 0);

        _mCallback = new UpdateCallback(this);

        AppLog.Line("Installing Updates... This may take several minutes.");
        try
        {
            _mInstalationJob = _mInstaller.BeginInstall(_mCallback, _mCallback, Updates);
        }
        catch (Exception err)
        {
            return OnWuError(err);
        }

        return RetCodes.InProgress;
    }

    // Note: this works _only_ for updates installed from WSUS
    /*public RetCodes UnInstallUpdates(List<MsUpdate> Updates)
    {
        if (mCallback != null)
            return RetCodes.Busy;

        if (mInstaller == null)
            mInstaller = mUpdateSession.CreateUpdateInstaller() as IUpdateInstaller;

        mInstaller.Updates = new UpdateCollection();
        foreach (MsUpdate Update in Updates)
        {
            IUpdate update = Update.GetUpdate();
            if (update == null)
                continue;

            if (!update.IsUninstallable)
            {
                AppLog.Line("Update can not be uninstalled: {0}", update.Title);
                continue;
            }
            mInstaller.Updates.Add(update);
        }
        if (mInstaller.Updates.Count == 0)
        {
            AppLog.Line("No updates selected or eligible for uninstallation");
            return RetCodes.NoUpdated;
        }

        mCurOperation = AgentOperation.RemoveingUpdates;
        OnProgress(-1, 0, 0, 0);

        mCallback = new UpdateCallback(this);

        AppLog.Line("Removing Updates... This may take several minutes.");
        try
        {
            mInstalationJob = mInstaller.BeginUninstall(mCallback, mCallback, Updates);
        }
        catch (Exception err)
        {
            return OnWuError(err);
        }
        return RetCodes.InProgress;
    }*/

    public bool RemoveFrom(List<MsUpdate> updates, MsUpdate update)
    {
        for (int i = 0; i < updates.Count; i++)
            if (updates[i] == update)
            {
                updates.RemoveAt(i);
                return true;
            }

        return false;
    }

    public void HideUpdates(List<MsUpdate> updates, bool hide)
    {
        foreach (MsUpdate Update in updates)
            try
            {
                IUpdate update = Update.GetUpdate();
                if (update == null)
                    continue;
                update.IsHidden = hide;

                if (hide)
                {
                    Update.Attributes |= (int)MsUpdate.UpdateAttr.Hidden;
                    MHiddenUpdates.Add(Update);
                    RemoveFrom(MPendingUpdates, Update);
                }
                else
                {
                    Update.Attributes &= ~(int)MsUpdate.UpdateAttr.Hidden;
                    MPendingUpdates.Add(Update);
                    RemoveFrom(MHiddenUpdates, Update);
                }

                OnUpdatesChanged();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            } // Hide update may throw an exception, if the user has hidden the update manually while the search was in progress.
    }

    private void OnUpdatesFound(ISearchJob searchJob)
    {
        if (searchJob != _mSearchJob)
            return;
        _mSearchJob = null;
        _mCallback = null;

        ISearchResult searchResults = null;
        try
        {
            searchResults = _mUpdateSearcher.EndSearch(searchJob);
        }
        catch (Exception err)
        {
            AppLog.Line("Search for updates failed");
            LogError(err);
            OnFinished(RetCodes.InternalError);
            return;
        }

        MPendingUpdates.Clear();
        MInstalledUpdates.Clear();
        MHiddenUpdates.Clear();
        _mIsValid = true;

        foreach (IUpdate update in searchResults.Updates)
        {
            if (update.IsHidden)
                MHiddenUpdates.Add(new MsUpdate(update, MsUpdate.UpdateState.Hidden));
            else if (update.IsInstalled)
                MInstalledUpdates.Add(new MsUpdate(update, MsUpdate.UpdateState.Installed));
            else
                MPendingUpdates.Add(new MsUpdate(update, MsUpdate.UpdateState.Pending));
            Console.WriteLine(update.Title);
        }

        AppLog.Line("Found {0} pending updates.", MPendingUpdates.Count);

        OnUpdatesChanged(true);

        RetCodes ret = RetCodes.Undefined;
        if (searchResults.ResultCode == OperationResultCode.orcSucceeded ||
            searchResults.ResultCode == OperationResultCode.orcSucceededWithErrors)
            ret = RetCodes.Success;
        else if (searchResults.ResultCode == OperationResultCode.orcAborted)
            ret = RetCodes.Abborted;
        else if (searchResults.ResultCode == OperationResultCode.orcFailed)
            ret = RetCodes.InternalError;
        OnFinished(ret);
    }

    private void OnUpdatesDownloaded(IDownloadJob downloadJob, List<MsUpdate> updates)
    {
        if (downloadJob != _mDownloadJob)
            return;
        _mDownloadJob = null;
        _mCallback = null;

        IDownloadResult downloadResults = null;
        try
        {
            downloadResults = _mDownloader.EndDownload(downloadJob);
        }
        catch (Exception err)
        {
            AppLog.Line("Downloading updates failed");
            LogError(err);
            OnFinished(RetCodes.InternalError);
            return;
        }

        OnUpdatesChanged();

        if (_mCurOperation == AgentOperation.PreparingUpdates)
        {
            RetCodes ret = InstallUpdates(updates);
            if (ret <= 0)
                OnFinished(ret);
        }
        else
        {
            AppLog.Line("Updates downloaded to %windir%\\SoftwareDistribution\\Download");

            RetCodes ret = RetCodes.Undefined;
            if (downloadResults.ResultCode == OperationResultCode.orcSucceeded ||
                downloadResults.ResultCode == OperationResultCode.orcSucceededWithErrors)
                ret = RetCodes.Success;
            else if (downloadResults.ResultCode == OperationResultCode.orcAborted)
                ret = RetCodes.Abborted;
            else if (downloadResults.ResultCode == OperationResultCode.orcFailed)
                ret = RetCodes.InternalError;
            OnFinished(ret);
        }
    }

    private void OnInstalationCompleted(IInstallationJob installationJob, List<MsUpdate> updates)
    {
        if (installationJob != _mInstalationJob)
            return;
        _mInstalationJob = null;
        _mCallback = null;

        IInstallationResult installationResults = null;
        try
        {
            if (_mCurOperation == AgentOperation.InstallingUpdates)
                installationResults = _mInstaller.EndInstall(installationJob);
            else if (_mCurOperation == AgentOperation.RemoveingUpdates)
                installationResults = _mInstaller.EndUninstall(installationJob);
        }
        catch (Exception err)
        {
            AppLog.Line("(Un)Installing updates failed");
            LogError(err);
            OnFinished(RetCodes.InternalError);
            return;
        }

        if (installationResults!.ResultCode == OperationResultCode.orcSucceeded)
        {
            AppLog.Line("Updates (Un)Installed succesfully");

            foreach (MsUpdate update in updates)
                if (_mCurOperation == AgentOperation.InstallingUpdates)
                {
                    if (RemoveFrom(MPendingUpdates, update))
                    {
                        update.Attributes |= (int)MsUpdate.UpdateAttr.Installed;
                        MInstalledUpdates.Add(update);
                    }
                }
                else if (_mCurOperation == AgentOperation.RemoveingUpdates)
                {
                    if (RemoveFrom(MInstalledUpdates, update))
                    {
                        update.Attributes &= ~(int)MsUpdate.UpdateAttr.Installed;
                        MPendingUpdates.Add(update);
                    }
                }

            if (installationResults.RebootRequired)
                AppLog.Line("Reboot is required for one or more updates");
        }
        else
        {
            AppLog.Line("Updates failed to (Un)Install");
        }

        OnUpdatesChanged();

        RetCodes ret = RetCodes.Undefined;
        if (installationResults.ResultCode == OperationResultCode.orcSucceeded ||
            installationResults.ResultCode == OperationResultCode.orcSucceededWithErrors)
            ret = RetCodes.Success;
        else if (installationResults.ResultCode == OperationResultCode.orcAborted)
            ret = RetCodes.Abborted;
        else if (installationResults.ResultCode == OperationResultCode.orcFailed)
            ret = RetCodes.InternalError;
        OnFinished(ret, installationResults.RebootRequired);
    }


    public void EnableWuAuServ(bool enable = true)
    {
        ServiceController svc = new("wuauserv"); // Windows Update Service
        try
        {
            if (enable)
            {
                if (svc.Status != ServiceControllerStatus.Running)
                {
                    ServiceHelper.ChangeStartMode(svc, ServiceStartMode.Manual);
                    svc.Start();
                }
            }
            else
            {
                if (svc.Status == ServiceControllerStatus.Running)
                    svc.Stop();
                ServiceHelper.ChangeStartMode(svc, ServiceStartMode.Disabled);
            }
        }
        catch (Exception err)
        {
            AppLog.Line("Error: " + err.Message);
        }

        svc.Close();
    }

    public bool TestWuAuServ()
    {
        ServiceController svc = new("wuauserv");
        bool ret = svc.Status == ServiceControllerStatus.Running;
        svc.Close();
        return ret;
    }

    public event EventHandler<ProgressArgs> Progress;

    private void OnProgress(int totalUpdates, int totalPercent, int currentIndex, int updatePercent, string info = "")
    {
        Progress?.Invoke(this, new ProgressArgs(totalUpdates, totalPercent, currentIndex, updatePercent, info));
    }

    public event EventHandler<FinishedArgs> Finished;

    private void OnFinished(RetCodes ret, bool needReboot = false)
    {
        FinishedArgs args = new(_mCurOperation, ret, needReboot);

        _mCurOperation = AgentOperation.None;

        Finished?.Invoke(this, args);
    }

    public event EventHandler<UpdatesArgs> UpdatesChaged;

    protected void OnUpdatesChanged(bool found = false)
    {
        string iniPath = DlPath + @"\updates.ini";
        FileOps.DeleteFile(iniPath);

        StoreUpdates(MUpdateHistory);
        StoreUpdates(MPendingUpdates);
        StoreUpdates(MInstalledUpdates);
        StoreUpdates(MHiddenUpdates);

        UpdatesChaged?.Invoke(this, new UpdatesArgs(found));
    }

    private void StoreUpdates(List<MsUpdate> updates)
    {
        string iniPath = DlPath + @"\updates.ini";
        foreach (MsUpdate update in updates)
        {
            if (update.Kb.Length == 0) // sanity check
                continue;

            Program.IniWriteValue(update.Kb, "UUID", update.Uuid, iniPath);

            Program.IniWriteValue(update.Kb, "Title", update.Title, iniPath);
            Program.IniWriteValue(update.Kb, "Info", update.Description, iniPath);
            Program.IniWriteValue(update.Kb, "Category", update.Category, iniPath);

            Program.IniWriteValue(update.Kb, "Date",
                update.Date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern), iniPath);
            Program.IniWriteValue(update.Kb, "Size", update.Size.ToString(CultureInfo.InvariantCulture), iniPath);

            Program.IniWriteValue(update.Kb, "SupportUrl", update.SupportUrl, iniPath);

            Program.IniWriteValue(update.Kb, "Downloads", string.Join("|", update.Downloads.Cast<string>().ToArray()),
                iniPath);

            Program.IniWriteValue(update.Kb, "State", ((int)update.State).ToString(), iniPath);
            Program.IniWriteValue(update.Kb, "Attributes", update.Attributes.ToString(), iniPath);
            Program.IniWriteValue(update.Kb, "ResultCode", update.ResultCode.ToString(), iniPath);
            Program.IniWriteValue(update.Kb, "HResult", update.HResult.ToString(), iniPath);
        }
    }

    private void LoadUpdates()
    {
        string iniPath = DlPath + @"\updates.ini";
        foreach (string kb in Program.IniEnumSections(iniPath))
        {
            if (kb.Length == 0)
                continue;

            MsUpdate update = new();
            update.Kb = kb;
            update.Uuid = Program.IniReadValue(update.Kb, "UUID", "", iniPath);

            update.Title = Program.IniReadValue(update.Kb, "Title", "", iniPath);
            update.Description = Program.IniReadValue(update.Kb, "Info", "", iniPath);
            update.Category = Program.IniReadValue(update.Kb, "Category", "", iniPath);

            try
            {
                update.Date = DateTime.Parse(Program.IniReadValue(update.Kb, "Date", "", iniPath));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            update.Size = MiscFunc.ParseInt(Program.IniReadValue(update.Kb, "Size", "0", iniPath));

            update.SupportUrl = Program.IniReadValue(update.Kb, "SupportUrl", "", iniPath);
            update.Downloads.AddRange(Program.IniReadValue(update.Kb, "Downloads", "", iniPath).Split('|'));

            update.State =
                (MsUpdate.UpdateState)MiscFunc.ParseInt(Program.IniReadValue(update.Kb, "State", "0", iniPath));
            update.Attributes = MiscFunc.ParseInt(Program.IniReadValue(update.Kb, "Attributes", "0", iniPath));
            update.ResultCode = MiscFunc.ParseInt(Program.IniReadValue(update.Kb, "ResultCode", "0", iniPath));
            update.HResult = MiscFunc.ParseInt(Program.IniReadValue(update.Kb, "HResult", "0", iniPath));

            switch (update.State)
            {
                case MsUpdate.UpdateState.Pending:
                    MPendingUpdates.Add(update);
                    break;
                case MsUpdate.UpdateState.Installed:
                    MInstalledUpdates.Add(update);
                    break;
                case MsUpdate.UpdateState.Hidden:
                    MHiddenUpdates.Add(update);
                    break;
                case MsUpdate.UpdateState.History:
                    MUpdateHistory.Add(update);
                    break;
            }
        }
    }

    public class ProgressArgs : EventArgs
    {
        public int CurrentIndex;
        public int CurrentPercent;
        public string Info = "";

        public int TotalCount;
        public int TotalPercent;

        public ProgressArgs(int totalCount, int totalPercent, int currentIndex, int currentPercent, string info)
        {
            this.TotalCount = totalCount;
            this.TotalPercent = totalPercent;
            this.CurrentIndex = currentIndex;
            this.CurrentPercent = currentPercent;
            this.Info = info;
        }
    }

    public class FinishedArgs : EventArgs
    {
        public AgentOperation Op = AgentOperation.None;
        public bool RebootNeeded;
        public RetCodes Ret = RetCodes.Undefined;

        public FinishedArgs(AgentOperation op, RetCodes ret, bool needReboot = false)
        {
            Op = op;
            Ret = ret;
            RebootNeeded = needReboot;
        }
    }

    public class UpdatesArgs : EventArgs
    {
        public bool Found;

        public UpdatesArgs(bool found)
        {
            Found = found;
        }
    }

    private class UpdateCallback : ISearchCompletedCallback, IDownloadProgressChangedCallback,
        IDownloadCompletedCallback, IInstallationProgressChangedCallback, IInstallationCompletedCallback
    {
        private readonly WuAgent _agent;

        public UpdateCallback(WuAgent agent)
        {
            this._agent = agent;
        }

        // Implementation of IDownloadCompletedCallback interface...
        public void Invoke(IDownloadJob downloadJob, IDownloadCompletedCallbackArgs callbackArgs)
        {
            // !!! warning this function is invoced from a different thread !!!            
            _agent._mDispatcher.Invoke(() => { _agent.OnUpdatesDownloaded(downloadJob, downloadJob.AsyncState); });
        }

        // Implementation of IDownloadProgressChangedCallback interface...
        public void Invoke(IDownloadJob downloadJob, IDownloadProgressChangedCallbackArgs callbackArgs)
        {
            // !!! warning this function is invoced from a different thread !!!            
            _agent._mDispatcher.Invoke(() =>
            {
                _agent.OnProgress(downloadJob.Updates.Count, callbackArgs.Progress.PercentComplete,
                    callbackArgs.Progress.CurrentUpdateIndex + 1,
                    callbackArgs.Progress.CurrentUpdatePercentComplete,
                    downloadJob.Updates[callbackArgs.Progress.CurrentUpdateIndex].Title);
            });
        }

        // Implementation of IInstallationCompletedCallback interface...
        public void Invoke(IInstallationJob installationJob, IInstallationCompletedCallbackArgs callbackArgs)
        {
            // !!! warning this function is invoced from a different thread !!!            
            _agent._mDispatcher.Invoke(() =>
            {
                _agent.OnInstalationCompleted(installationJob, installationJob.AsyncState);
            });
        }

        // Implementation of IInstallationProgressChangedCallback interface...
        public void Invoke(IInstallationJob installationJob, IInstallationProgressChangedCallbackArgs callbackArgs)
        {
            // !!! warning this function is invoced from a different thread !!!            
            _agent._mDispatcher.Invoke(() =>
            {
                _agent.OnProgress(installationJob.Updates.Count, callbackArgs.Progress.PercentComplete,
                    callbackArgs.Progress.CurrentUpdateIndex + 1,
                    callbackArgs.Progress.CurrentUpdatePercentComplete,
                    installationJob.Updates[callbackArgs.Progress.CurrentUpdateIndex].Title);
            });
        }

        // Implementation of ISearchCompletedCallback interface...
        public void Invoke(ISearchJob searchJob, ISearchCompletedCallbackArgs e)
        {
            // !!! warning this function is invoced from a different thread !!!            
            _agent._mDispatcher.Invoke(() => { _agent.OnUpdatesFound(searchJob); });
        }
    }
}

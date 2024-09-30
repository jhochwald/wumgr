#region

using System;
using WUApiLib;
using StringCollection = System.Collections.Specialized.StringCollection;

#endregion

namespace wumgr;

public class MsUpdate
{
    public enum UpdateAttr
    {
        None = 0x0000,
        Beta = 0x0001,
        Downloaded = 0x0002,
        Hidden = 0x0004,
        Installed = 0x0008,
        Mandatory = 0x0010,
        Uninstallable = 0x0020,
        Exclusive = 0x0040,
        Reboot = 0x0080,
        AutoSelect = 0x0100
    }

    public enum UpdateState
    {
        None = 0,
        Pending,
        Installed,
        Hidden,
        History
    }

    public readonly string ApplicationId = "";
    public readonly StringCollection Downloads = new();
    private IUpdate _entry;
    public int Attributes;
    public string Category = "";
    public DateTime Date = DateTime.MinValue;
    public string Description = "";
    public int HResult;
    public string Kb = "";
    public int ResultCode;
    public decimal Size;
    public UpdateState State = UpdateState.None;
    public string SupportUrl = "";
    public string Title = "";
    public string Uuid = "";

    public MsUpdate()
    {
    }

    public MsUpdate(IUpdate update, UpdateState state)
    {
        _entry = update;

        try
        {
            Uuid = update.Identity.UpdateID;

            Title = update.Title;
            Category = GetCategory(update.Categories);
            Description = update.Description;
            Size = update.MaxDownloadSize;
            Date = update.LastDeploymentChangeTime;
            Kb = GetKb(update);
            SupportUrl = update.SupportUrl;

            AddUpdates();

            State = state;

            Attributes |= update.IsBeta ? (int)UpdateAttr.Beta : 0;
            Attributes |= update.IsDownloaded ? (int)UpdateAttr.Downloaded : 0;
            Attributes |= update.IsHidden ? (int)UpdateAttr.Hidden : 0;
            Attributes |= update.IsInstalled ? (int)UpdateAttr.Installed : 0;
            Attributes |= update.IsMandatory ? (int)UpdateAttr.Mandatory : 0;
            Attributes |= update.IsUninstallable ? (int)UpdateAttr.Uninstallable : 0;
            Attributes |= update.AutoSelectOnWebSites ? (int)UpdateAttr.AutoSelect : 0;

            if (update.InstallationBehavior.Impact == InstallationImpact.iiRequiresExclusiveHandling)
                Attributes |= (int)UpdateAttr.Exclusive;

            switch (update.InstallationBehavior.RebootBehavior)
            {
                case InstallationRebootBehavior.irbAlwaysRequiresReboot:
                    Attributes |= (int)UpdateAttr.Reboot;
                    break;
                case InstallationRebootBehavior.irbCanRequestReboot:
                case InstallationRebootBehavior.irbNeverReboots:
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public MsUpdate(IUpdateHistoryEntry2 update)
    {
        try
        {
            Uuid = update.UpdateIdentity.UpdateID;

            Title = update.Title;
            Category = GetCategory(update.Categories);
            Description = update.Description;
            Date = update.Date;
            SupportUrl = update.SupportUrl;
            ApplicationId = update.ClientApplicationID;

            State = UpdateState.History;

            ResultCode = (int)update.ResultCode;
            HResult = update.HResult;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private void AddUpdates()
    {
        AddUpdates(_entry.DownloadContents);
        if (Downloads.Count == 0)
            foreach (IUpdate5 bundle in _entry.BundledUpdates)
                AddUpdates(bundle.DownloadContents);
    }

    private void AddUpdates(IUpdateDownloadContentCollection content)
    {
        foreach (IUpdateDownloadContent2 udc in content)
        {
            if (udc.IsDeltaCompressedContent)
                continue;
            if (string.IsNullOrEmpty(udc.DownloadUrl))
                continue; // sanity check
            Downloads.Add(udc.DownloadUrl);
        }
    }

    private static string GetKb(IUpdate update)
    {
        return update.KBArticleIDs.Count > 0 ? "KB" + update.KBArticleIDs[0] : "KBUnknown";
    }

    private static string GetCategory(ICategoryCollection cats)
    {
        string classification = "";
        string product = "";
        foreach (ICategory cat in cats)
            switch (cat.Type)
            {
                case "UpdateClassification":
                    classification = cat.Name;
                    break;
                case "Product":
                    product = cat.Name;
                    break;
                default:
                    continue;
            }

        return product.Length == 0 ? classification : product + "; " + classification;
    }

    public void Invalidate()
    {
        _entry = null;
    }

    public IUpdate GetUpdate()
    {
        /*if (Entry == null)
        {
            WuAgent agen = WuAgent.GetInstance();
            if (agen.IsActive())
                Entry = agen.FindUpdate(UUID);
        }*/
        return _entry;
    }
}

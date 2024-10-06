#region

using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

#endregion

namespace wumgr.Common;

/// <summary>
///     Provides various file operations and security management methods.
/// </summary>
internal class FileOps
{
    public static string SID_null = "S-1-0-0"; //	Null SID
    public static readonly string SidWorls = "S-1-1-0"; //	World
    public static string SID_Local = "S-1-2-0"; //	Local
    public static string SID_Console = "S-1-2-1"; //	Console Logon
    public static string SID_OwnerID = "S-1-3-0"; //	Creator Owner ID
    public static string SID_GroupeID = "S-1-3-1"; //	Creator Group ID
    public static string SID_OwnerSvr = "S-1-3-2"; //	Creator Owner Server
    public static string SID_CreatorSvr = "S-1-3-3"; //	Creator Group Server
    public static string SID_OwnerRights = "S-1-3-4"; //	Owner Rights
    public static string SID_NonUnique = "S-1-4"; //	Non-unique Authority
    public static string SID_NTAuth = "S-1-5"; //	NT Authority
    public static string SID_AllServices = "S-1-5-80-0"; //	All Services
    public static string SID_DialUp = "S-1-5-1"; //	Dialup
    public static string SID_LocalAcc = "S-1-5-113"; //	Local account
    public static string SID_LocalAccAdmin = "S-1-5-114"; //	Local account and member of Administrators-Group
    public static string SID_Net = "S-1-5-2"; //	Network
    public static string SID_Natch = "S-1-5-3"; //	Batch

    public static string SID_Interactive = "S-1-5-4"; //	Interactive

    //public static string SID_ = "S-1-5-5- *X*- *Y* Logon Session
    public static string SID_Service = "S-1-5-6"; //	Service
    public static string SID_AnonLogin = "S-1-5-7"; //	Anonymous Logon
    public static string SID_Proxy = "S-1-5-8"; //	Proxy
    public static string SID_EDC = "S-1-5-9"; //	Enterprise Domain Controllers
    public static string SID_Self = "S-1-5-10"; //	Self
    public static string SID_AuthenticatedUser = "S-1-5-11"; //	Authenticated Users
    public static string SID_Restricted = "S-1-5-12"; //	Restricted Code
    public static string SID_TermUser = "S-1-5-13"; //	Terminal Server User
    public static string SID_RemoteLogin = "S-1-5-14"; //	Remote Interactive Logon
    public static string SID_ThisORg = "S-1-5-15"; //	This Organization
    public static string SID_IIS = "S-1-5-17"; //	IIS_USRS
    public static readonly string SidSystem = "S-1-5-18"; //	System(or LocalSystem)
    public static string SID_NTAuthL = "S-1-5-19"; //	NT Authority(LocalService)
    public static string SID_NetServices = "S-1-5-20"; //	Network Service
    private static readonly string SidAdmins = "S-1-5-32-544"; //	Administrators
    private static readonly string _sidUsers = "S-1-5-32-545"; //	Users
    public static string SID_Guests = "S-1-5-32-546"; //	Guests
    public static string SID_PowerUsers = "S-1-5-32-547"; //	Power-Users
    public static string SID_AccOps = "S-1-5-32-548"; //	Account Operators
    public static string SID_ServerOps = "S-1-5-32-549"; //	Server Operators
    public static string SID_PrintOps = "S-1-5-32-550"; //	Print Operators
    public static string SID_BackupOps = "S-1-5-32-551"; //	Backup Operators
    public static string SID_Replicators = "S-1-5-32-552"; //	Replicators
    public static string SID_NTLM_Auth = "S-1-5-64-10"; //	NTLM Authentication
    public static string SID_SCh_Auth = "S-1-5-64-14"; //	SChannel Authentication
    public static string SID_DigestAuth = "S-1-5-64-21"; //	Digest Authentication
    public static string SID_NT_Service = "S-1-5-80"; //	NT Service
    public static string SID_All_Services = "S-1-5-80-0"; //	All Services
    public static string SID_VM = "S-1-5-83-0"; //	NT VIRTUAL MACHINE\Virtual Machines
    public static string SID_UntrustedLevel = "S-1-16-0"; //	Untrusted Mandatory Level
    public static string SID_LowLevel = "S-1-16-4096"; //	Low Mandatory Level
    public static string SID_MediumLevel = "S-1-16-8192"; //	Medium Mandatory Level
    public static string SID_MediumPLevel = "S-1-16-8448"; //	Medium Plus Mandatory Level
    public static string SID_HighLevel = "S-1-16-12288"; //	High Mandatory Level
    public static string SID_SysLevel = "S-1-16-16384"; //	System Mandatory Level
    public static string SID_PPLevel = "S-1-16-20480"; //	Protected Process Mandatory Level
    public static string SidSpLevel = "S-1-16-28672"; //	Secure Process Mandatory Level

    /// <summary>
    ///     Formats the given size in bytes to a human-readable string.
    /// </summary>
    /// <param name="size">The size in bytes.</param>
    /// <returns>A formatted string representing the size in B, KB, MB, or GB.</returns>
    public static string FormatSize(decimal size)
    {
        if (size == 0)
            return "";
        if (size >= 1024 * 1024 * 1024)
            return (size / (1024 * 1024 * 1024)).ToString("F") + " GB";
        if (size >= 1024 * 1024)
            return (size / (1024 * 1024)).ToString("F") + " MB";
        if (size >= 1024)
            return (size / 1024).ToString("F") + " KB";
        return (long)size + " B";
    }

    /// <summary>
    ///     Moves a file from one location to another.
    /// </summary>
    /// <param name="from">The source file path.</param>
    /// <param name="to">The destination file path.</param>
    /// <param name="overwrite">Whether to overwrite the destination file if it exists.</param>
    /// <returns>True if the file was moved successfully, otherwise false.</returns>
    public static bool MoveFile(string from, string to, bool overwrite = false)
    {
        try
        {
            if (File.Exists(to))
            {
                if (!overwrite)
                    return false;
                File.Delete(to);
            }

            File.Move(from, to);

            if (File.Exists(from))
                return false;
        }
        catch (Exception e)
        {
            Console.WriteLine(@"The process failed: {0}", e);
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Deletes a file at the specified path.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>True if the file was deleted successfully, otherwise false.</returns>
    public static bool DeleteFile(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Tests the administrative security settings of a file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>
    ///     0 if the file has write or delete permissions for non-admin users, 1 if it does not, 2 if the file does not
    ///     exist.
    /// </returns>
    public static int TestFileAdminSec(string filePath)
    {
        //get file info
        FileInfo fi = new(filePath);
        if (!fi.Exists)
            return 2;

        //get security access
        FileSecurity fs = fi.GetAccessControl();

        //get any special user access
        AuthorizationRuleCollection
            rules = fs.GetAccessRules(true, true, typeof(SecurityIdentifier)); // get as SID not string


        //remove any special access
        foreach (FileSystemAccessRule rule in rules)
        {
            if (rule.AccessControlType != AccessControlType.Allow)
                continue;
            if (rule.IdentityReference.Value.Equals(SidAdmins) || rule.IdentityReference.Value.Equals(SidSystem))
                continue;
            if ((rule.FileSystemRights & (FileSystemRights.Write | FileSystemRights.Delete)) != 0)
                return 0;
        }

        return 1;
    }

    /// <summary>
    ///     Sets the administrative security settings of a file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    public static void SetFileAdminSec(string filePath)
    {
        //get file info
        FileInfo fi = new(filePath);
        if (!fi.Exists)
        {
            FileStream fOut = fi.OpenWrite();
            fOut.Close();
        }

        //get security access
        FileSecurity fs = fi.GetAccessControl();

        //remove any inherited access
        fs.SetAccessRuleProtection(true, false);

        //get any special user access
        AuthorizationRuleCollection rules = fs.GetAccessRules(true, true, typeof(NTAccount)); // show as names

        //remove any special access
        foreach (FileSystemAccessRule rule in rules)
            fs.RemoveAccessRule(rule);

        fs.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(SidAdmins), FileSystemRights.FullControl,
            AccessControlType.Allow));
        fs.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(SidSystem), FileSystemRights.FullControl,
            AccessControlType.Allow));
        fs.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(_sidUsers), FileSystemRights.Read,
            AccessControlType.Allow));

        //add current user with full control.
        //fs.AddAccessRule(new FileSystemAccessRule(domainName + "\\" + userName, FileSystemRights.FullControl, AccessControlType.Allow));

        //add all other users delete only permissions.
        //SecurityIdentifier sid = new SecurityIdentifier("S-1-5-11"); // Authenticated Users
        //fs.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.Delete, AccessControlType.Allow));

        //flush security access.
        File.SetAccessControl(filePath, fs);
    }

    /// <summary>
    ///     Tests if the file can be written to.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>True if the file can be written to, otherwise false.</returns>
    public static bool TestWrite(string filePath)
    {
        FileInfo fi = new(filePath);
        try
        {
            FileStream fOut = fi.OpenWrite();
            fOut.Close();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Takes ownership of a file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>True if ownership was taken successfully, otherwise false.</returns>
    internal static bool TakeOwn(string path)
    {
        bool ret = true;
        try
        {
            //TokenManipulator.AddPrivilege("SeRestorePrivilege");
            //TokenManipulator.AddPrivilege("SeBackupPrivilege");
            TokenManipulator.AddPrivilege("SeTakeOwnershipPrivilege");


            FileSecurity ac = File.GetAccessControl(path);
            ac.SetOwner(new SecurityIdentifier(SidAdmins));
            File.SetAccessControl(path, ac);
        }
        catch (PrivilegeNotHeldException err)
        {
            AppLog.Line("Couldn't take Ownership {0}", err.ToString());
            ret = false;
        }
        finally
        {
            //TokenManipulator.RemovePrivilege("SeRestorePrivilege");
            //TokenManipulator.RemovePrivilege("SeBackupPrivilege");
            TokenManipulator.RemovePrivilege("SeTakeOwnershipPrivilege");
        }

        return ret;
    }
}

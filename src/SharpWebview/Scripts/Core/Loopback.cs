using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

namespace SharpWebview.Scripts.Core;

/// <summary>
/// This class includes all methods necessary to check for the loopback exception on windows.
/// </summary>
public class Loopback
{
    private const string webViewAppContainerName = "microsoft.win32webviewhost_cw5n1h2txyewy";

    /// <summary>
    /// This method checks, if the loopback exception is present for the webview on windows.
    /// </summary>
    /// <returns>True, if the exception is present.</returns>
    public bool IsWebViewLoopbackEnabled()
    {
        var webViewSids = GetWebViewAppContainerSids();

        return GetAllAppContainerConfigs().Any(c =>
        {
            FirewallAPI.ConvertSidToStringSidWrapper(c.Sid, out var currentSid);
            return webViewSids.Any(webViewSid => currentSid == webViewSid);
        });
    }

    private IEnumerable<string> GetWebViewAppContainerSids()
    {
        return GetAllAppContainers()
            .Where(a => string.Equals(Marshal.PtrToStringUni(a.appContainerName), webViewAppContainerName, StringComparison.OrdinalIgnoreCase))
            .Select(a =>
            {
                FirewallAPI.ConvertSidToStringSidWrapper(a.appContainerSid, out var webViewSid);
                return webViewSid;
            });
    }

    private List<FirewallAppContainer> GetAllAppContainers()
    {
        uint size = 0;
        IntPtr arrayValue = IntPtr.Zero;
        int structSize = Marshal.SizeOf<FirewallAppContainer>();

        FirewallAPI.NetworkIsolationEnumAppContainers((uint)NETISO_FLAG.NETISO_FLAG_MAX, ref size, ref arrayValue);

        var firewallApps = new List<FirewallAppContainer>();
        for (int i = 0; i < size; i++)
        {
            var cur = Marshal.PtrToStructure<FirewallAppContainer>(arrayValue);
            firewallApps.Add(cur);
            arrayValue = IntPtr.Add(arrayValue, structSize);
        }

        return firewallApps;
    }

    private List<SidAndAttributes> GetAllAppContainerConfigs()
    {
        uint size = 0;
        IntPtr arrayValue = IntPtr.Zero;
        int structSize = Marshal.SizeOf<SidAndAttributes>();

        FirewallAPI.NetworkIsolationGetAppContainerConfig(ref size, ref arrayValue);

        var firewallAppConfigs = new List<SidAndAttributes>();
        for (int i = 0; i < size; i++)
        {
            var currentConfig = Marshal.PtrToStructure<SidAndAttributes>(arrayValue);
            firewallAppConfigs.Add(currentConfig);
            arrayValue = IntPtr.Add(arrayValue, structSize);
        }

        return firewallAppConfigs;
    }

    internal enum NETISO_FLAG : uint
    {
        NETISO_FLAG_FORCE_COMPUTE_BINARIES = 0x1,
        NETISO_FLAG_MAX = 0x2
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FirewallAppContainer
    {
        internal IntPtr appContainerSid;
        internal IntPtr userSid;
        public IntPtr appContainerName;
        public IntPtr displayName;
        public IntPtr description;
        internal FirewallAcCapabilities capabilities;
        internal FirewallAcBinaries binaries;
        public IntPtr workingDirectory;
        public IntPtr packageFullName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FirewallAcCapabilities
    {
        public uint count;
        public IntPtr capabilities;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FirewallAcBinaries
    {
        public uint count;
        public IntPtr binaries;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SidAndAttributes
    {
        public IntPtr Sid;
        public uint Attributes;
    }
}

internal static partial class FirewallAPI
{
    [LibraryImport("FirewallAPI.dll")]
    internal static partial uint NetworkIsolationEnumAppContainers(uint Flags, ref uint pdwCntPublicACs, ref IntPtr ppACs);

    [LibraryImport("FirewallAPI.dll")]
    internal static partial uint NetworkIsolationGetAppContainerConfig(ref uint pdwCntACs, ref IntPtr appContainerSids);

    [LibraryImport("advapi32", EntryPoint = "ConvertSidToStringSidW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int ConvertSidToStringSid(IntPtr pSid, out string strSid);

    internal static bool ConvertSidToStringSidWrapper(IntPtr pSid, out string strSid)
    {
        int result = ConvertSidToStringSid(pSid, out strSid);
        return result != 0;
    }
}
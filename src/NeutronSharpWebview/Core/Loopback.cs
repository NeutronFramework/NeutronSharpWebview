using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

namespace NeutronSharpWebview.Core;

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
        nint arrayValue = nint.Zero;
        int structSize = Marshal.SizeOf<FirewallAppContainer>();

        FirewallAPI.NetworkIsolationEnumAppContainers((uint)NETISO_FLAG.NETISO_FLAG_MAX, ref size, ref arrayValue);

        var firewallApps = new List<FirewallAppContainer>();
        for (int i = 0; i < size; i++)
        {
            var cur = Marshal.PtrToStructure<FirewallAppContainer>(arrayValue);
            firewallApps.Add(cur);
            arrayValue = nint.Add(arrayValue, structSize);
        }

        return firewallApps;
    }

    private List<SidAndAttributes> GetAllAppContainerConfigs()
    {
        uint size = 0;
        nint arrayValue = nint.Zero;
        int structSize = Marshal.SizeOf<SidAndAttributes>();

        FirewallAPI.NetworkIsolationGetAppContainerConfig(ref size, ref arrayValue);

        var firewallAppConfigs = new List<SidAndAttributes>();
        for (int i = 0; i < size; i++)
        {
            var currentConfig = Marshal.PtrToStructure<SidAndAttributes>(arrayValue);
            firewallAppConfigs.Add(currentConfig);
            arrayValue = nint.Add(arrayValue, structSize);
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
        internal nint appContainerSid;
        internal nint userSid;
        public nint appContainerName;
        public nint displayName;
        public nint description;
        internal FirewallAcCapabilities capabilities;
        internal FirewallAcBinaries binaries;
        public nint workingDirectory;
        public nint packageFullName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FirewallAcCapabilities
    {
        public uint count;
        public nint capabilities;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FirewallAcBinaries
    {
        public uint count;
        public nint binaries;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SidAndAttributes
    {
        public nint Sid;
        public uint Attributes;
    }
}

internal static partial class FirewallAPI
{
    [LibraryImport("FirewallAPI.dll")]
    internal static partial uint NetworkIsolationEnumAppContainers(uint Flags, ref uint pdwCntPublicACs, ref nint ppACs);

    [LibraryImport("FirewallAPI.dll")]
    internal static partial uint NetworkIsolationGetAppContainerConfig(ref uint pdwCntACs, ref nint appContainerSids);

    [LibraryImport("advapi32", EntryPoint = "ConvertSidToStringSidW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int ConvertSidToStringSid(nint pSid, out string strSid);

    internal static bool ConvertSidToStringSidWrapper(nint pSid, out string strSid)
    {
        int result = ConvertSidToStringSid(pSid, out strSid);
        return result != 0;
    }
}

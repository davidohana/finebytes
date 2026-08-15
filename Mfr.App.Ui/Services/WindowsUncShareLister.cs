using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Mfr.App.Ui.Services
{
    /// <summary>
    /// Lists SMB disk shares on a UNC server such as <c>\\ohanas</c>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class WindowsUncShareLister
    {
        private const uint _ErrorSuccess = 0;
        private const uint _ErrorMoreData = 234;
        private const uint _ErrorAccessDenied = 5;
        private const uint _ErrorLogonFailure = 1326;
        private const uint _ShareTypeDisk = 0;
        private const uint _ShareTypeMask = 0xF;
        private const int _ShareInfoLevel1 = 1;
        private const int _MaxPreferredLength = -1;

        /// <summary>
        /// Lists non-hidden disk shares on <paramref name="serverRoot"/>.
        /// </summary>
        /// <param name="serverRoot">UNC server root such as <c>\\ohanas</c>.</param>
        /// <param name="sharePaths">Full UNC share paths when the server responded.</param>
        /// <returns>
        /// <see langword="true"/> when the server was reached (even with no shares or access denied).
        /// </returns>
        public static bool TryListDiskShares(string serverRoot, out List<string> sharePaths)
        {
            sharePaths = [];
            uint resumeHandle = 0;
            uint status;
            do
            {
                status = NativeMethods.NetShareEnum(
                    serverRoot,
                    _ShareInfoLevel1,
                    out var buffer,
                    _MaxPreferredLength,
                    out var entriesRead,
                    out _,
                    ref resumeHandle);

                try
                {
                    var isReachable = status is _ErrorSuccess or _ErrorMoreData
                        or _ErrorAccessDenied or _ErrorLogonFailure;
                    if (!isReachable)
                        return false;

                    if (buffer == IntPtr.Zero || entriesRead <= 0)
                        continue;

                    _AddDiskShares(buffer, entriesRead, serverRoot, sharePaths);
                }
                finally
                {
                    if (buffer != IntPtr.Zero)
                        _ = NativeMethods.NetApiBufferFree(buffer);
                }
            }
            while (status == _ErrorMoreData);

            return true;
        }

        private static void _AddDiskShares(
            IntPtr buffer,
            int entriesRead,
            string serverRoot,
            List<string> sharePaths)
        {
            var stride = Marshal.SizeOf<ShareInfo1>();
            var pathToIsAdded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var existing in sharePaths)
                pathToIsAdded.Add(existing);

            for (var i = 0; i < entriesRead; i++)
            {
                var info = Marshal.PtrToStructure<ShareInfo1>(buffer + (i * stride));
                var name = Marshal.PtrToStringUni(info.NetName);
                if (string.IsNullOrWhiteSpace(name) || name.EndsWith('$'))
                    continue;

                if ((info.Type & _ShareTypeMask) != _ShareTypeDisk)
                    continue;

                var sharePath = serverRoot.TrimEnd('\\') + @"\" + name;
                if (!pathToIsAdded.Add(sharePath))
                    continue;

                sharePaths.Add(sharePath);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ShareInfo1
        {
            public IntPtr NetName;
            public uint Type;
            public IntPtr Remark;
        }

        private static class NativeMethods
        {
            [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)]
            public static extern uint NetShareEnum(
                string serverName,
                int level,
                out IntPtr bufPtr,
                int prefMaxLen,
                out int entriesRead,
                out int totalEntries,
                ref uint resumeHandle);

            [DllImport("Netapi32.dll")]
            public static extern uint NetApiBufferFree(IntPtr buffer);
        }
    }
}

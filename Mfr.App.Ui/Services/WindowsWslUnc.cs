using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Win32;

namespace Mfr.App.Ui.Services
{
    /// <summary>
    /// Resolves Windows Subsystem for Linux UNC paths such as <c>\\wsl$</c> and <c>\\wsl.localhost</c>.
    /// <para>
    /// Those hosts are a Plan 9 redirector, not SMB. The server root is not a real directory
    /// (<c>Directory.Exists(@"\\wsl$")</c> is false); distros are listed from WSL, and each distro
    /// is a share such as <c>\\wsl$\Ubuntu</c>. <c>\\wsl</c> is an alias for the live host.
    /// </para>
    /// </summary>
    internal static class WindowsWslUnc
    {
        private const string _ShortHost = "wsl";
        private const string _LegacyHost = "wsl$";
        private const string _LocalhostHost = "wsl.localhost";
        private const int _Windows11Build = 22000;
        private const int _WslListTimeoutMs = 5000;
        private const string _LxssKey = @"Software\Microsoft\Windows\CurrentVersion\Lxss";

        /// <summary>
        /// Whether <paramref name="path"/> is under a WSL UNC host, including the short <c>\\wsl</c> alias.
        /// </summary>
        /// <param name="path">Typed or resolved path.</param>
        /// <returns><see langword="true"/> for <c>\\wsl</c>, <c>\\wsl$</c>, <c>\\wsl.localhost</c>, and descendants.</returns>
        public static bool IsWslUncPath(string? path)
        {
            return _TrySplit(path, out _, out _);
        }

        /// <summary>
        /// Whether <paramref name="path"/> is a WSL host root with no distro or folder segment.
        /// </summary>
        /// <param name="path">Typed or resolved path.</param>
        /// <returns><see langword="true"/> for <c>\\wsl</c>, <c>\\wsl$</c>, and <c>\\wsl.localhost</c>.</returns>
        public static bool IsWslServerRoot(string? path)
        {
            return _TrySplit(path, out _, out var remainder) && remainder.Length == 0;
        }

        /// <summary>
        /// Rewrites a WSL UNC path onto <paramref name="liveRoot"/> when the host is the short alias.
        /// </summary>
        /// <param name="path">Typed WSL path.</param>
        /// <param name="liveRoot">Reachable <c>\\wsl$</c> or <c>\\wsl.localhost</c>.</param>
        /// <param name="mapped">Rewritten path when <paramref name="path"/> is a WSL UNC path.</param>
        /// <returns><see langword="true"/> when <paramref name="path"/> uses a WSL host.</returns>
        public static bool TryMapPath(
            string? path,
            string liveRoot,
            [NotNullWhen(true)] out string? mapped)
        {
            mapped = null;
            if (string.IsNullOrWhiteSpace(liveRoot) || !_TrySplit(path, out var host, out var remainder))
                return false;

            var targetRoot = _IsShortAlias(host) ? liveRoot.TrimEnd('\\') : @"\\" + host;
            mapped = remainder.Length == 0 ? targetRoot : targetRoot + @"\" + remainder;
            return true;
        }

        /// <summary>
        /// Resolves a typed WSL path to a UNC location.
        /// </summary>
        /// <param name="path">Typed path, possibly using the <c>\\wsl</c> alias.</param>
        /// <param name="resolved">Filesystem path when WSL distros are installed.</param>
        /// <returns><see langword="true"/> when <paramref name="path"/> is WSL and can be opened.</returns>
        public static bool TryResolve(string? path, [NotNullWhen(true)] out string? resolved)
        {
            resolved = null;
            if (!OperatingSystem.IsWindows() || !_TrySplit(path, out var host, out var remainder))
                return false;

            if (!TryGetLiveRoot(out var liveRoot))
                return false;

            var explicitRoot = _IsShortAlias(host) ? liveRoot : @"\\" + host;
            if (remainder.Length == 0)
            {
                resolved = explicitRoot;
                return true;
            }

            foreach (var root in _UniqueRoots(explicitRoot, liveRoot))
            {
                var candidate = root + @"\" + remainder;
                if (!_IsAccessible(candidate))
                    continue;

                resolved = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the WSL UNC root to use when distros are installed, preferring
        /// <c>\\wsl.localhost</c> on Windows 11.
        /// </summary>
        /// <param name="root">Preferred <c>\\wsl.localhost</c> or <c>\\wsl$</c>.</param>
        /// <returns><see langword="true"/> when at least one distro is registered.</returns>
        public static bool TryGetLiveRoot([NotNullWhen(true)] out string? root)
        {
            root = null;
            if (!OperatingSystem.IsWindows() || _ListDistroNames().Count == 0)
                return false;

            root = _PreferredRoot();
            return true;
        }

        /// <summary>
        /// Lists installed distro UNC paths under <paramref name="serverRoot"/>.
        /// </summary>
        /// <param name="serverRoot">WSL host such as <c>\\wsl$</c> or <c>\\wsl.localhost</c>.</param>
        /// <param name="distroPaths">One path per distro, such as <c>\\wsl$\Ubuntu</c>.</param>
        /// <returns><see langword="true"/> when at least one distro is registered.</returns>
        public static bool TryListDistroPaths(string serverRoot, [NotNullWhen(true)] out List<string>? distroPaths)
        {
            distroPaths = null;
            if (!OperatingSystem.IsWindows() || !IsWslServerRoot(serverRoot))
                return false;

            var names = _ListDistroNames();
            if (names.Count == 0)
                return false;

            if (!TryMapPath(serverRoot, _PreferredRoot(), out var mappedRoot))
                return false;

            distroPaths = [];
            foreach (var name in names)
                distroPaths.Add(mappedRoot + @"\" + name);

            return true;
        }

        private static string _PreferredRoot()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, _Windows11Build))
                return @"\\" + _LocalhostHost;

            return @"\\" + _LegacyHost;
        }

        private static List<string> _ListDistroNames()
        {
            var names = _ReadRegistryDistroNames();
            if (names.Count > 0)
                return names;

            return _ReadWslListDistroNames();
        }

        private static List<string> _ReadRegistryDistroNames()
        {
            var names = new List<string>();
            if (!OperatingSystem.IsWindows())
                return names;

            var pathToIsAdded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var lxss = Registry.CurrentUser.OpenSubKey(_LxssKey);
                if (lxss is null)
                    return names;

                foreach (var subName in lxss.GetSubKeyNames())
                {
                    using var subKey = lxss.OpenSubKey(subName);
                    if (subKey?.GetValue("DistributionName") is not string distroName)
                        continue;

                    if (string.IsNullOrWhiteSpace(distroName) || !pathToIsAdded.Add(distroName))
                        continue;

                    names.Add(distroName);
                }
            }
            catch (Exception ex) when (
                ex is ArgumentException
                    or IOException
                    or UnauthorizedAccessException
                    or System.Security.SecurityException)
            {
                return names;
            }

            return names;
        }

        private static List<string> _ReadWslListDistroNames()
        {
            try
            {
                var start = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = "--list --quiet",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.Unicode,
                };
                using var process = Process.Start(start);
                if (process is null)
                    return [];

                if (!process.WaitForExit(_WslListTimeoutMs))
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch (InvalidOperationException)
                    {
                    }

                    return [];
                }

                var output = process.StandardOutput.ReadToEnd();
                return _ParseWslListOutput(output);
            }
            catch (Exception ex) when (
                ex is ArgumentException
                    or InvalidOperationException
                    or IOException
                    or System.ComponentModel.Win32Exception)
            {
                return [];
            }
        }

        private static List<string> _ParseWslListOutput(string output)
        {
            var names = new List<string>();
            var pathToIsAdded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var name = line.Trim().Trim('\0');
                if (string.IsNullOrWhiteSpace(name) || !pathToIsAdded.Add(name))
                    continue;

                names.Add(name);
            }

            return names;
        }

        private static IEnumerable<string> _UniqueRoots(string first, string second)
        {
            yield return first;
            if (!first.Equals(second, StringComparison.OrdinalIgnoreCase))
                yield return second;
        }

        private static bool _TrySplit(
            string? path,
            [NotNullWhen(true)] out string? host,
            [NotNullWhen(true)] out string? remainder)
        {
            host = null;
            remainder = null;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var normalized = _CanonicalUnc(path.Trim());
            if (!normalized.StartsWith(@"\\", StringComparison.Ordinal) || normalized.Length < 3)
                return false;

            var rest = normalized[2..];
            var slash = rest.IndexOf('\\');
            var parsedHost = slash < 0 ? rest : rest[..slash];
            if (!_IsWslHost(parsedHost))
                return false;

            host = parsedHost;
            remainder = slash < 0 ? string.Empty : rest[(slash + 1)..];
            return true;
        }

        private static bool _IsWslHost(string host)
        {
            return host.Equals(_ShortHost, StringComparison.OrdinalIgnoreCase)
                || host.Equals(_LegacyHost, StringComparison.OrdinalIgnoreCase)
                || host.Equals(_LocalhostHost, StringComparison.OrdinalIgnoreCase);
        }

        private static bool _IsShortAlias(string host)
        {
            return host.Equals(_ShortHost, StringComparison.OrdinalIgnoreCase);
        }

        private static bool _IsAccessible(string path)
        {
            try
            {
                return Directory.Exists(path);
            }
            catch (Exception ex) when (
                ex is ArgumentException
                    or NotSupportedException
                    or IOException
                    or UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static string _CanonicalUnc(string path)
        {
            var normalized = path.Replace('/', '\\');
            const string longUnc = @"\\?\UNC\";
            if (normalized.StartsWith(longUnc, StringComparison.OrdinalIgnoreCase))
                normalized = @"\\" + normalized[longUnc.Length..];

            while (normalized.Length > 2 && normalized.EndsWith('\\'))
                normalized = normalized[..^1];

            return normalized;
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using Mfr.App.Ui.Services.FileList;
using Mfr.Utils;

namespace Mfr.App.Ui.ViewModels.FileList
{
    /// <summary>
    /// File List location sentinels and Windows UNC path rules.
    /// </summary>
    internal static class FileListPath
    {
        /// <summary>
        /// Sentinel path for the Windows drive list ("This PC").
        /// </summary>
        public const string ComputerPath = "";

        /// <summary>
        /// Address-bar label shown when listing drives on Windows.
        /// </summary>
        public const string ComputerDisplayName = "This PC";

        /// <summary>
        /// Sentinel path for mapped drives and recent UNC locations.
        /// </summary>
        public const string NetworkPath = "Network";

        /// <summary>
        /// Address-bar label shown for <see cref="NetworkPath"/>.
        /// </summary>
        public const string NetworkDisplayName = "Network";

        /// <summary>
        /// Address-bar label and path for the filesystem root on Unix.
        /// </summary>
        public const string UnixRootPath = "/";

        /// <summary>
        /// Whether <paramref name="path"/> is the Windows drive list.
        /// </summary>
        /// <param name="path">Path or display name to classify.</param>
        /// <returns><see langword="true"/> for empty / This PC.</returns>
        public static bool IsComputerPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return true;
            }

            return path.Equals(ComputerDisplayName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Treats a typed Windows drive spec such as <c>D:</c> as the drive root.
        /// <para>
        /// Windows otherwise resolves <c>D:</c> to the process current directory on that drive.
        /// </para>
        /// </summary>
        /// <param name="path">Typed address-bar text.</param>
        /// <param name="root">Drive root such as <c>D:\</c> when parsing succeeds.</param>
        /// <returns><see langword="true"/> for a letter plus colon with no path.</returns>
        public static bool TryGetDriveRoot(string? path, [NotNullWhen(true)] out string? root)
        {
            root = null;
            if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var trimmed = path.Trim();
            if (trimmed.Length != 2 || trimmed[1] != ':' || !char.IsAsciiLetter(trimmed[0]))
            {
                return false;
            }

            root = char.ToUpperInvariant(trimmed[0]) + @":\";
            return true;
        }

        /// <summary>
        /// Whether <paramref name="path"/> is the Windows Network location.
        /// </summary>
        /// <param name="path">Path or display name to classify.</param>
        /// <returns><see langword="true"/> for Network, <c>\\</c>, or <c>//</c> on Windows.</returns>
        public static bool IsNetworkPath(string? path)
        {
            if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (path.Equals(NetworkDisplayName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var trimmed = path.Trim();
            return trimmed is @"\\" or "//";
        }

        /// <summary>
        /// Whether <paramref name="path"/> is a UNC filesystem path (not the Network sentinel).
        /// </summary>
        /// <param name="path">Path to classify.</param>
        /// <returns><see langword="true"/> for <c>\\server\share</c> and long UNC form.</returns>
        public static bool IsUncPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || IsNetworkPath(path) || IsComputerPath(path))
            {
                return false;
            }

            var normalized = path.Replace('/', '\\');
            if (normalized.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
            {
                return normalized.Length > 8;
            }

            if (normalized.StartsWith(@"\\?\", StringComparison.Ordinal))
            {
                return false;
            }

            return normalized.StartsWith(@"\\", StringComparison.Ordinal) && normalized.Length > 2;
        }

        /// <summary>
        /// Whether <paramref name="path"/> is a UNC server root such as <c>\\ohanas</c>.
        /// </summary>
        /// <param name="path">Path to classify.</param>
        /// <returns><see langword="true"/> when the path is a server with no share segment.</returns>
        public static bool IsUncServerRoot(string path)
        {
            if (!TryGetUncServerRoot(path, out var root))
            {
                return false;
            }

            return PathRelations.IsSamePath(_CanonicalUncPath(path), root);
        }

        /// <summary>
        /// Reads the <c>\\server</c> root from a UNC path.
        /// </summary>
        /// <param name="path">UNC path, possibly with a share and folders.</param>
        /// <param name="root">Server root when parsing succeeds.</param>
        /// <returns><see langword="true"/> when <paramref name="path"/> includes a server name.</returns>
        public static bool TryGetUncServerRoot(string path, [NotNullWhen(true)] out string? root)
        {
            root = null;
            if (!IsUncPath(path))
            {
                return false;
            }

            var normalized = _CanonicalUncPath(path);
            if (!normalized.StartsWith(@"\\", StringComparison.Ordinal) || normalized.Length < 3)
            {
                return false;
            }

            var rest = normalized[2..];
            var serverEnd = rest.IndexOf('\\');
            var server = serverEnd < 0 ? rest : rest[..serverEnd];
            if (server.Length == 0)
            {
                return false;
            }

            root = @"\\" + server;
            return true;
        }

        /// <summary>
        /// Whether <paramref name="path"/> is a UNC share root such as <c>\\server\share</c>.
        /// </summary>
        /// <param name="path">Path to classify.</param>
        /// <returns><see langword="true"/> when the path has no segments below the share.</returns>
        public static bool IsUncShareRoot(string path)
        {
            if (!TryGetUncShareRoot(path, out var root))
            {
                return false;
            }

            return PathRelations.IsSamePath(_CanonicalUncPath(path), root);
        }

        /// <summary>
        /// Reads the <c>\\server\share</c> root from a UNC path.
        /// </summary>
        /// <param name="path">UNC path, possibly with extra folders or a long-path prefix.</param>
        /// <param name="root">Share root when parsing succeeds.</param>
        /// <returns><see langword="true"/> when <paramref name="path"/> includes a server and share.</returns>
        public static bool TryGetUncShareRoot(string path, [NotNullWhen(true)] out string? root)
        {
            root = null;
            if (!IsUncPath(path))
            {
                return false;
            }

            var normalized = _CanonicalUncPath(path);
            if (!normalized.StartsWith(@"\\", StringComparison.Ordinal) || normalized.Length < 5)
            {
                return false;
            }

            var rest = normalized[2..];
            var serverEnd = rest.IndexOf('\\');
            if (serverEnd <= 0)
            {
                return false;
            }

            var afterServer = rest[(serverEnd + 1)..];
            if (afterServer.Length == 0)
            {
                return false;
            }

            var shareEnd = afterServer.IndexOf('\\');
            var share = shareEnd < 0 ? afterServer : afterServer[..shareEnd];
            if (share.Length == 0)
            {
                return false;
            }

            var server = rest[..serverEnd];
            root = @"\\" + server + @"\" + share;
            return true;
        }

        /// <summary>
        /// Parent folder, Network, or This PC for File List Go Up.
        /// </summary>
        /// <param name="path">Current File List path or sentinel.</param>
        /// <returns>Parent path, or <see langword="null"/> at This PC / Unix root.</returns>
        public static string? GetParentPath(string path)
        {
            if (IsComputerPath(path))
            {
                return null;
            }

            if (IsNetworkPath(path))
            {
                return OperatingSystem.IsWindows() ? ComputerPath : null;
            }

            if (OperatingSystem.IsWindows() && IsUncServerRoot(path))
            {
                return NetworkPath;
            }

            if (OperatingSystem.IsWindows() && IsUncShareRoot(path) && TryGetUncServerRoot(path, out var serverRoot))
            {
                return serverRoot;
            }

            if (WindowsKnownPlaces.TryGetPlace(path, out _))
            {
                return ComputerPath;
            }

            var parent = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parent))
            {
                return OperatingSystem.IsWindows() ? ComputerPath : null;
            }

            return parent;
        }

        /// <summary>
        /// Address-bar text for a resolved File List path.
        /// </summary>
        /// <param name="path">Filesystem path or sentinel.</param>
        /// <returns>Display name for This PC / Network, otherwise <paramref name="path"/>.</returns>
        public static string ToDisplayPath(string path)
        {
            if (IsComputerPath(path))
            {
                return ComputerDisplayName;
            }

            if (IsNetworkPath(path))
            {
                return NetworkDisplayName;
            }

            return path;
        }

        /// <summary>
        /// Builds the clickable address-bar trail for <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Filesystem path or sentinel.</param>
        /// <returns>Root-to-current segments.</returns>
        public static List<PathBreadcrumbSegment> BuildBreadcrumbSegments(string path)
        {
            if (OperatingSystem.IsWindows())
            {
                return _BuildWindowsBreadcrumbSegments(path);
            }

            return _BuildUnixBreadcrumbSegments(path);
        }

        private static List<PathBreadcrumbSegment> _BuildWindowsBreadcrumbSegments(string path)
        {
            var segments = new List<PathBreadcrumbSegment>
            {
                _CreateSegment(ComputerDisplayName, ComputerDisplayName, showLeadingChevron: false),
            };

            if (IsComputerPath(path))
            {
                return segments;
            }

            if (IsNetworkPath(path))
            {
                segments.Add(_CreateSegment(NetworkDisplayName, NetworkDisplayName, showLeadingChevron: true));
                return segments;
            }

            if (IsUncPath(path))
            {
                segments.Add(_CreateSegment(NetworkDisplayName, NetworkDisplayName, showLeadingChevron: true));
                _AddUncBreadcrumbSegments(segments, path);
                return segments;
            }

            if (WindowsKnownPlaces.TryGetContainingPlace(path, out var place))
            {
                segments.Add(_CreateSegment(place.Name, place.Path, showLeadingChevron: true));
                if (PathRelations.IsSamePath(path, place.Path))
                {
                    return segments;
                }

                _AddChildBreadcrumbSegments(segments, path, place.Path);
                return segments;
            }

            var root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root))
            {
                return segments;
            }

            segments.Add(_CreateSegment(_FormatDriveLabel(root), root, showLeadingChevron: true));
            _AddChildBreadcrumbSegments(segments, path, root);
            return segments;
        }

        private static List<PathBreadcrumbSegment> _BuildUnixBreadcrumbSegments(string path)
        {
            var segments = new List<PathBreadcrumbSegment>
            {
                _CreateSegment(UnixRootPath, UnixRootPath, showLeadingChevron: false),
            };

            if (IsComputerPath(path) || PathRelations.IsSamePath(path, UnixRootPath))
            {
                return segments;
            }

            _AddChildBreadcrumbSegments(segments, path, UnixRootPath);
            return segments;
        }

        private static void _AddUncBreadcrumbSegments(List<PathBreadcrumbSegment> segments, string path)
        {
            if (!TryGetUncServerRoot(path, out var serverRoot))
            {
                return;
            }

            var serverName = serverRoot[2..];
            segments.Add(_CreateSegment(serverName, serverRoot, showLeadingChevron: true));
            if (IsUncServerRoot(path) || !TryGetUncShareRoot(path, out var shareRoot))
            {
                return;
            }

            var shareName = shareRoot[(serverRoot.Length + 1)..];
            segments.Add(_CreateSegment(shareName, shareRoot, showLeadingChevron: true));
            if (IsUncShareRoot(path))
            {
                return;
            }

            _AddChildBreadcrumbSegments(segments, path, shareRoot);
        }

        private static void _AddChildBreadcrumbSegments(
            List<PathBreadcrumbSegment> segments,
            string path,
            string rootPath
        )
        {
            DirectoryInfo? current;
            string rootFullName;
            try
            {
                current = new DirectoryInfo(path);
                rootFullName = new DirectoryInfo(rootPath).FullName;
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException)
            {
                return;
            }

            var parts = new List<PathBreadcrumbSegment>();
            while (current is not null && !PathRelations.IsSamePath(current.FullName, rootFullName))
            {
                var name = current.Name;
                if (string.IsNullOrEmpty(name))
                {
                    break;
                }

                parts.Add(_CreateSegment(name, current.FullName, showLeadingChevron: true));
                current = current.Parent;
            }

            parts.Reverse();
            segments.AddRange(parts);
        }

        private static PathBreadcrumbSegment _CreateSegment(string label, string targetPath, bool showLeadingChevron)
        {
            return new PathBreadcrumbSegment
            {
                Label = label,
                TargetPath = targetPath,
                ShowLeadingChevron = showLeadingChevron,
            };
        }

        private static string _FormatDriveLabel(string root)
        {
            try
            {
                var drive = new DriveInfo(root);
                var letter = drive.Name.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (drive.IsReady)
                {
                    var volume = drive.VolumeLabel;
                    if (!string.IsNullOrWhiteSpace(volume))
                    {
                        return volume + " (" + letter + ")";
                    }
                }

                return letter;
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
            {
                return root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
        }

        private static string _CanonicalUncPath(string path)
        {
            return _TrimUncEndingSeparators(_NormalizeUncPrefix(path));
        }

        private static string _TrimUncEndingSeparators(string path)
        {
            var trimmed = path;
            while (trimmed.Length > 2 && trimmed.EndsWith('\\'))
            {
                trimmed = trimmed[..^1];
            }

            return trimmed;
        }

        private static string _NormalizeUncPrefix(string path)
        {
            var normalized = path.Replace('/', '\\');
            const string longUnc = @"\\?\UNC\";
            if (normalized.StartsWith(longUnc, StringComparison.OrdinalIgnoreCase))
            {
                return @"\\" + normalized[longUnc.Length..];
            }

            return normalized;
        }
    }
}

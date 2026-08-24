using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Mfr.Utils;
using Serilog;

namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// Enumerates File List locations and resolves typed paths, including network timeouts.
    /// </summary>
    internal static class FileListCatalog
    {
        private static readonly EnumerationOptions _ListingOptions = new()
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = false,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
        };

        // Caps how long a disconnected UNC or mapped drive may block Exists/enumerate.
        // The OS SMB timeout cannot be cancelled; this bound keeps the File List responsive.
        private static readonly TimeSpan _NetworkProbeTimeout = TimeSpan.FromSeconds(3);

        // First contact with a UNC server (\\ohanas) is often slower than a share Exists check.
        private static readonly TimeSpan _UncServerProbeTimeout = TimeSpan.FromSeconds(8);

        private const int _VolumeListingGroup = 0;
        private const int _KnownPlaceListingGroup = 1;

        /// <summary>
        /// Lists the contents of a File List location.
        /// </summary>
        /// <param name="currentPath">Folder, This PC, Network, or UNC server root.</param>
        /// <param name="includeMask">Include mask applied to file names. Folders are always listed.</param>
        /// <param name="excludeMasksEnabled">Whether <paramref name="excludeMasks"/> are applied.</param>
        /// <param name="excludeMasks">Exclude patterns used when <paramref name="excludeMasksEnabled"/> is true.</param>
        /// <param name="pathHistory">Recent paths used to seed Network UNC locations.</param>
        /// <returns>Listed rows and a failure kind when a folder could not be read.</returns>
        public static FileListCatalogResult List(
            string currentPath,
            string includeMask,
            bool excludeMasksEnabled,
            IReadOnlyList<string> excludeMasks,
            IEnumerable<string> pathHistory
        )
        {
            if (FileListPath.IsComputerPath(currentPath))
            {
                var computerItems = new List<FileListListedItem>();
                computerItems.AddRange(_ListKnownPlaces());
                computerItems.AddRange(_ListDrives());
                if (OperatingSystem.IsWindows())
                {
                    computerItems.Add(_CreateNetworkRootItem());
                }

                return FileListCatalogResult.Ok(computerItems);
            }

            if (FileListPath.IsNetworkPath(currentPath))
            {
                return FileListCatalogResult.Ok(_ListNetworkLocations(pathHistory));
            }

            if (OperatingSystem.IsWindows() && WindowsWslUnc.IsWslServerRoot(currentPath))
            {
                return FileListCatalogResult.Ok(_ListWslDistros(currentPath));
            }

            if (FileListPath.IsUncServerRoot(currentPath))
            {
                List<FileListListedItem> shares = [];
                if (OperatingSystem.IsWindows())
                {
                    shares = _ListUncShares(currentPath);
                }

                return FileListCatalogResult.Ok(shares);
            }

            var maskFilter = new MaskFilter(includeMask, excludeMasksEnabled, excludeMasks);
            if (!_TryListFolder(currentPath, maskFilter, out var folders, out var files, out var failure))
            {
                return FileListCatalogResult.Failed(failure);
            }

            var folderItems = new List<FileListListedItem>(folders.Count + files.Count);
            folderItems.AddRange(folders);
            folderItems.AddRange(files);
            return FileListCatalogResult.Ok(folderItems);
        }

        /// <summary>
        /// Builds the empty-state message for a folder listing failure.
        /// </summary>
        /// <param name="failure">Why listing failed.</param>
        /// <returns>User-facing text, or empty when listing succeeded.</returns>
        public static string FormatListingError(FileListListingFailure failure)
        {
            return failure switch
            {
                FileListListingFailure.AccessDenied => "Access denied reading this folder.",
                FileListListingFailure.NotFound => "This folder could not be found.",
                FileListListingFailure.TimedOut => "Timed out reading this folder.",
                FileListListingFailure.Unavailable => "Could not read this folder.",
                FileListListingFailure.None => string.Empty,
                _ => "Could not read this folder.",
            };
        }

        /// <summary>
        /// Resolves a start folder for the File List: the given path, else the user profile, else cwd.
        /// </summary>
        /// <param name="initialPath">Directory to open, or <see langword="null"/> for the user profile.</param>
        /// <returns>A resolvable filesystem path.</returns>
        public static string ResolveStartPath(string? initialPath)
        {
            if (TryResolvePath(initialPath, out var resolved) && !FileListPath.IsComputerPath(resolved))
            {
                return resolved;
            }

            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (TryResolvePath(profile, out resolved) && !FileListPath.IsComputerPath(resolved))
            {
                return resolved;
            }

            return Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Resolves typed address-bar text to a File List location.
        /// </summary>
        /// <param name="path">Typed path, alias, or sentinel.</param>
        /// <param name="resolved">Normalized path when this method returns <see langword="true"/>.</param>
        /// <returns><see langword="true"/> when the location can be listed.</returns>
        public static bool TryResolvePath(string? path, [NotNullWhen(true)] out string resolved)
        {
            if (FileListPath.IsComputerPath(path))
            {
                if (!OperatingSystem.IsWindows())
                {
                    resolved = FileListPath.ComputerPath;
                    return false;
                }

                resolved = FileListPath.ComputerPath;
                return true;
            }

            if (FileListPath.IsNetworkPath(path))
            {
                resolved = FileListPath.NetworkPath;
                return true;
            }

            if (WindowsKnownPlaces.TryResolveAlias(path, out var aliasPath))
            {
                resolved = aliasPath;
                return true;
            }

            if (OperatingSystem.IsWindows() && WindowsWslUnc.IsWslUncPath(path))
            {
                if (WindowsWslUnc.TryResolve(path, out var wslPath))
                {
                    resolved = wslPath;
                    return true;
                }

                resolved = FileListPath.ComputerPath;
                return false;
            }

            if (OperatingSystem.IsWindows())
            {
                var isUncServer = path is not null && FileListPath.IsUncServerRoot(path);
                if (isUncServer && FileListPath.TryGetUncServerRoot(path!, out var serverRoot))
                {
                    resolved = serverRoot;
                    return _UncServerIsReachable(serverRoot);
                }
            }

            try
            {
                var expanded = Environment.ExpandEnvironmentVariables(path!);
                if (FileListPath.TryGetDriveRoot(expanded, out var driveRoot))
                {
                    expanded = driveRoot;
                }

                resolved = new DirectoryInfo(expanded).FullName;
                return _DirectoryExists(resolved);
            }
            catch (Exception ex)
                when (ex is ArgumentException or NotSupportedException or IOException or UnauthorizedAccessException)
            {
                resolved = FileListPath.ComputerPath;
                return false;
            }
        }

        private static List<FileListListedItem> _ListDrives()
        {
            DriveInfo[] drives;
            try
            {
                drives = DriveInfo.GetDrives();
            }
            catch (IOException)
            {
                return [];
            }

            var items = new List<FileListListedItem>();
            foreach (var drive in drives)
            {
                string name;
                try
                {
                    name = drive.Name;
                }
                catch (IOException)
                {
                    continue;
                }

                items.Add(_CreateListedItem(name, isDirectory: true, listingGroup: _VolumeListingGroup));
            }

            return items;
        }

        private static List<FileListListedItem> _ListKnownPlaces()
        {
            var items = new List<FileListListedItem>();
            foreach (var place in WindowsKnownPlaces.GetPlaces())
            {
                items.Add(
                    new FileListListedItem(
                        place.Path,
                        place.Name,
                        IsDirectory: true,
                        Length: null,
                        LastWriteTime: _TryGetLastWriteTime(place.Path),
                        ListingGroup: _KnownPlaceListingGroup
                    )
                );
            }

            return items;
        }

        private static List<FileListListedItem> _ListNetworkLocations(IEnumerable<string> pathHistory)
        {
            var items = new List<FileListListedItem>();
            var pathToIsAdded = new HashSet<string>(PathComparers.Os);

            foreach (var drive in _ListNetworkDrives())
            {
                if (!pathToIsAdded.Add(drive.Path))
                {
                    continue;
                }

                items.Add(drive);
            }

            if (
                OperatingSystem.IsWindows()
                && WindowsWslUnc.TryGetLiveRoot(out var wslRoot)
                && pathToIsAdded.Add(wslRoot)
            )
            {
                items.Add(
                    new FileListListedItem(wslRoot, wslRoot[2..], IsDirectory: true, Length: null, LastWriteTime: null)
                );
            }

            foreach (var historyPath in pathHistory)
            {
                if (!FileListPath.IsUncPath(historyPath))
                {
                    continue;
                }

                var location = historyPath.TrimTrailingSeparator();
                if (!pathToIsAdded.Add(location))
                {
                    continue;
                }

                items.Add(
                    new FileListListedItem(location, location, IsDirectory: true, Length: null, LastWriteTime: null)
                );
            }

            return items;
        }

        private static List<FileListListedItem> _ListWslDistros(string serverRoot)
        {
            if (!WindowsWslUnc.TryListDistroPaths(serverRoot, out var distroPaths))
            {
                return [];
            }

            var items = new List<FileListListedItem>();
            foreach (var distroPath in distroPaths)
            {
                var name = FileListPath.LastUncSegment(distroPath);
                items.Add(
                    new FileListListedItem(distroPath, name, IsDirectory: true, Length: null, LastWriteTime: null)
                );
            }

            return items;
        }

        [SupportedOSPlatform("windows")]
        private static List<FileListListedItem> _ListUncShares(string serverRoot)
        {
            if (
                !_TryRunWithTimeout(() => _TryReadUncShares(serverRoot), _UncServerProbeTimeout, out var sharePaths)
                || sharePaths is null
            )
            {
                return [];
            }

            var items = new List<FileListListedItem>();
            foreach (var sharePath in sharePaths)
            {
                var name = Path.GetFileName(sharePath.TrimTrailingSeparator());
                items.Add(
                    new FileListListedItem(
                        sharePath,
                        string.IsNullOrEmpty(name) ? sharePath : name,
                        IsDirectory: true,
                        Length: null,
                        LastWriteTime: null
                    )
                );
            }

            return items;
        }

        [SupportedOSPlatform("windows")]
        private static List<string>? _TryReadUncShares(string serverRoot)
        {
            if (!WindowsUncShareLister.TryListDiskShares(serverRoot, out var sharePaths))
            {
                return null;
            }

            return sharePaths;
        }

        [SupportedOSPlatform("windows")]
        private static bool _UncServerIsReachable(string serverRoot)
        {
            return _TryRunWithTimeout(
                    () => _TryReadUncShares(serverRoot) is not null,
                    _UncServerProbeTimeout,
                    out var reachable
                ) && reachable;
        }

        private static List<FileListListedItem> _ListNetworkDrives()
        {
            DriveInfo[] drives;
            try
            {
                drives = DriveInfo.GetDrives();
            }
            catch (IOException)
            {
                return [];
            }

            var items = new List<FileListListedItem>();
            foreach (var drive in drives)
            {
                DriveType driveType;
                string name;
                try
                {
                    driveType = drive.DriveType;
                    name = drive.Name;
                }
                catch (IOException)
                {
                    continue;
                }

                if (driveType != DriveType.Network)
                {
                    continue;
                }

                items.Add(_CreateListedItem(name, isDirectory: true));
            }

            return items;
        }

        private static FileListListedItem _CreateNetworkRootItem()
        {
            return new FileListListedItem(
                FileListPath.NetworkPath,
                FileListPath.NetworkDisplayName,
                IsDirectory: true,
                Length: null,
                LastWriteTime: null,
                ListingGroup: _KnownPlaceListingGroup
            );
        }

        private static bool _TryListFolder(
            string path,
            MaskFilter maskFilter,
            out List<FileListListedItem> folders,
            out List<FileListListedItem> files,
            out FileListListingFailure failure
        )
        {
            folders = [];
            files = [];
            failure = FileListListingFailure.None;
            try
            {
                if (!_NeedsNetworkTimeout(path))
                {
                    (folders, files) = _ReadFolderListing(path, maskFilter);
                    return true;
                }

                var timeout = WindowsWslUnc.IsWslUncPath(path) ? _UncServerProbeTimeout : _NetworkProbeTimeout;
                var listTask = Task.Run(() => _ReadFolderListing(path, maskFilter));
                try
                {
                    if (!listTask.Wait(timeout))
                    {
                        // Exists/enumerate cannot be cancelled; observe later faults so they are not unhandled.
                        _ = listTask.ContinueWith(
                            static completed => completed.Exception,
                            CancellationToken.None,
                            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default
                        );
                        failure = FileListListingFailure.TimedOut;
                        Log.Warning("Timed out listing folder {Path} after {Timeout}.", path, timeout);
                        return false;
                    }

                    (folders, files) = listTask.Result;
                    return true;
                }
                catch (AggregateException ex)
                {
                    failure = _MapListingException(ex.GetBaseException());
                    Log.Warning(ex, "Failed to list folder {Path}.", path);
                    return false;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                failure = _MapListingException(ex);
                Log.Warning(ex, "Failed to list folder {Path}.", path);
                return false;
            }
        }

        private static FileListListingFailure _MapListingException(Exception ex)
        {
            if (ex is UnauthorizedAccessException)
            {
                return FileListListingFailure.AccessDenied;
            }

            if (ex is DirectoryNotFoundException)
            {
                return FileListListingFailure.NotFound;
            }

            return FileListListingFailure.Unavailable;
        }

        private static (List<FileListListedItem> Folders, List<FileListListedItem> Files) _ReadFolderListing(
            string path,
            MaskFilter maskFilter
        )
        {
            // IgnoreInaccessible would treat an unreadable directory as empty; probe first so browse can show an error.
            _EnsureDirectoryReadable(path);

            var folders = Directory
                .EnumerateDirectories(path, "*", _ListingOptions)
                .Select(folderPath => _CreateListedItem(folderPath, isDirectory: true))
                .ToList();

            var files = Directory
                .EnumerateFiles(path, "*", _ListingOptions)
                .Where(filePath => _PassesFileMasks(filePath, maskFilter))
                .Select(filePath => _CreateListedItem(filePath, isDirectory: false))
                .ToList();

            return (folders, files);
        }

        /// <summary>
        /// Throws when <paramref name="path"/> cannot be listed (e.g. access denied).
        /// </summary>
        /// <para>
        /// Uses <see cref="EnumerationOptions.IgnoreInaccessible"/> = false so denial on the directory itself
        /// surfaces as an exception instead of an empty listing.
        /// </para>
        private static void _EnsureDirectoryReadable(string path)
        {
            var probeOptions = new EnumerationOptions
            {
                IgnoreInaccessible = false,
                RecurseSubdirectories = false,
                ReturnSpecialDirectories = false,
            };
            _ = Directory.EnumerateFileSystemEntries(path, "*", probeOptions).Any();
        }

        private static bool _PassesFileMasks(string path, MaskFilter maskFilter)
        {
            var fileName = Path.GetFileName(path);
            if (!WildcardMask.IsMatch(fileName, maskFilter.IncludeMask))
            {
                return false;
            }

            if (!maskFilter.ExcludeEnabled)
            {
                return true;
            }

            return !WildcardMask.MatchesAny(fileName, maskFilter.ExcludeMasks);
        }

        private static bool _DirectoryExists(string path)
        {
            if (!_NeedsNetworkTimeout(path))
            {
                return Directory.Exists(path);
            }

            return _TryRunWithTimeout(() => Directory.Exists(path), _NetworkProbeTimeout, out var exists) && exists;
        }

        private static bool _NeedsNetworkTimeout(string path)
        {
            if (FileListPath.IsUncPath(path))
            {
                return true;
            }

            try
            {
                var root = Path.GetPathRoot(path);
                if (string.IsNullOrEmpty(root))
                {
                    return false;
                }

                return new DriveInfo(root).DriveType == DriveType.Network;
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static bool _TryRunWithTimeout<T>(Func<T> action, TimeSpan timeout, out T result)
        {
            var task = Task.Run(action);
            try
            {
                if (task.Wait(timeout))
                {
                    result = task.Result;
                    return true;
                }
            }
            catch (AggregateException)
            {
                result = default!;
                return false;
            }

            // Exists/enumerate cannot be cancelled; observe later faults so they are not unhandled.
            _ = task.ContinueWith(
                static completed => completed.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
            result = default!;
            return false;
        }

        private static FileListListedItem _CreateListedItem(string path, bool isDirectory, int listingGroup = 0)
        {
            var name = isDirectory ? FileListPath.DirectoryDisplayName(path) : Path.GetFileName(path);
            if (isDirectory)
            {
                return new FileListListedItem(
                    path,
                    name,
                    IsDirectory: true,
                    Length: null,
                    LastWriteTime: _TryGetLastWriteTime(path),
                    ListingGroup: listingGroup
                );
            }

            var (length, lastWriteTime) = _TryGetFileInfo(path);
            return new FileListListedItem(path, name, IsDirectory: false, Length: length, LastWriteTime: lastWriteTime);
        }

        private static (long? Length, DateTime? LastWriteTime) _TryGetFileInfo(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return (info.Length, info.LastWriteTime);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return (null, null);
            }
        }

        private static DateTime? _TryGetLastWriteTime(string path)
        {
            try
            {
                return Directory.GetLastWriteTime(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return null;
            }
        }

        private readonly record struct MaskFilter(
            string IncludeMask,
            bool ExcludeEnabled,
            IReadOnlyList<string> ExcludeMasks
        );
    }
}

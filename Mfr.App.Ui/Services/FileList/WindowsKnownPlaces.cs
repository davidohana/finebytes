using System.Diagnostics.CodeAnalysis;
using Mfr.Utils;

namespace Mfr.App.Ui.Services.FileList
{
    /// <summary>
    /// One Windows user folder shown on This PC, such as Documents or Music.
    /// </summary>
    /// <param name="Name">Explorer-style label used in the listing and address bar.</param>
    /// <param name="Path">Resolved filesystem directory.</param>
    internal sealed record WindowsKnownPlace(string Name, string Path);

    /// <summary>
    /// Maps a small set of Explorer-like names to real user folders.
    /// <para>
    /// No shell namespace: paths come from <see cref="Environment.SpecialFolder"/> plus a Downloads
    /// folder under the profile when that directory exists.
    /// </para>
    /// </summary>
    internal static class WindowsKnownPlaces
    {
        /// <summary>
        /// User folders to show on This PC, in Explorer-like order.
        /// </summary>
        /// <returns>Existing unique folders; empty on non-Windows.</returns>
        public static IReadOnlyList<WindowsKnownPlace> GetPlaces()
        {
            if (!OperatingSystem.IsWindows())
            {
                return [];
            }

            var places = new List<WindowsKnownPlace>();
            var pathToIsAdded = new HashSet<string>(PathComparers.Os);
            _TryAddPlace(places, pathToIsAdded, "Desktop", Environment.SpecialFolder.DesktopDirectory);
            _TryAddPlace(places, pathToIsAdded, "Documents", Environment.SpecialFolder.MyDocuments);
            _TryAddDownloads(places, pathToIsAdded);
            _TryAddPlace(places, pathToIsAdded, "Music", Environment.SpecialFolder.MyMusic);
            _TryAddPlace(places, pathToIsAdded, "Pictures", Environment.SpecialFolder.MyPictures);
            _TryAddPlace(places, pathToIsAdded, "Videos", Environment.SpecialFolder.MyVideos);
            return places;
        }

        /// <summary>
        /// Resolves an address-bar alias such as <c>Documents</c> or the folder's last path segment.
        /// </summary>
        /// <param name="text">Typed path or display name.</param>
        /// <param name="path">Filesystem path when <paramref name="text"/> matches a known place.</param>
        /// <returns><see langword="true"/> when <paramref name="text"/> is a known-place alias.</returns>
        public static bool TryResolveAlias(string? text, [NotNullWhen(true)] out string? path)
        {
            path = null;
            if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var trimmed = text.Trim();
            foreach (var place in GetPlaces())
            {
                if (place.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    path = place.Path;
                    return true;
                }

                var segment = Path.GetFileName(place.Path.TrimTrailingSeparator());
                if (string.IsNullOrEmpty(segment))
                {
                    continue;
                }

                if (!segment.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                path = place.Path;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Whether <paramref name="filesystemPath"/> is exactly a known place.
        /// </summary>
        /// <param name="filesystemPath">Resolved directory path.</param>
        /// <param name="place">Matching place when found.</param>
        /// <returns><see langword="true"/> for an exact known-place path.</returns>
        public static bool TryGetPlace(string filesystemPath, [NotNullWhen(true)] out WindowsKnownPlace? place)
        {
            place = null;
            if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(filesystemPath))
            {
                return false;
            }

            foreach (var candidate in GetPlaces())
            {
                if (!PathRelations.IsSamePath(filesystemPath, candidate.Path))
                {
                    continue;
                }

                place = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the known place that is <paramref name="filesystemPath"/> or its closest ancestor.
        /// </summary>
        /// <param name="filesystemPath">Resolved directory path.</param>
        /// <param name="place">Longest matching known place.</param>
        /// <returns><see langword="true"/> when the path is a known place or under one.</returns>
        public static bool TryGetContainingPlace(
            string filesystemPath,
            [NotNullWhen(true)] out WindowsKnownPlace? place
        )
        {
            place = null;
            if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(filesystemPath))
            {
                return false;
            }

            foreach (var candidate in GetPlaces())
            {
                var isMatch =
                    PathRelations.IsSamePath(filesystemPath, candidate.Path)
                    || PathRelations.IsDescendantOf(filesystemPath, candidate.Path);
                if (!isMatch)
                {
                    continue;
                }

                if (place is not null && candidate.Path.Length <= place.Path.Length)
                {
                    continue;
                }

                place = candidate;
            }

            return place is not null;
        }

        private static void _TryAddPlace(
            List<WindowsKnownPlace> places,
            HashSet<string> pathToIsAdded,
            string name,
            Environment.SpecialFolder folder
        )
        {
            string folderPath;
            try
            {
                folderPath = Environment.GetFolderPath(folder);
            }
            catch (Exception ex) when (ex is ArgumentException or PlatformNotSupportedException)
            {
                return;
            }

            _TryAddResolved(places, pathToIsAdded, name, folderPath);
        }

        private static void _TryAddDownloads(List<WindowsKnownPlace> places, HashSet<string> pathToIsAdded)
        {
            string profile;
            try
            {
                profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            catch (Exception ex) when (ex is ArgumentException or PlatformNotSupportedException)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(profile))
            {
                return;
            }

            _TryAddResolved(places, pathToIsAdded, "Downloads", Path.Combine(profile, "Downloads"));
        }

        private static void _TryAddResolved(
            List<WindowsKnownPlace> places,
            HashSet<string> pathToIsAdded,
            string name,
            string folderPath
        )
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            string fullPath;
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    return;
                }

                fullPath = new DirectoryInfo(folderPath).FullName;
            }
            catch (Exception ex)
                when (ex is ArgumentException or NotSupportedException or IOException or UnauthorizedAccessException)
            {
                return;
            }

            if (!pathToIsAdded.Add(fullPath))
            {
                return;
            }

            places.Add(new WindowsKnownPlace(name, fullPath));
        }
    }
}

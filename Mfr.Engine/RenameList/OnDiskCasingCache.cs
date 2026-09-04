using Mfr.Utils;

namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Per-<see cref="RenameList.RefreshOriginals"/> cache so sibling rows share parent listings and resolved paths.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Must not be reused across refresh calls: Explorer case-only renames change disk casing between F5s.
    /// Walks parents like MFR7 <c>GetFullFileName</c>.
    /// </para>
    /// </remarks>
    internal sealed class OnDiskCasingCache
    {
        private readonly Dictionary<string, string> _pathToResolved = new(PathComparers.Os);
        private readonly Dictionary<string, Dictionary<string, string>> _parentToLeafName = new(PathComparers.Os);

        /// <summary>
        /// Returns <paramref name="path"/> with filesystem casing for each segment that still exists.
        /// </summary>
        /// <param name="path">Stored absolute path (any casing).</param>
        /// <returns>Path rebuilt from on-disk leaf names; unchanged segments when listing finds no match.</returns>
        public string Resolve(string path)
        {
            if (_pathToResolved.TryGetValue(path, out var cached))
            {
                return cached;
            }

            var resolved = _ResolveUncached(path);
            _pathToResolved[path] = resolved;
            return resolved;
        }

        private string _ResolveUncached(string path)
        {
            var trimmedDirectory = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var parent = Path.GetDirectoryName(trimmedDirectory);
            var fileName = Path.GetFileName(trimmedDirectory);
            if (string.IsNullOrEmpty(fileName))
            {
                return path;
            }

            if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
            {
                return path;
            }

            if (!_TryGetOnDiskLeafName(parent, fileName, out var onDiskLeafName))
            {
                return path;
            }

            var resolvedParent = Resolve(parent);
            return Path.Combine(resolvedParent, onDiskLeafName);
        }

        private bool _TryGetOnDiskLeafName(string parent, string fileName, out string onDiskLeafName)
        {
            if (!_parentToLeafName.TryGetValue(parent, out var leafToCasing))
            {
                leafToCasing = new Dictionary<string, string>(PathComparers.Os);
                foreach (var info in new DirectoryInfo(parent).EnumerateFileSystemInfos())
                {
                    leafToCasing[info.Name] = info.Name;
                }

                _parentToLeafName[parent] = leafToCasing;
            }

            return leafToCasing.TryGetValue(fileName, out onDiskLeafName!);
        }
    }
}

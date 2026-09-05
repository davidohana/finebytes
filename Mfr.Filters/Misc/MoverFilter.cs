using Mfr.Filters.Formatting;
using Mfr.Utils;

namespace Mfr.Filters.Misc
{
    /// <summary>
    /// Options for the mover filter.
    /// </summary>
    /// <param name="RootFolder">
    /// Required absolute destination directory. All items are moved under this root.
    /// </param>
    /// <param name="SubFolder">
    /// Optional sub-folder path appended below <paramref name="RootFolder"/>. May contain formatter
    /// tokens (e.g. <c>&lt;file-name&gt;</c>, <c>&lt;parent-folder&gt;</c>) and backslash-separated
    /// hierarchy levels to build deep structures dynamically. Use <c>string.Empty</c> when none; items then
    /// land directly in <paramref name="RootFolder"/>.
    /// </param>
    public sealed record MoverOptions(string RootFolder, string SubFolder = "");

    /// <summary>
    /// Moves items to a destination folder built from a static root and an optional dynamic sub-folder template.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The target parent path is <c>RootFolder</c> + <c>\</c> + resolved <c>SubFolder</c>. If
    /// <c>SubFolder</c> is empty the item lands directly in <c>RootFolder</c>. Backslashes in
    /// <c>SubFolder</c> create nested directory levels. A resolved sub-folder that is a Windows
    /// absolute path (drive or UNC) or that remains rooted after stripping a leading separator is
    /// rejected so <see cref="Path.Combine(string, string)"/> cannot discard <c>RootFolder</c>.
    /// </para>
    /// <para>
    /// Applies to filesystem directory rows in the rename list as well as files (directories use an empty extension
    /// and keep the folder name in <see cref="FileMeta.Prefix"/>).
    /// </para>
    /// <para>
    /// This filter updates only the preview parent-directory path; actual filesystem creation and
    /// movement happen during commit. The filter does not erase original source folders.
    /// </para>
    /// </remarks>
    /// <param name="Options">Mover options.</param>
    [FilterPalette(FilterGroup.Misc, "Mover")]
    public sealed record MoverFilter(MoverOptions Options) : BaseFilter
    {
        private Formatter? _compiledSubFolder;

        /// <summary>
        /// Creates a filter with MFR7 add-to-list defaults (<c>C:\</c> root, <c>MFR</c> sub-folder).
        /// </summary>
        public MoverFilter()
            : this(new MoverOptions(RootFolder: @"C:\", SubFolder: "MFR")) { }

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Mover";

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when <see cref="MoverOptions.RootFolder"/> is empty, whitespace-only, or not an absolute path.</exception>
        protected override void _Setup()
        {
            var root = Options.RootFolder;
            var rootIsBlank = string.IsNullOrWhiteSpace(root);
            Require.That(!rootIsBlank, "MoverFilter: RootFolder must not be empty.", nameof(MoverOptions.RootFolder));

            var rootIsAbsolute = Path.IsPathFullyQualified(root);
            Require.That(
                rootIsAbsolute,
                $"MoverFilter: RootFolder must be an absolute path (got '{root}').",
                nameof(MoverOptions.RootFolder)
            );

            // Unconditional assign (BaseFilter._Setup): `with` copies this field; empty SubFolder must clear it.
            _compiledSubFolder = string.IsNullOrEmpty(Options.SubFolder)
                ? null
                : FormatStringCompiler.Compile(Options.SubFolder);
        }

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            VerifySetupComplete();

            item.Preview.DirectoryPath = _ResolveTargetDirectory(item);
        }

        /// <summary>
        /// Builds <c>RootFolder</c> plus the resolved relative <c>SubFolder</c> (or root alone when empty).
        /// </summary>
        /// <param name="item">Rename list row used when evaluating formatter tokens in <c>SubFolder</c>.</param>
        /// <returns>Absolute preview parent directory for <paramref name="item"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the resolved sub-folder is a Windows absolute path (drive or UNC) or remains rooted
        /// after stripping a leading separator. <see cref="Path.Combine(string, string)"/> would otherwise
        /// discard <c>RootFolder</c>.
        /// </exception>
        private string _ResolveTargetDirectory(RenameItem item)
        {
            if (_compiledSubFolder is null)
            {
                return Options.RootFolder;
            }

            var resolved = _compiledSubFolder(item);
            // Reject drive/UNC forms using Windows path shape (templates use '\'), before host normalize.
            // A single leading '\' (MFR7 "\Sub") is not absolute here and is stripped below.
            var resolvedIsWindowsAbsolute = _IsWindowsAbsoluteSubFolder(resolved);
            Require.That(
                !resolvedIsWindowsAbsolute,
                $"MoverFilter: SubFolder must resolve under RootFolder (got absolute path '{resolved}').",
                nameof(MoverOptions.SubFolder)
            );

            // SubFolder templates use Windows-style '\' for nested levels (MFR7). Normalize to the host
            // separator so Path.Combine builds real nested segments on Linux CI as well as Windows.
            var normalized = resolved.Replace('\\', Path.DirectorySeparatorChar);
            // Strip a leading separator so Path.Combine appends relative segments. Otherwise a value like
            // "\Sub" is rooted on Windows and Path.Combine ignores RootFolder entirely.
            var relative = normalized.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var relativeIsEmpty = string.IsNullOrEmpty(relative);
            if (relativeIsEmpty)
            {
                return Options.RootFolder;
            }

            // Host-rooted leftovers after TrimStart (e.g. odd forms) must not discard RootFolder.
            var relativeIsRooted = Path.IsPathRooted(relative);
            Require.That(
                !relativeIsRooted,
                $"MoverFilter: SubFolder must resolve under RootFolder (got rooted path '{relative}').",
                nameof(MoverOptions.SubFolder)
            );

            return Path.Combine(Options.RootFolder, relative);
        }

        /// <summary>
        /// Returns whether <paramref name="path"/> is a Windows drive or UNC absolute (or drive-relative) path.
        /// </summary>
        /// <param name="path">Resolved sub-folder text before host separator normalization.</param>
        /// <returns><see langword="true"/> when the value must not be combined under <c>RootFolder</c>.</returns>
        private static bool _IsWindowsAbsoluteSubFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var isUnc =
                path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal);
            if (isUnc)
            {
                return true;
            }

            // Drive absolute (X:\ / X:/) or drive-relative (X:foo).
            var hasDrivePrefix = path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':';
            return hasDrivePrefix;
        }
    }
}

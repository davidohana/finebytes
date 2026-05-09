using Mfr.Filters.Formatting;
using Mfr.Models;

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
    /// <c>SubFolder</c> create nested directory levels.
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
    public sealed record MoverFilter(MoverOptions Options) : BaseFilter
    {
        private Func<RenameItem, string>? _compiledSubFolder;

        /// <summary>
        /// Gets the filter type discriminator.
        /// </summary>
        public override string Type => "Mover";

        /// <inheritdoc />
        protected override void _Setup()
        {
            var root = Options.RootFolder;
            var rootIsBlank = string.IsNullOrWhiteSpace(root);
            if (rootIsBlank)
                throw new InvalidOperationException("MoverFilter: RootFolder must not be empty.");

            var rootIsAbsolute = Path.IsPathFullyQualified(root);
            if (!rootIsAbsolute)
            {
                throw new InvalidOperationException(
                    $"MoverFilter: RootFolder must be an absolute path (got '{root}').");
            }

            if (!string.IsNullOrEmpty(Options.SubFolder))
                _compiledSubFolder = FormatStringCompiler.Compile(Options.SubFolder);
        }

        /// <inheritdoc />
        protected internal override void ApplyCore(RenameItem item)
        {
            VerifySetupComplete();

            item.Preview.DirectoryPath = _ResolveTargetDirectory(item);
        }

        private string _ResolveTargetDirectory(RenameItem item)
        {
            if (_compiledSubFolder is null)
                return Options.RootFolder;

            var resolved = _compiledSubFolder(item);
            // Strip a leading slash so Path.Combine appends relative segments. Otherwise a value like
            // "\Sub" is rooted on Windows and Path.Combine ignores RootFolder entirely.
            var relative = resolved.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var relativeIsEmpty = string.IsNullOrEmpty(relative);
            if (relativeIsEmpty)
                return Options.RootFolder;

            return Path.Combine(Options.RootFolder, relative);
        }
    }
}

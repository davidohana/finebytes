using System.Diagnostics.CodeAnalysis;
using Mfr.App.Ui.Services.Shell;

namespace Mfr.App.Ui.Services.Help
{
    /// <summary>
    /// Resolves and opens per-filter Help HTML shipped beside the application.
    /// <para>
    /// Looks under <c>help/</c> next to the exe (<see cref="AppContext.BaseDirectory"/>).
    /// Opens the file with the OS default app (typically the browser).
    /// </para>
    /// </summary>
    /// <param name="shellOpener">
    /// Opens resolved HTML paths. When null, uses <see cref="FileShellOpener.CreateDefault"/>
    /// (no-op on non-Windows).
    /// </param>
    /// <param name="helpRoots">
    /// Directories to search for Help HTML. When null, uses <see cref="DefaultHelpRoots"/>.
    /// </param>
    public sealed class FilterHelpHost(IFileShellOpener? shellOpener = null, IEnumerable<string>? helpRoots = null)
    {
        /// <summary>
        /// Default Help root directories. First existing <c>helpFileName</c> under these roots wins.
        /// </summary>
        public static IReadOnlyList<string> DefaultHelpRoots { get; } =
        [Path.Combine(AppContext.BaseDirectory, "help")];

        private readonly IReadOnlyList<string> _helpRoots = helpRoots is null ? DefaultHelpRoots : [.. helpRoots];
        private readonly IFileShellOpener _shellOpener = shellOpener ?? FileShellOpener.CreateDefault();

        /// <summary>
        /// Builds the missing-help dialog text for a file expected under the app <c>help/</c> folder.
        /// </summary>
        /// <param name="helpFileName">Expected Help HTML basename (e.g. <c>SpaceCharacter.html</c>).</param>
        /// <returns>User-facing message.</returns>
        public static string FormatMissingHelpMessage(string helpFileName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(helpFileName);

            var root = DefaultHelpRoots[0].TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var folder = root + Path.DirectorySeparatorChar;

            return $"Help file '{helpFileName}' was not found in the application folder.\n\n"
                + "Expected location:\n"
                + folder;
        }

        /// <summary>
        /// Finds an existing Help HTML file under the configured roots.
        /// </summary>
        /// <param name="helpFileName">File name only (e.g. <c>SpaceCharacter.html</c>).</param>
        /// <param name="fullPath">Absolute path when found.</param>
        /// <returns><see langword="true"/> when a readable file exists.</returns>
        public bool TryResolve(string helpFileName, [NotNullWhen(true)] out string? fullPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(helpFileName);

            var safeName = Path.GetFileName(helpFileName);
            if (!string.Equals(safeName, helpFileName, StringComparison.Ordinal))
            {
                fullPath = null;
                return false;
            }

            foreach (var root in _helpRoots)
            {
                if (string.IsNullOrWhiteSpace(root))
                {
                    continue;
                }

                var candidate = Path.Combine(root, safeName);
                if (File.Exists(candidate))
                {
                    fullPath = candidate;
                    return true;
                }
            }

            fullPath = null;
            return false;
        }

        /// <summary>
        /// Resolves <paramref name="helpFileName"/> and opens it with the default application.
        /// </summary>
        /// <param name="helpFileName">File name only (e.g. <c>SpaceCharacter.html</c>).</param>
        /// <param name="fullPath">Absolute path when opened.</param>
        /// <returns><see langword="true"/> when the file was found and open was requested.</returns>
        public bool TryOpen(string helpFileName, [NotNullWhen(true)] out string? fullPath)
        {
            if (!TryResolve(helpFileName, out fullPath))
            {
                return false;
            }

            _shellOpener.OpenWithDefaultApp(fullPath);
            return true;
        }
    }
}

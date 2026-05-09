using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.General
{
    /// <summary>
    /// Parsed arguments for <c>&lt;name-list-entry:name-list-file-path&gt;</c>.
    /// </summary>
    /// <param name="NameListFilePath">Full path to the name-list text file.</param>
    internal sealed record NameListEntryFormatOptions(string NameListFilePath)
    {
        /// <summary>
        /// Parses and validates the required file-path argument.
        /// </summary>
        /// <param name="arg">Raw argument text from the template.</param>
        /// <returns>Parsed options.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the required file path argument is missing.
        /// </exception>
        internal static NameListEntryFormatOptions Parse(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg))
                throw new InvalidOperationException(
                    "<name-list-entry> requires one argument: name-list-file-path.");

            return new NameListEntryFormatOptions(NameListFilePath: arg);
        }
    }

    /// <summary>
    /// Resolves the <c>&lt;name-list-entry:name-list-file-path&gt;</c> token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The token reads one line from the provided text file using the item's zero-based
    /// <see cref="FileMeta.GlobalIndex"/> as the line index.
    /// </para>
    /// <para>
    /// When the rename-list index exceeds the number of lines in the file, an empty string is returned.
    /// </para>
    /// </remarks>
    internal sealed class NameListEntryToken : IFormatToken
    {
        private static readonly Dictionary<string, IReadOnlyList<string>> _pathToEntries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Lock _cacheLock = new();

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["name-list-entry"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">
        /// Thrown when token arguments are invalid or item index is negative.
        /// </exception>
        /// <exception cref="UserException">
        /// Thrown when the list file path is invalid, missing, or a line exceeds configured limits.
        /// </exception>
        public string Resolve(string arg, RenameItem item)
        {
            var options = NameListEntryFormatOptions.Parse(arg);
            var entries = _GetEntries(options.NameListFilePath);
            var index = item.Original.GlobalIndex;
            if (index < 0)
                throw new InvalidOperationException(
                    $"<name-list-entry> requires non-negative global index (got {index}).");

            if (index >= entries.Count)
                return string.Empty;

            return entries[index];
        }

        private static IReadOnlyList<string> _GetEntries(string filePath)
        {
            var normalizedPath = Path.GetFullPath(filePath);
            lock (_cacheLock)
            {
                if (_pathToEntries.TryGetValue(normalizedPath, out var cachedEntries))
                    return cachedEntries;

                var loadedEntries = NameListParser.ParseFile(filePath: normalizedPath);
                _pathToEntries[normalizedPath] = loadedEntries;
                return loadedEntries;
            }
        }
    }
}

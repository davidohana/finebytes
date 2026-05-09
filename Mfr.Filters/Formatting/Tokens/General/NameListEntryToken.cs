using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.General
{
    /// <summary>
    /// Resolves the <c>&lt;name-list-entry:name-list-file-path&gt;</c> token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The token reads one line from the provided text file using the item's zero-based
    /// <see cref="FileMeta.GlobalIndex"/> as the line index.
    /// </para>
    /// <para>
    /// When the rename-list index exceeds the number of parsed entries in the file, resolution fails
    /// with a user-facing validation error.
    /// </para>
    /// </remarks>
    internal sealed class NameListEntryToken : IFormatToken
    {
        private const string TokenName = "name-list-entry";

        private static readonly Dictionary<string, IReadOnlyList<string>> _pathToEntries =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Lock _cacheLock = new();

        /// <summary>
        /// Parsed arguments for <c>&lt;name-list-entry:name-list-file-path&gt;</c>.
        /// </summary>
        /// <param name="NameListFilePath">Full path to the name-list text file.</param>
        private sealed record Options(string NameListFilePath);

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = [TokenName];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">
        /// Thrown when token arguments are invalid or item index is negative.
        /// </exception>
        /// <exception cref="UserException">
        /// Thrown when the list file path is invalid, missing, or a line exceeds configured limits.
        /// </exception>
        public string Resolve(string arg, RenameItem item)
        {
            var tokenDisplayName = FormatOptionsParsing.TokenDisplayName(this);
            var options = _ParseOptions(arg);
            var entries = _GetEntries(options.NameListFilePath);
            var index = item.Original.GlobalIndex;
            if (index < 0)
                throw new InvalidOperationException(
                    $"{tokenDisplayName} requires non-negative global index (got {index}).");

            if (index >= entries.Count)
                throw new UserException(
                    $"{tokenDisplayName} index {index} is out of range for '{options.NameListFilePath}' " +
                    $"({entries.Count} parsed entries).");

            return entries[index];
        }

        private Options _ParseOptions(string arg)
        {
            var tokenDisplayName = FormatOptionsParsing.TokenDisplayName(this);
            if (string.IsNullOrWhiteSpace(arg))
                throw new InvalidOperationException(
                    $"{tokenDisplayName} requires one argument: name-list-file-path.");

            return new Options(NameListFilePath: arg);
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

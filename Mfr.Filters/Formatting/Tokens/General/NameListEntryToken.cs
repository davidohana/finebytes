using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.General
{
    /// <summary>
    /// Resolves the <c>&lt;name-list-entry:name-list-file-path&gt;</c> token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The token reads one line from the provided text file using the item's zero-based
    /// <see cref="FileMeta.RenameListIndex"/> as the line index.
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

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = [TokenName];

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when the format argument is missing or whitespace-only.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the item's global index is negative.</exception>
        /// <exception cref="UserException">
        /// Thrown when the list file path is invalid, missing, or a line exceeds configured limits.
        /// </exception>
        public Func<RenameItem, string> Compile(string arg)
        {
            var tokenDisplayName = FormatOptionsParsing.TokenDisplayName(this);
            Require.That(
                !string.IsNullOrWhiteSpace(arg),
                $"{tokenDisplayName} requires one argument: name-list-file-path.",
                nameof(arg));

            var filePath = arg;
            var entries = _GetEntries(filePath);
            return item =>
            {
                var index = item.Original.RenameListIndex;
                if (index < 0)
                    throw new InvalidOperationException(
                        $"{tokenDisplayName} requires non-negative global index (got {index}).");

                if (index >= entries.Count)
                    throw new UserException(
                        $"{tokenDisplayName} index {index} is out of range for '{filePath}' " +
                        $"({entries.Count} parsed entries).");

                return entries[index];
            };
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

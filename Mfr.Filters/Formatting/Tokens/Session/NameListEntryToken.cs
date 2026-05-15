using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens.Session
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
    /// <para>
    /// The list file is parsed when the format string is compiled (for example on each preview
    /// refresh), not held in a cross-compilation cache.
    /// </para>
    /// </remarks>
    internal sealed class NameListEntryToken : IFormatToken
    {
        private const string TokenName = "name-list-entry";

        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = [TokenName];

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when the format argument is missing or whitespace-only.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the item's global index is negative.</exception>
        /// <exception cref="UserException">
        /// Thrown when the list file path is invalid, missing, or a line exceeds configured limits.
        /// </exception>
        public Formatter Compile(string arg)
        {
            var tokenDisplayName = FormatOptionsParsing.TokenDisplayName(this);
            Require.That(
                !string.IsNullOrWhiteSpace(arg),
                $"{tokenDisplayName} requires one argument: name-list-file-path.",
                nameof(arg));

            var normalizedPath = Path.GetFullPath(arg);
            var entries = NameListParser.ParseFile(filePath: normalizedPath);
            return item =>
            {
                var index = item.Original.RenameListIndex;
                if (index < 0)
                    throw new InvalidOperationException(
                        $"{tokenDisplayName} requires non-negative global index (got {index}).");

                if (index >= entries.Count)
                    throw new UserException(
                        $"{tokenDisplayName} index {index} is out of range for '{normalizedPath}' " +
                        $"({entries.Count} parsed entries).");

                return entries[index];
            };
        }
    }
}

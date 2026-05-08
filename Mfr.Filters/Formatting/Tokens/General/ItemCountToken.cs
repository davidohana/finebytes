using System.Globalization;
using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.General
{
    /// <summary>
    /// Parsed arguments for <c>&lt;item-count&gt;</c> (no parameters).
    /// </summary>
    internal readonly record struct ItemCountFormatOptions
    {
        internal static ItemCountFormatOptions Parse(string arg)
        {
            FormatOptionsParsing.RequireNoArgument(arg, "<item-count>");
            return default;
        }
    }

    /// <summary>
    /// Resolves the <c>&lt;item-count&gt;</c> token to the total number of items in the rename list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The count is populated on each item during rename-list preview/commit flow. Synthetic tests without
    /// that context observe <see cref="FileMeta.RenameListTotalCount"/> defaults (typically set via helpers).
    /// </para>
    /// </remarks>
    internal sealed class ItemCountToken : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names { get; } = ["item-count"];

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when arguments are supplied.</exception>
        public string Resolve(string arg, RenameItem item)
        {
            _ = ItemCountFormatOptions.Parse(arg);
            return item.Original.RenameListTotalCount.ToString(CultureInfo.InvariantCulture);
        }
    }
}

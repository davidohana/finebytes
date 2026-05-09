using System.Globalization;
using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.Session
{
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
        public Func<RenameItem, string> Compile(string arg)
        {
            FormatOptionsParsing.RequireNoArgument(arg, FormatOptionsParsing.TokenDisplayName(this));
            return item => item.Original.RenameListTotalCount.ToString(CultureInfo.InvariantCulture);
        }
    }
}

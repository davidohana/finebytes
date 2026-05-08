using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens
{
    /// <summary>
    /// Contract for a single formatter token (e.g. <c>&lt;file-name&gt;</c>) handled by <see cref="FormatStringResolver"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations are discovered automatically: any <c>internal sealed</c> class under
    /// <c>Mfr.Filters.Formatting.Tokens.*</c> with a parameterless constructor that implements this
    /// interface is registered at startup.
    /// </para>
    /// <para>
    /// Aliases are declared by listing multiple entries in <see cref="Names"/> on the same token
    /// (e.g. <c>file-extension</c> and <c>ext</c>).
    /// </para>
    /// </remarks>
    internal interface IFormatToken
    {
        /// <summary>
        /// Gets the canonical name plus any aliases this token responds to.
        /// </summary>
        IReadOnlyList<string> Names { get; }

        /// <summary>
        /// Resolves this token against <paramref name="item"/>.
        /// </summary>
        /// <param name="arg">Raw argument text from the template (after the first <c>:</c>); empty when no argument was supplied.</param>
        /// <param name="item">Rename item providing the source data.</param>
        /// <returns>The resolved replacement text.</returns>
        string Resolve(string arg, RenameItem item);
    }
}

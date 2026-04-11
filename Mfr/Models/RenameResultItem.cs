using System.Text;

namespace Mfr.Models
{
    /// <summary>
    /// Represents one modified property for a rename item.
    /// </summary>
    /// <param name="Property">Name of the property that changed.</param>
    /// <param name="OldValue">Original value before rename.</param>
    /// <param name="NewValue">Updated value after rename.</param>
    public sealed record RenamePropertyChange(
        string Property,
        string OldValue,
        string NewValue)
    {
        /// <summary>
        /// Formats this change using the same layout as console preview output.
        /// </summary>
        /// <returns>Multi-line text block for the change.</returns>
        public string FormatPreviewChangeBlock()
        {
            const int valueLineIndentWidth = 10;
            var valueLinePadding = new string(' ', valueLineIndentWidth);
            var builder = new StringBuilder()
                .Append("  ")
                .Append(Property)
                .Append(':')
                .AppendLine()
                .Append(valueLinePadding)
                .Append(OldValue)
                .Append(" -->")
                .AppendLine()
                .Append(valueLinePadding)
                .Append(NewValue);
            return builder.ToString();
        }
    }

    /// <summary>
    /// Represents the commit outcome for one rename item.
    /// </summary>
    /// <param name="OriginalPath">Original source path before commit.</param>
    /// <param name="Status">Final preview/commit status for this row.</param>
    /// <param name="Error">Optional error message when commit fails.</param>
    /// <param name="Changes">Property-level changes that were applied for this item.</param>
    public sealed record RenameResultItem(
        string OriginalPath,
        RenameStatus Status,
        string? Error,
        IReadOnlyList<RenamePropertyChange> Changes);
}

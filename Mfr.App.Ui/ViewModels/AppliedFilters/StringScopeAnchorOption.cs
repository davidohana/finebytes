using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.AppliedFilters
{
    /// <summary>
    /// Display entry for a <see cref="StringScopeAnchor"/> combo.
    /// </summary>
    /// <param name="Label">Lowercase anchor label shown in the UI.</param>
    /// <param name="Anchor">Anchor value written to apply scope.</param>
    public sealed record StringScopeAnchorOption(string Label, StringScopeAnchor Anchor)
    {
        /// <summary>
        /// Gets substring endpoint anchor choices.
        /// </summary>
        public static IReadOnlyList<StringScopeAnchorOption> All { get; } =
        [
            new("left", StringScopeAnchor.Left),
            new("right", StringScopeAnchor.Right),
        ];

        /// <summary>
        /// Maps an anchor to its combo entry.
        /// </summary>
        /// <param name="anchor">Current anchor.</param>
        /// <returns>Matching list entry.</returns>
        public static StringScopeAnchorOption FromAnchor(StringScopeAnchor anchor)
        {
            return anchor == StringScopeAnchor.Right ? All[1] : All[0];
        }
    }
}

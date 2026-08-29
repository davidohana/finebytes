using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.AppliedFilters
{
    /// <summary>
    /// One Apply-To choice for string-target filters in Filter Configuration.
    /// </summary>
    /// <param name="Label">Combo display text.</param>
    /// <param name="Target">Filter target instance written onto the selected step.</param>
    public sealed record FilterApplyToOption(string Label, FilterTarget Target)
    {
        /// <summary>
        /// Gets the Apply-To choices shown in Filter Configuration (MFR7 basic file-name targets).
        /// </summary>
        public static IReadOnlyList<FilterApplyToOption> All { get; } =
        [
            new("File Prefix", new FilePrefixTarget()),
            new("Extension", new FileExtensionTarget()),
            new("Full File Name", new FileFullNameTarget()),
        ];

        /// <summary>
        /// Maps a filter target to the matching combo entry, or <see langword="null"/> when unsupported.
        /// </summary>
        /// <param name="target">Current filter target.</param>
        /// <returns>The list entry for <paramref name="target"/>.</returns>
        public static FilterApplyToOption? FromTarget(FilterTarget target)
        {
            ArgumentNullException.ThrowIfNull(target);

            return target switch
            {
                FilePrefixTarget => All[0],
                FileExtensionTarget => All[1],
                FileFullNameTarget => All[2],
                _ => null,
            };
        }
    }
}

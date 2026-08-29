using Mfr.Filters;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.AppliedFilters
{
    /// <summary>
    /// Human-readable Apply-To subtitles for Applied Filters list rows.
    /// </summary>
    internal static class FilterTargetLabels
    {
        /// <summary>
        /// Gets the Apply-To subtitle for <paramref name="filter"/>, or empty when not applicable.
        /// </summary>
        /// <param name="filter">Applied filter instance.</param>
        /// <returns>Subtitle text for string-target filters; otherwise an empty string.</returns>
        public static string GetApplyToLabel(BaseFilter filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            if (filter is not StringTargetFilter stringFilter)
            {
                return string.Empty;
            }

            var label = FilterTargetCatalog.GetLabel(stringFilter.Target);
            if (string.IsNullOrEmpty(label))
            {
                return string.Empty;
            }

            return stringFilter.ApplyScope switch
            {
                SubstringApplyScope => $"{label} (Substring)",
                TokenApplyScope => $"{label} (Token)",
                _ => label,
            };
        }
    }
}

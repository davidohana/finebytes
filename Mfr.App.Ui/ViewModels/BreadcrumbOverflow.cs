namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// Chooses which address-bar folders stay visible when the trail is too wide.
    /// </summary>
    internal static class BreadcrumbOverflow
    {
        /// <summary>
        /// Width reserved for the ancestor-overflow button when any leading folder is hidden.
        /// </summary>
        public const double ButtonWidth = 22;

        /// <summary>
        /// Returns the first visible segment index so the current folder stays on screen.
        /// </summary>
        /// <param name="segmentWidths">Root-to-current desired widths.</param>
        /// <param name="availableWidth">Width for an overflow button plus visible segments.</param>
        /// <param name="overflowButtonWidth">Width reserved when any ancestor is hidden.</param>
        /// <returns>
        /// Index of the first visible segment. <c>0</c> means the full trail fits.
        /// </returns>
        public static int PickVisibleStart(
            IReadOnlyList<double> segmentWidths,
            double availableWidth,
            double overflowButtonWidth)
        {
            if (segmentWidths.Count == 0)
                return 0;

            var canShowFullTrail = double.IsNaN(availableWidth) || double.IsInfinity(availableWidth);
            if (canShowFullTrail)
                return 0;

            var lastIndex = segmentWidths.Count - 1;
            var suffixWidth = segmentWidths[lastIndex];
            var visibleStart = lastIndex;
            var reservedOverflowWidth = Math.Max(0, overflowButtonWidth);

            for (var i = lastIndex - 1; i >= 0; i--)
            {
                var nextSuffixWidth = suffixWidth + segmentWidths[i];
                var overflowCost = i > 0 ? reservedOverflowWidth : 0;
                if (overflowCost + nextSuffixWidth > availableWidth)
                    break;

                suffixWidth = nextSuffixWidth;
                visibleStart = i;
            }

            return visibleStart;
        }
    }
}

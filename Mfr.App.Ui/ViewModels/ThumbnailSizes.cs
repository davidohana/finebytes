namespace Mfr.App.Ui.ViewModels
{
    /// <summary>
    /// Discrete thumbnail display sizes for the File Explorer Thumbnails layout.
    /// </summary>
    public static class ThumbnailSizes
    {
        /// <summary>
        /// Smallest preview: 48 CSS pixels on a side.
        /// </summary>
        public const int ExtraSmall = 48;

        /// <summary>
        /// Compact preview: 64 pixels.
        /// </summary>
        public const int Small = 64;

        /// <summary>
        /// Default preview: 96 pixels.
        /// </summary>
        public const int Medium = 96;

        /// <summary>
        /// Large preview: 128 pixels.
        /// </summary>
        public const int Large = 128;

        /// <summary>
        /// Extra-large preview: 192 pixels.
        /// </summary>
        public const int ExtraLarge = 192;

        /// <summary>
        /// Largest preview: 256 pixels. Bitmaps are decoded at this width.
        /// </summary>
        public const int Huge = 256;

        /// <summary>
        /// Starting size when Thumbnails view is first used.
        /// </summary>
        public const int Default = Medium;

        /// <summary>
        /// Extra width around the image so ListBoxItem padding, border, and template margin fit.
        /// </summary>
        public const int CellPadding = 10;

        /// <summary>
        /// Extra height under the image for two name lines, item padding, and margin.
        /// </summary>
        public const int CaptionHeight = 44;

        /// <summary>
        /// Allowed sizes, smallest to largest.
        /// </summary>
        public static readonly int[] Steps = [ExtraSmall, Small, Medium, Large, ExtraLarge, Huge];

        /// <summary>
        /// Snaps <paramref name="size"/> to the nearest allowed step.
        /// <para>
        /// Ties round up so a value halfway between steps becomes the larger size.
        /// </para>
        /// </summary>
        /// <param name="size">Requested display size in pixels.</param>
        /// <returns>One of <see cref="Steps"/>.</returns>
        public static int Clamp(int size)
        {
            var nearest = Steps[0];
            foreach (var step in Steps)
            {
                var stepDistance = Math.Abs(step - size);
                var nearestDistance = Math.Abs(nearest - size);
                var isCloser = stepDistance < nearestDistance;
                var isTiePreferLarger = stepDistance == nearestDistance && step > nearest;
                if (isCloser || isTiePreferLarger)
                    nearest = step;
            }

            return nearest;
        }

        /// <summary>
        /// Next larger step after <paramref name="size"/>, or the maximum when already there.
        /// </summary>
        /// <param name="size">Current display size in pixels.</param>
        /// <returns>One of <see cref="Steps"/>.</returns>
        public static int LargerThan(int size)
        {
            var current = Clamp(size);
            foreach (var step in Steps)
            {
                if (step > current)
                    return step;
            }

            return Steps[^1];
        }

        /// <summary>
        /// Next smaller step before <paramref name="size"/>, or the minimum when already there.
        /// </summary>
        /// <param name="size">Current display size in pixels.</param>
        /// <returns>One of <see cref="Steps"/>.</returns>
        public static int SmallerThan(int size)
        {
            var current = Clamp(size);
            for (var i = Steps.Length - 1; i >= 0; i--)
            {
                if (Steps[i] < current)
                    return Steps[i];
            }

            return Steps[0];
        }
    }
}

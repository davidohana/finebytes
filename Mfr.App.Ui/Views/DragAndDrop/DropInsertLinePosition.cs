using Avalonia;
using Avalonia.Controls;

namespace Mfr.App.Ui.Views.DragAndDrop
{
    /// <summary>
    /// Shared vertical position for salmon insert lines on list/grid drop targets.
    /// </summary>
    internal static class DropInsertLinePosition
    {
        /// <summary>
        /// Fallback Y when the list is empty or the target container is not realized.
        /// </summary>
        public const double FallbackY = 2.0;

        /// <summary>
        /// Resolves insert-line center Y: top of the row at <paramref name="insertIndex"/>, or below the last row when appending.
        /// </summary>
        /// <param name="host">List or grid that hosts the adorner.</param>
        /// <param name="insertIndex">Insert slot in <c>[0, itemCount]</c>.</param>
        /// <param name="itemCount">Number of rows in the drop target.</param>
        /// <param name="getContainer">Returns the visual for a row index, or <see langword="null"/> when unrealized.</param>
        /// <returns>Y relative to <paramref name="host"/>.</returns>
        public static double GetY(Control host, int insertIndex, int itemCount, Func<int, Control?> getContainer)
        {
            if (itemCount == 0)
            {
                return FallbackY;
            }

            if (insertIndex < itemCount)
            {
                return _TopOf(host, getContainer(insertIndex));
            }

            return _BottomOf(host, getContainer(itemCount - 1));
        }

        private static double _TopOf(Control host, Control? container)
        {
            if (container is not null && container.TranslatePoint(default, host) is { } origin)
            {
                return origin.Y;
            }

            return FallbackY;
        }

        private static double _BottomOf(Control host, Control? container)
        {
            if (container is not null && container.TranslatePoint(default, host) is { } origin)
            {
                return origin.Y + container.Bounds.Height;
            }

            return FallbackY;
        }
    }
}

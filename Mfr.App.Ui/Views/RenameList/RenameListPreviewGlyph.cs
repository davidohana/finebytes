using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Compact preview badge shown beside Rename List field labels.
    /// </summary>
    internal static class RenameListPreviewGlyph
    {
        /// <summary>
        /// Style class for preview badge borders.
        /// </summary>
        public const string ClassName = "rename-list-preview-glyph";

        /// <summary>
        /// Badge label text.
        /// </summary>
        public const string Text = "P";

        /// <summary>
        /// Builds a preview badge for grid headers and field shuttle rows.
        /// </summary>
        /// <returns>Styled preview glyph control.</returns>
        public static Border Create()
        {
            return new Border
            {
                Classes = { ClassName },
                Child = new TextBlock
                {
                    Text = Text,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                },
            };
        }

        /// <summary>
        /// Builds a horizontal label row with an optional preview badge.
        /// </summary>
        /// <param name="label">Field display name.</param>
        /// <param name="isPreview">When <see langword="true"/>, appends the preview badge.</param>
        /// <returns>Header or list row content.</returns>
        public static Control CreateLabelRow(string label, bool isPreview)
        {
            var title = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };

            if (!isPreview)
            {
                return title;
            }

            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { title, Create() },
            };
        }
    }
}

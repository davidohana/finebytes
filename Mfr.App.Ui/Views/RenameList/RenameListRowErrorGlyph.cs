using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mfr.App.Ui.Resources;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views.Controls;
using ShapePath = Avalonia.Controls.Shapes.Path;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Simple red exclamation mark shown in the Rename List status column.
    /// </summary>
    internal static class RenameListRowErrorGlyph
    {
        /// <summary>
        /// Style class for the row-error mark.
        /// </summary>
        public const string ClassName = "rename-list-row-error-glyph";

        private static readonly Geometry _MarkGeometry = StreamGeometry.Parse(
            "M5,0.5 H7 V7.5 H5 Z M6,10.2 A1,1 0 1,1 6,8.2 A1,1 0 1,1 6,10.2"
        );

        /// <summary>
        /// Builds a centered red exclamation mark for the status column.
        /// </summary>
        /// <param name="listViewModel">
        /// Host list whose <see cref="RenameListViewModel.FieldDisplayRevision"/> refreshes visibility.
        /// </param>
        /// <returns>Styled row-error mark control.</returns>
        public static Control Create(RenameListViewModel listViewModel)
        {
            ArgumentNullException.ThrowIfNull(listViewModel);

            var mark = new ShapePath
            {
                Classes = { ClassName },
                Data = _MarkGeometry,
                Stretch = Stretch.Uniform,
                Width = 7,
                Height = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            void ApplyVisibility()
            {
                mark.IsVisible = mark.DataContext is RenameListEntry { HasRowError: true };
            }

            mark.DataContextChanged += (_, _) => ApplyVisibility();
            RenameListView.ListenToFieldDisplayRevision(mark, listViewModel, ApplyVisibility);
            ApplyVisibility();

            ToolTip.SetTip(mark, RichToolTip.Wrap(AppTips.RenameListRowErrorGlyph));
            return mark;
        }
    }
}

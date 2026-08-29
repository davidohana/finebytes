using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Mfr.App.Ui.Views.GridColumnSizing
{
    /// <summary>
    /// Keeps DataGrid star columns sized to the cell area, not the vertical scrollbar gutter.
    /// </summary>
    /// <remarks>
    /// Fluent's template sets a local <c>Grid.ColumnSpan="2"</c> on the headers presenter, so a style
    /// cannot shrink it. Without this, star columns stay one scrollbar-width too wide and a horizontal
    /// bar sticks around.
    /// </remarks>
    internal static class DataGridScrollGutter
    {
        /// <summary>
        /// Applies the header-span fix whenever <paramref name="grid"/>'s template is applied.
        /// </summary>
        /// <param name="grid">Target grid.</param>
        internal static void Attach(DataGrid grid)
        {
            ArgumentNullException.ThrowIfNull(grid);
            grid.TemplateApplied += _OnTemplateApplied;
        }

        private static void _OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            var headers = e.NameScope.Find<DataGridColumnHeadersPresenter>("PART_ColumnHeadersPresenter");
            if (headers is null)
            {
                return;
            }

            Grid.SetColumnSpan(headers, 1);
        }
    }
}

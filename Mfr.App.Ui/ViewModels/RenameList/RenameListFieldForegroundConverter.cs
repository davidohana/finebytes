using Avalonia.Controls;
using Avalonia.Media;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Paints Rename List metadata load failures gray (MFR7 <c>ForeErrorColor</c>).
    /// </summary>
    internal static class RenameListFieldForegroundConverter
    {
        /// <summary>
        /// Gray foreground for metadata load-error cells (MFR7 <c>ForeErrorColor</c>).
        /// </summary>
        internal static IBrush ErrorBrush { get; } = new SolidColorBrush(Color.Parse("#808080"));

        /// <summary>
        /// Paints a cell gray when it shows the load-error sentinel; otherwise restores inherited foreground.
        /// </summary>
        /// <param name="textBlock">Grid cell text.</param>
        /// <remarks>
        /// <para>
        /// DataGrid recycles rows, so gray must be cleared when the same <see cref="TextBlock"/> later shows a real
        /// value. Avalonia's <c>DataGridCell</c> has no column property, so this keys off display text rather than
        /// <see cref="RenameListEntry.IsFieldLoadError"/>.
        /// </para>
        /// </remarks>
        internal static void ApplyFromCellText(TextBlock textBlock)
        {
            ArgumentNullException.ThrowIfNull(textBlock);

            if (string.Equals(textBlock.Text, RenameListFieldCatalog.FieldLoadErrorText, StringComparison.Ordinal))
            {
                textBlock.Foreground = ErrorBrush;
                return;
            }

            textBlock.ClearValue(TextBlock.ForegroundProperty);
        }
    }
}

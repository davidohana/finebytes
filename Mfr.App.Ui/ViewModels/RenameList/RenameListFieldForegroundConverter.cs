using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Paints Rename List metadata load failures gray (MFR7 <c>ForeErrorColor</c>).
    /// </summary>
    internal sealed class RenameListFieldForegroundConverter : IValueConverter
    {
        /// <summary>
        /// Shared converter instance for grid column bindings.
        /// </summary>
        public static RenameListFieldForegroundConverter Instance { get; } = new();

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
        /// DataGrid recycles rows, so gray must be cleared when the same <see cref="TextBlock"/> later shows a real value.
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

        /// <summary>
        /// Returns gray for error cells; otherwise transparent so row foreground applies.
        /// </summary>
        /// <param name="entry">Grid row.</param>
        /// <param name="key">Bound field key.</param>
        /// <returns>Cell foreground brush.</returns>
        internal static IBrush GetCellForeground(RenameListEntry? entry, RenameListFieldKey key)
        {
            if (entry is not null && entry.IsFieldLoadError(key))
            {
                return ErrorBrush;
            }

            return Brushes.Transparent;
        }

        /// <inheritdoc />
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is RenameListEntry entry && parameter is RenameListFieldKey key && entry.IsFieldLoadError(key))
            {
                return ErrorBrush;
            }

            return Brushes.Transparent;
        }

        /// <inheritdoc />
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

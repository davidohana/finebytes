using System.Globalization;
using Avalonia.Data.Converters;
using Mfr.App.Ui.Views.Controls;

namespace Mfr.App.Ui.Converters
{
    /// <summary>
    /// Converts a plain string into a wrapping <see cref="RichToolTip"/> for <c>ToolTip.Tip</c> bindings.
    /// </summary>
    public sealed class RichToolTipConverter : IValueConverter
    {
        /// <summary>
        /// Shared converter instance for AXAML bindings.
        /// </summary>
        public static RichToolTipConverter Instance { get; } = new();

        /// <inheritdoc />
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string text || string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return RichToolTip.Wrap(text);
        }

        /// <inheritdoc />
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

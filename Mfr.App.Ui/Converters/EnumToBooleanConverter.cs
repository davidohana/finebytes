using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Mfr.App.Ui.Converters
{
    /// <summary>
    /// Converts an enum value to a radio <c>IsChecked</c> boolean (and back).
    /// <para>
    /// Pass the enum member as <c>ConverterParameter</c> (typically via <c>x:Static</c>). Unchecking a radio
    /// returns <see cref="BindingOperations.DoNothing"/> so sibling radios do not reset the source.
    /// </para>
    /// </summary>
    public sealed class EnumToBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Gets the shared converter instance for <c>x:Static</c> bindings.
        /// </summary>
        public static EnumToBooleanConverter Instance { get; } = new();

        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return Equals(value, parameter);
        }

        /// <inheritdoc />
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not true || parameter is null)
            {
                return BindingOperations.DoNothing;
            }

            return parameter;
        }
    }
}

using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Mfr.App.Ui.Converters
{
    /// <summary>
    /// Converts a <see cref="bool"/> to a radio <c>IsChecked</c> boolean (and back).
    /// <para>
    /// Pass <see cref="True"/> or <see cref="False"/> as <c>ConverterParameter</c>. Unchecking a radio
    /// returns <see cref="BindingOperations.DoNothing"/> so sibling radios do not reset the source.
    /// </para>
    /// </summary>
    public sealed class BooleanToRadioConverter : IValueConverter
    {
        /// <summary>
        /// Gets the shared converter instance for <c>x:Static</c> bindings.
        /// </summary>
        public static BooleanToRadioConverter Instance { get; } = new();

        /// <summary>
        /// Parameter for the radio that represents <see langword="true"/>.
        /// </summary>
        public static object True { get; } = true;

        /// <summary>
        /// Parameter for the radio that represents <see langword="false"/>.
        /// </summary>
        public static object False { get; } = false;

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

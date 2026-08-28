using System.Globalization;
using Avalonia.Data.Converters;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Resolves <see cref="RenameListEntry"/> cell text for a bound <see cref="RenameListFieldKey"/>.
    /// </summary>
    internal sealed class RenameListFieldTextConverter : IValueConverter
    {
        /// <summary>
        /// Shared converter instance for grid column bindings.
        /// </summary>
        public static RenameListFieldTextConverter Instance { get; } = new();

        /// <inheritdoc />
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not RenameListEntry entry || parameter is not RenameListFieldKey key)
            {
                return string.Empty;
            }

            return entry.GetFieldText(key);
        }

        /// <inheritdoc />
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

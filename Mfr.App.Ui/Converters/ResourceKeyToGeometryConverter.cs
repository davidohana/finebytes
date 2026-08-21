using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Mfr.App.Ui.Converters
{
    /// <summary>
    /// Resolves a resource key string to a <see cref="StreamGeometry"/> from application resources.
    /// </summary>
    public sealed class ResourceKeyToGeometryConverter : IValueConverter
    {
        /// <summary>
        /// Shared converter instance for XAML bindings.
        /// </summary>
        public static ResourceKeyToGeometryConverter Instance { get; } = new();

        /// <inheritdoc />
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string key || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            var app = Application.Current;
            if (app is null)
            {
                return null;
            }

            if (
                app.TryGetResource(key, app.ActualThemeVariant, out var resource) && resource is StreamGeometry geometry
            )
            {
                return geometry;
            }

            return null;
        }

        /// <inheritdoc />
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

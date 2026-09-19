namespace Mfr.Models.Media
{
    /// <summary>
    /// Formats <see cref="GeoNamesInfo"/> fields for formatter tokens and Rename List columns.
    /// </summary>
    public static class GeoNamesFormatting
    {
        /// <summary>
        /// Formats one nearby GeoNames field.
        /// </summary>
        /// <param name="info">Loaded snapshot, or <see langword="null"/> when unset.</param>
        /// <param name="field">Which property to format.</param>
        /// <returns>Field text, or empty when absent.</returns>
        public static string Format(GeoNamesInfo? info, GeoNamesField field)
        {
            if (info is null)
            {
                return string.Empty;
            }

            var value = field switch
            {
                GeoNamesField.Place => info.Place,
                GeoNamesField.Region => info.Region,
                GeoNamesField.Country => info.Country,
                _ => null,
            };

            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

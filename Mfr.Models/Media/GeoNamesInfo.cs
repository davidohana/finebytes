namespace Mfr.Models.Media
{
    /// <summary>
    /// Read-only GeoNames <c>findNearby</c> snapshot for formatter tokens and Rename List columns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Populated lazily from HTTPS GeoNames when GPS is present. Missing fields are
    /// <see langword="null"/>. No GPS leaves this cache unset or empty (tokens expand empty).
    /// Failures are not stored here — they surface as PreviewError / Rename List load errors.
    /// </para>
    /// </remarks>
    public sealed record GeoNamesInfo
    {
        /// <summary>
        /// Gets the nearby place name (<c>name</c>), or <see langword="null"/> when absent.
        /// </summary>
        public string? Place { get; init; }

        /// <summary>
        /// Gets the first-level admin region (<c>adminName1</c>), or <see langword="null"/> when absent.
        /// </summary>
        public string? Region { get; init; }

        /// <summary>
        /// Gets the country name (<c>countryName</c>), or <see langword="null"/> when absent.
        /// </summary>
        public string? Country { get; init; }
    }
}

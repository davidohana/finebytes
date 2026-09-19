namespace Mfr.Metadata.GeoNames
{
    /// <summary>
    /// GeoNames rejected the request because of hourly/daily credit limits or quota.
    /// </summary>
    /// <param name="message">User-facing rate-limit explanation.</param>
    public sealed class GeoNamesRateLimitException(string message) : InvalidOperationException(message);
}

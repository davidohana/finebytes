namespace Mfr.Engine.Beta
{
    /// <summary>
    /// Thrown when a non-dry-run commit is refused because the beta build has expired.
    /// </summary>
    /// <param name="expiresUtc">UTC instant at which the beta becomes expired (inclusive).</param>
    public sealed class BetaExpiredException(DateTime expiresUtc) : Exception(FormatMessage(expiresUtc))
    {
        /// <summary>
        /// Gets the UTC expiry instant used when the commit was blocked.
        /// </summary>
        public DateTime ExpiresUtc { get; } = expiresUtc;

        /// <summary>
        /// Builds the user-facing expiry message for <paramref name="expiresUtc"/>.
        /// </summary>
        /// <param name="expiresUtc">UTC instant at which the beta becomes expired (inclusive).</param>
        /// <returns>Status / CLI text including the expiry day and download guidance.</returns>
        public static string FormatMessage(DateTime expiresUtc)
        {
            var day = expiresUtc.ToUniversalTime().ToString("yyyy-MM-dd");
            return $"This beta expired on {day} UTC. Download a newer beta or the release.";
        }
    }
}

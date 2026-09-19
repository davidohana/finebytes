namespace Mfr.Models.Media
{
    /// <summary>
    /// Shared GeoNames defaults for Options prefs and the HTTPS client.
    /// </summary>
    public static class GeoNamesDefaults
    {
        /// <summary>
        /// FineBytes-registered GeoNames username used as the Options / config default.
        /// </summary>
        public const string Username = "fbmfr";

        /// <summary>
        /// Resolves an Options / config username (blank → <see cref="Username"/>).
        /// </summary>
        /// <param name="username">Configured username, or <see langword="null"/>/blank for default.</param>
        /// <returns>Trimmed username used for HTTPS and cache keys.</returns>
        public static string ResolveUsername(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Username;
            }

            return username.Trim();
        }
    }
}

using Mfr.Engine.Presets;
using Mfr.Models.Config;

namespace Mfr.Engine.Config
{
    /// <summary>
    /// Deletes roaming AppData files that correspond to MFR7 <c>mfrconfig.xml</c> (Reset Configuration).
    /// <para>
    /// Removes <c>config.json</c>, <c>session.json</c>, and <c>filter-defaults.json</c>. Does not touch
    /// <c>presets.json</c> or local logs.
    /// </para>
    /// </summary>
    public static class PersistedConfigurationReset
    {
        /// <summary>
        /// Deletes the three AppData files when present.
        /// </summary>
        /// <param name="configFilePath">
        /// Config path, or blank for the default AppData <c>config.json</c>.
        /// </param>
        /// <param name="sessionFilePath">
        /// Session path, or blank for the default AppData <c>session.json</c>.
        /// </param>
        /// <param name="filterDefaultsFilePath">
        /// Filter defaults path, or blank for <see cref="FilterDefaultsStore.DefaultFilePath"/>.
        /// </param>
        /// <exception cref="IOException">Thrown when a present file cannot be deleted.</exception>
        public static void DeleteAppDataFiles(
            string? configFilePath = null,
            string? sessionFilePath = null,
            string? filterDefaultsFilePath = null
        )
        {
            ConfigStore.DeleteDefaultFile(configFilePath);
            SessionStore.Delete(sessionFilePath);
            FilterDefaultsStore.DeleteFileAt(filterDefaultsFilePath);
        }
    }
}

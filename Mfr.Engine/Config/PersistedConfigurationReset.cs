using Mfr.Models.Config;

namespace Mfr.Engine.Config
{
    /// <summary>
    /// Deletes the roaming prefs file that corresponds to MFR7 <c>mfrconfig.xml</c> (Reset Configuration).
    /// <para>
    /// Removes <c>config.json</c> only (log/ui + session + filterDefaults). Does not touch
    /// <c>presets.json</c> or local logs.
    /// </para>
    /// </summary>
    public static class PersistedConfigurationReset
    {
        /// <summary>
        /// Deletes the prefs file when present.
        /// </summary>
        /// <param name="configFilePath">
        /// Config path, or blank for the default AppData <c>config.json</c>.
        /// </param>
        /// <exception cref="IOException">Thrown when a present file cannot be deleted.</exception>
        public static void DeleteAppDataFiles(string? configFilePath = null)
        {
            ConfigStore.DeleteDefaultFile(configFilePath);
        }
    }
}

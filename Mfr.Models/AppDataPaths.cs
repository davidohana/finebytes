using Mfr.Utils;

namespace Mfr.Models
{
    /// <summary>
    /// Shared user-profile roots for MFR files under <c>finebytes/mfr</c>.
    /// <para>
    /// Roaming holds config, session, and presets; local holds diagnostic logs.
    /// </para>
    /// </summary>
    public static class AppDataPaths
    {
        /// <summary>
        /// Vendor directory name under ApplicationData / LocalApplicationData.
        /// </summary>
        public const string VendorDirectoryName = "finebytes";

        /// <summary>
        /// Product directory name under the vendor folder.
        /// </summary>
        public const string ProductDirectoryName = "mfr";

        /// <summary>
        /// Roaming product root (<c>%ApplicationData%/finebytes/mfr</c>).
        /// </summary>
        /// <returns>Absolute path to the roaming MFR data directory.</returns>
        public static string RoamingRoot()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return appData.CombinePath(VendorDirectoryName, ProductDirectoryName);
        }

        /// <summary>
        /// Local product root (<c>%LocalApplicationData%/finebytes/mfr</c>).
        /// </summary>
        /// <returns>Absolute path to the local MFR data directory.</returns>
        public static string LocalRoot()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return localAppData.CombinePath(VendorDirectoryName, ProductDirectoryName);
        }
    }
}

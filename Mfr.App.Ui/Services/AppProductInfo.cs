using System.Globalization;
using System.Reflection;
using Mfr.Engine.Beta;
using Mfr.Utils;

namespace Mfr.App.Ui.Services
{
    /// <summary>
    /// Shared product name, display version, and copyright for the window title and About dialog.
    /// </summary>
    public static class AppProductInfo
    {
        /// <summary>Product web site opened from About.</summary>
        public const string WebSiteUrl = ProductUrls.WebSite;

        /// <summary>Support mailbox opened from About (<c>mailto:</c>).</summary>
        public const string SupportEmailUrl = "mailto:support@finebytes.com";

        /// <summary>
        /// Returns the product display name from the entry assembly attributes.
        /// </summary>
        /// <param name="assembly">
        /// Assembly to read. When null, uses the UI assembly that defines this type.
        /// </param>
        /// <returns>Product name, or <c>Magic File Renamer</c> when the attribute is missing.</returns>
        public static string GetProductName(Assembly? assembly = null)
        {
            var source = assembly ?? typeof(AppProductInfo).Assembly;
            var product = source.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
            if (!string.IsNullOrWhiteSpace(product))
            {
                return product;
            }

            return "Magic File Renamer";
        }

        /// <summary>
        /// Returns the user-facing version string (informational version, else three-part assembly version).
        /// </summary>
        /// <param name="assembly">
        /// Assembly to read. When null, uses the UI assembly that defines this type.
        /// </param>
        /// <returns>
        /// Display version, or <c>unknown</c> when unavailable.
        /// </returns>
        public static string GetDisplayVersion(Assembly? assembly = null)
        {
            var source = assembly ?? typeof(AppProductInfo).Assembly;
            var informational = source
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
            var version = !string.IsNullOrWhiteSpace(informational)
                ? informational
                : source.GetName().Version?.ToString(3) ?? "unknown";
            return version;
        }

        /// <summary>
        /// Returns a short About-dialog line for the beta expiry date.
        /// </summary>
        /// <returns>One-line notice including <see cref="BetaExpiryGate.ExpiresUtc"/>.</returns>
        public static string GetBetaExpiryNotice()
        {
            var expiry = BetaExpiryGate.ExpiresUtc.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
            return $"Beta expires {expiry} UTC — after that, GO is blocked until you install a newer build.";
        }

        /// <summary>
        /// Returns the assembly copyright string.
        /// </summary>
        /// <param name="assembly">
        /// Assembly to read. When null, uses the UI assembly that defines this type.
        /// </param>
        /// <returns>Copyright text, or empty when the attribute is missing.</returns>
        public static string GetCopyright(Assembly? assembly = null)
        {
            var source = assembly ?? typeof(AppProductInfo).Assembly;
            return source.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;
        }
    }
}

using System.Diagnostics.CodeAnalysis;

namespace Mfr.Utils
{
    /// <summary>
    /// Provides convenience extensions for common string checks.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Determines whether a value is <c>null</c>, empty, or whitespace-only.
        /// </summary>
        /// <param name="value">The string value to evaluate.</param>
        /// <returns><c>true</c> when the value is null/empty/whitespace; otherwise <c>false</c>.</returns>
        public static bool IsBlank([NotNullWhen(false)] this string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }
    }
}

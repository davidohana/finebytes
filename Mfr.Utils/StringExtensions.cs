using System.Diagnostics.CodeAnalysis;

namespace Mfr.Utils
{
    /// <summary>
    /// Provides convenience extensions for common string operations.
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

        /// <summary>
        /// Trims the string and returns <see langword="null"/> when the result is empty.
        /// </summary>
        /// <param name="value">Input text.</param>
        /// <returns>The trimmed string, or <see langword="null"/> when <paramref name="value"/> is <see langword="null"/> or trimming yields an empty string.</returns>
        public static string? TrimmedOrNull(this string? value)
        {
            if (value is null)
                return null;

            var trimmed = value.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }
    }
}

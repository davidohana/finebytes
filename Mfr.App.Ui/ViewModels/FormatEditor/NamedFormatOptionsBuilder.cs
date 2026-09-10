using System.Globalization;
using System.Text;
using Mfr.Filters.Formatting.Tokens;

namespace Mfr.App.Ui.ViewModels.FormatEditor
{
    /// <summary>
    /// Builds named <c>key=value</c> format-token arguments and soft-parses them for dialog editors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Split/parse rules live in <see cref="FormatOptionsParsing"/> so dialog OK and Compile cannot drift.
    /// Soft <c>Get*</c> defaults stay here; Compile keeps throw-on-invalid behavior.
    /// </para>
    /// </remarks>
    public static class NamedFormatOptionsBuilder
    {
        /// <summary>
        /// Joins named options as <c>key=value,key=value</c> (no surrounding whitespace).
        /// </summary>
        /// <param name="pairs">Option keys and values in emission order.</param>
        /// <returns>Joined argument text, or empty when <paramref name="pairs"/> is empty.</returns>
        public static string Join(params (string Key, string Value)[] pairs)
        {
            ArgumentNullException.ThrowIfNull(pairs);
            if (pairs.Length == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            for (var i = 0; i < pairs.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                sb.Append(pairs[i].Key);
                sb.Append('=');
                sb.Append(pairs[i].Value);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Formats an invariant-culture integer for named options.
        /// </summary>
        /// <param name="value">Integer value.</param>
        /// <returns>Invariant digit string.</returns>
        public static string FormatInt(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats an invariant-culture boolean as lowercase <c>true</c>/<c>false</c>.
        /// </summary>
        /// <param name="value">Boolean value.</param>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public static string FormatBool(bool value)
        {
            return value ? "true" : "false";
        }

        /// <summary>
        /// Tries to parse <c>key=value</c> segments separated by commas outside nested <c>&lt;…&gt;</c>.
        /// </summary>
        /// <param name="args">Raw argument text after the token name's <c>:</c>.</param>
        /// <param name="keyToValue">Case-insensitive map of trimmed keys to trimmed values when parse succeeds.</param>
        /// <returns><see langword="true"/> when every non-empty segment is a valid unique <c>key=value</c> pair.</returns>
        public static bool TryParse(string? args, out Dictionary<string, string> keyToValue)
        {
            keyToValue = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(args))
            {
                return true;
            }

            try
            {
                keyToValue = FormatOptionsParsing.ParseNamedKeyValuePairs(args.Trim(), "<format-options>");
                return true;
            }
            catch (ArgumentException)
            {
                keyToValue = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                return false;
            }
        }

        /// <summary>
        /// Reads an integer option, or <paramref name="fallback"/> when missing/invalid.
        /// </summary>
        /// <param name="keyToValue">Parsed options.</param>
        /// <param name="key">Option key.</param>
        /// <param name="fallback">Default when absent or not an integer.</param>
        /// <returns>Parsed integer or fallback.</returns>
        public static int GetInt(IReadOnlyDictionary<string, string> keyToValue, string key, int fallback)
        {
            if (!keyToValue.TryGetValue(key, out var raw))
            {
                return fallback;
            }

            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;
        }

        /// <summary>
        /// Reads a boolean option, or <paramref name="fallback"/> when missing/invalid.
        /// </summary>
        /// <param name="keyToValue">Parsed options.</param>
        /// <param name="key">Option key.</param>
        /// <param name="fallback">Default when absent or not a boolean.</param>
        /// <returns>Parsed boolean or fallback.</returns>
        public static bool GetBool(IReadOnlyDictionary<string, string> keyToValue, string key, bool fallback)
        {
            if (!keyToValue.TryGetValue(key, out var raw))
            {
                return fallback;
            }

            return bool.TryParse(raw.Trim(), out var value) ? value : fallback;
        }

        /// <summary>
        /// Reads a string option, or <paramref name="fallback"/> when missing.
        /// </summary>
        /// <param name="keyToValue">Parsed options.</param>
        /// <param name="key">Option key.</param>
        /// <param name="fallback">Default when absent.</param>
        /// <returns>Trimmed value or fallback.</returns>
        public static string GetString(IReadOnlyDictionary<string, string> keyToValue, string key, string fallback)
        {
            return keyToValue.TryGetValue(key, out var raw) ? raw : fallback;
        }
    }
}

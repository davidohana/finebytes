using Mfr.Utils;

namespace Mfr.Filters.Formatting.Tokens
{
    /// <summary>
    /// Shared helpers used by multiple formatter tokens (display labels, keyword hints, named arguments, and common preconditions).
    /// </summary>
    internal static class FormatOptionsParsing
    {
        /// <summary>
        /// Splits <paramref name="arg"/> on commas that are not inside balanced <c>&lt;…&gt;</c> segments (depth tracked per character).
        /// </summary>
        /// <param name="arg">Raw formatter token argument text.</param>
        /// <returns>Segments before trimming; callers typically trim each segment.</returns>
        internal static List<string> SplitNamedArgumentSegments(string arg)
        {
            var segments = new List<string>();
            var depth = 0;
            var start = 0;
            for (var i = 0; i < arg.Length; i++)
            {
                var c = arg[i];
                if (c == '<')
                    depth++;
                else if (c == '>')
                    depth = Math.Max(0, depth - 1);
                else if (c == ',' && depth == 0)
                {
                    segments.Add(arg[start..i]);
                    start = i + 1;
                }
            }

            segments.Add(arg[start..]);
            return segments;
        }

        /// <summary>
        /// Parses <c>name=value</c> segments (comma-separated at bracket depth 0); surrounding whitespace on keys and values is trimmed.
        /// </summary>
        /// <param name="arg">Non-empty argument text after the first <c>:</c> in the token.</param>
        /// <param name="tokenDisplayName">Token label for errors (for example <c>&lt;counter&gt;</c>).</param>
        /// <returns>Case-insensitive keys mapped to trimmed values.</returns>
        /// <exception cref="ArgumentException">Thrown when a segment is empty, missing <c>=</c>, or duplicates a key.</exception>
        internal static Dictionary<string, string> ParseNamedKeyValuePairs(string arg, string tokenDisplayName)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var segment in SplitNamedArgumentSegments(arg))
            {
                var trimmed = segment.Trim();
                if (trimmed.Length == 0)
                {
                    throw new ArgumentException(
                        $"{tokenDisplayName} has an empty name=value segment in '{arg}'.",
                        nameof(arg));
                }

                var eq = trimmed.IndexOf('=');
                if (eq <= 0)
                {
                    throw new ArgumentException(
                        $"{tokenDisplayName} segment '{trimmed}' is not a valid name=value pair.",
                        nameof(arg));
                }

                var key = trimmed[..eq].Trim();
                var value = trimmed[(eq + 1)..].Trim();
                if (key.Length == 0)
                {
                    throw new ArgumentException(
                        $"{tokenDisplayName} segment '{trimmed}' is missing a key before '='.",
                        nameof(arg));
                }

                if (!map.TryAdd(key, value))
                {
                    throw new ArgumentException(
                        $"{tokenDisplayName} duplicate option '{key}' in '{arg}'.",
                        nameof(arg));
                }
            }

            return map;
        }

        /// <summary>
        /// Ensures every key in <paramref name="map"/> is listed in <paramref name="allowedOptionKeys"/> (case-insensitive).
        /// </summary>
        /// <param name="map">Parsed option keys from <see cref="ParseNamedKeyValuePairs"/>.</param>
        /// <param name="tokenDisplayName">Token label for errors (for example <c>&lt;substr&gt;</c>).</param>
        /// <param name="allowedOptionKeys">Canonical allowed names; order defines the phrase passed to <see cref="FormatExpectedKeywords"/>.</param>
        /// <param name="argParamName">Parameter name for <see cref="ArgumentException.ParamName"/>.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="map"/> contains a key not in <paramref name="allowedOptionKeys"/>.</exception>
        internal static void RequireKnownOptionKeysOnly(
            Dictionary<string, string> map,
            string tokenDisplayName,
            IReadOnlyList<string> allowedOptionKeys,
            string argParamName)
        {
            var allowed = new HashSet<string>(allowedOptionKeys, StringComparer.OrdinalIgnoreCase);
            foreach (var key in map.Keys)
            {
                if (!allowed.Contains(key))
                {
                    throw new ArgumentException(
                        $"{tokenDisplayName} unknown option '{key}' (expected {FormatExpectedKeywords(allowedOptionKeys)}).",
                        argParamName);
                }
            }
        }

        /// <summary>
        /// Formats keyword strings as a short English list for error messages (<c>x or y</c>; <c>x, y, or z</c>).
        /// </summary>
        /// <param name="keywords">Keywords to list (for example dictionary keys; insertion order is preserved).</param>
        /// <returns>A phrase suitable after <c>expected</c> in a user-facing message.</returns>
        internal static string FormatExpectedKeywords(IEnumerable<string> keywords)
        {
            var keys = keywords.ToArray();
            return keys.Length switch
            {
                0 => "",
                1 => keys[0],
                2 => $"{keys[0]} or {keys[1]}",
                _ => $"{string.Join(", ", keys[..^1])}, or {keys[^1]}",
            };
        }

        /// <summary>
        /// Gets canonical token label for messages (for example <c>&lt;file-name&gt;</c>).
        /// </summary>
        /// <param name="token">Token instance.</param>
        /// <returns>Display form of the first token name wrapped in angle brackets.</returns>
        internal static string TokenDisplayName(IFormatToken token)
        {
            return $"<{token.Names[0]}>";
        }

        /// <summary>
        /// Validates that no extra formatter argument text is supplied (<see cref="Require"/> precondition).
        /// </summary>
        /// <param name="arg">Raw argument text after <c>:</c>.</param>
        /// <param name="tokenDisplayName">Token label for error messages.</param>
        /// <exception cref="ArgumentException">Thrown when unexpected arguments are present.</exception>
        internal static void RequireNoArgument(string arg, string tokenDisplayName)
        {
            Require.That(string.IsNullOrWhiteSpace(arg), $"{tokenDisplayName} does not accept arguments.", nameof(arg));
        }
    }
}

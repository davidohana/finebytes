using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Mfr.Models.Media
{
    /// <summary>
    /// Case-insensitive keyword ↔ <see cref="TimestampField"/> map from JSON enum member names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Keywords match <see cref="JsonStringEnumMemberNameAttribute"/> on <see cref="TimestampField"/>
    /// (<c>creation</c>, <c>lastWrite</c>, <c>lastAccess</c>) so formatter tokens and preset JSON stay aligned.
    /// </para>
    /// </remarks>
    public static class TimestampFieldKeywords
    {
        private static readonly Dictionary<string, TimestampField> _keywordToField = _BuildKeywordToField();

        private static readonly Dictionary<TimestampField, string> _fieldToKeyword = _keywordToField.ToDictionary(
            static pair => pair.Value,
            static pair => pair.Key
        );

        /// <summary>
        /// Gets the JSON/enum keywords in declaration order (for error hints).
        /// </summary>
        public static IEnumerable<string> Keywords =>
            Enum.GetValues<TimestampField>().Select(static timestampField => _fieldToKeyword[timestampField]);

        /// <summary>
        /// Maps a case-insensitive keyword to <see cref="TimestampField"/>.
        /// </summary>
        /// <param name="raw">Keyword text, or <see langword="null"/>/blank.</param>
        /// <param name="timestampField">Resolved field when this method returns <see langword="true"/>.</param>
        /// <returns><see langword="true"/> when <paramref name="raw"/> matches a known keyword.</returns>
        public static bool TryParse(string? raw, out TimestampField timestampField)
        {
            timestampField = default;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            return _keywordToField.TryGetValue(raw.Trim(), out timestampField);
        }

        /// <summary>
        /// Gets the canonical JSON keyword for <paramref name="timestampField"/>.
        /// </summary>
        /// <param name="timestampField">Filesystem timestamp field.</param>
        /// <returns>JSON enum member name (e.g. <c>lastWrite</c>).</returns>
        /// <exception cref="UnreachableException">Unexpected enum value.</exception>
        public static string GetKeyword(TimestampField timestampField)
        {
            if (_fieldToKeyword.TryGetValue(timestampField, out var keyword))
            {
                return keyword;
            }

            throw new UnreachableException($"Unexpected {nameof(TimestampField)} value: {timestampField}.");
        }

        private static Dictionary<string, TimestampField> _BuildKeywordToField()
        {
            var keywordToField = new Dictionary<string, TimestampField>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in Enum.GetValues<TimestampField>())
            {
                var member =
                    typeof(TimestampField).GetField(field.ToString())
                    ?? throw new UnreachableException($"Missing enum member field for {field}.");
                var attribute =
                    member.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()
                    ?? throw new UnreachableException(
                        $"{nameof(TimestampField)}.{field} is missing [JsonStringEnumMemberName]."
                    );
                keywordToField.Add(attribute.Name, field);
            }

            return keywordToField;
        }
    }
}

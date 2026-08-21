using System.Collections.Immutable;
using Mfr.Models.Tags;

namespace Mfr.Metadata.TagFields
{
    /// <summary>
    /// Original→Preview key diffing shared by the per-type tag field writers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every block type indexes its rows by a key (Xiph/APE field key, INFO fourCC, ASF descriptor name,
    /// Apple atom type, ID3v2 frame identity) and then touches only the keys that were dropped or changed.
    /// Keys absent from both sides are never written, which is what leaves unmodeled on-disk content alone.
    /// </para>
    /// </remarks>
    internal static class TagFieldDiff
    {
        /// <summary>
        /// Removes keys the preview dropped, then sets keys the preview added or changed.
        /// </summary>
        /// <typeparam name="TValue">Per-key field value.</typeparam>
        /// <param name="original">Keys and values read from disk.</param>
        /// <param name="preview">Keys and values the preview wants on disk.</param>
        /// <param name="valuesEqual">Compares the original and preview value of one key.</param>
        /// <param name="remove">Clears one key on the live tag.</param>
        /// <param name="set">Writes one key on the live tag.</param>
        public static void Apply<TValue>(
            Dictionary<string, TValue> original,
            Dictionary<string, TValue> preview,
            Func<TValue, TValue, bool> valuesEqual,
            Action<string> remove,
            Action<string, TValue> set
        )
        {
            foreach (var key in original.Keys)
            {
                if (preview.ContainsKey(key))
                    continue;

                remove(key);
            }

            foreach (var (key, value) in preview)
            {
                if (original.TryGetValue(key, out var prior) && valuesEqual(prior, value))
                    continue;

                set(key, value);
            }
        }

        /// <summary>
        /// Indexes text field rows by key.
        /// </summary>
        /// <param name="fields">Rows from a Xiph or APE block.</param>
        /// <returns>Key → values map; a duplicate key keeps the last row.</returns>
        public static Dictionary<string, ImmutableArray<string>> IndexTextFields(ImmutableArray<TextFieldRow> fields)
        {
            var keyToValues = new Dictionary<string, ImmutableArray<string>>(StringComparer.Ordinal);
            foreach (var row in fields)
                keyToValues[row.Key] = row.Values;

            return keyToValues;
        }
    }
}

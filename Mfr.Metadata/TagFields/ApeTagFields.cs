using System.Collections.Immutable;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Ape;
using Mfr.Utils;
using TagLib;
using ApeTag = TagLib.Ape.Tag;

namespace Mfr.Metadata.TagFields
{
    /// <summary>
    /// Reads and field-patches the modeled APE text items on a live TagLib tag.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read folds alias spellings into their modeled key and splits <c>number/total</c> pairs so counts reach
    /// their own key. Item lookup is case-insensitive, so casing variants need no alias entry. Unknown on-disk
    /// items survive every patch by omission.
    /// </para>
    /// </remarks>
    internal static class ApeTagFields
    {
        private static readonly string[] _KnownKeys =
        [
            "Title", "Album", "Artist", "Album Artist", "Composer", "Genre",
            "Comment", "Lyrics", "Copyright", "Grouping",
            "Year", "Track", "TrackCount", "Disc", "DiscCount",
            "BPM", "Conductor",
        ];

        // Spellings other taggers use for a modeled APE key; values are stored under the modeled key.
        private static readonly Dictionary<string, string> _AliasToKnownKey = new(StringComparer.Ordinal)
        {
            ["ALBUMARTIST"] = "Album Artist",
        };

        /// <summary>
        /// Reads the file's known APE text items.
        /// </summary>
        /// <param name="file">Open TagLib file.</param>
        /// <returns>Block data, or <see langword="null"/> when the tag is absent or holds no known item.</returns>
        public static ApeTagData? Read(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Ape, false) is not ApeTag live || live.IsEmpty)
                return null;

            var keyToValues = new Dictionary<string, ImmutableArray<string>>(StringComparer.Ordinal);
            foreach (var key in _KnownKeys)
            {
                var values = _ReadItem(live, key);
                if (values.Length == 0)
                    continue;

                keyToValues[key] = values;
            }

            foreach (var (alias, knownKey) in _AliasToKnownKey)
            {
                if (keyToValues.ContainsKey(knownKey))
                    continue;

                var values = _ReadItem(live, alias);
                if (values.Length == 0)
                    continue;

                keyToValues[knownKey] = values;
            }

            _SplitCountPair(keyToValues, numberKey: "Track", countKey: "TrackCount");
            _SplitCountPair(keyToValues, numberKey: "Disc", countKey: "DiscCount");

            if (keyToValues.Count == 0)
                return null;

            var rows = keyToValues.Select(static kvp => new TextFieldRow(kvp.Key, kvp.Value)).ToList();
            rows.Sort(_CompareRows);
            return new ApeTagData { Fields = [.. rows] };
        }

        /// <summary>
        /// Creates or patches the file's known APE items from <paramref name="original"/> → <paramref name="preview"/>.
        /// </summary>
        /// <param name="file">Open TagLib file.</param>
        /// <param name="original">Block as read from disk, or <see langword="null"/> to create.</param>
        /// <param name="preview">Block the preview wants on disk.</param>
        public static void Apply(TagLib.File file, ApeTagData? original, ApeTagData preview)
        {
            if (Equals(original, preview))
                return;

            var live = (ApeTag)file.GetTag(TagTypes.Ape, true);
            if (original is null)
            {
                _WriteAll(live, preview);
                return;
            }

            TagFieldDiff.Apply(
                TagFieldDiff.IndexTextFields(original.Fields),
                TagFieldDiff.IndexTextFields(preview.Fields),
                valuesEqual: OrdinalSequence.AreEqual,
                remove: key => live.RemoveItem(key),
                set: (key, values) => live.SetValue(key, [.. values]));
        }

        private static void _WriteAll(ApeTag live, ApeTagData data)
        {
            foreach (var key in _KnownKeys)
                live.RemoveItem(key);

            foreach (var row in data.Fields)
            {
                if (row.Values.Length == 0)
                    continue;

                live.SetValue(row.Key, [.. row.Values]);
            }
        }

        private static ImmutableArray<string> _ReadItem(ApeTag live, string key)
        {
            var item = live.GetItem(key);
            if (item is null || item.IsEmpty)
                return [];

            return DelimitedText.TrimNonEmpty(item.ToStringArray());
        }

        private static void _SplitCountPair(
            Dictionary<string, ImmutableArray<string>> keyToValues,
            string numberKey,
            string countKey)
        {
            if (!keyToValues.TryGetValue(numberKey, out var values) || values.Length == 0)
                return;

            var parts = values[0].Split('/', 2);
            if (parts.Length != 2)
                return;

            var number = parts[0].TrimmedOrNull();
            if (number is null)
                keyToValues.Remove(numberKey);
            else
                keyToValues[numberKey] = [number];

            var count = parts[1].TrimmedOrNull();
            if (count is not null && !keyToValues.ContainsKey(countKey))
                keyToValues[countKey] = [count];
        }

        private static int _CompareRows(TextFieldRow a, TextFieldRow b)
        {
            var byKey = string.CompareOrdinal(a.Key, b.Key);
            return byKey != 0 ? byKey : OrdinalSequence.Compare(a.Values, b.Values);
        }
    }
}

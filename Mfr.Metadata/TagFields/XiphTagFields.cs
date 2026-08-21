using Mfr.Models.Tags;
using Mfr.Models.Tags.Xiph;
using Mfr.Utils;
using TagLib;
using TagLib.Ogg;

namespace Mfr.Metadata.TagFields
{
    /// <summary>
    /// Reads and field-patches the modeled Xiph comment keys on a live TagLib comment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only the keys listed here are read or written; unknown on-disk keys survive every patch by omission.
    /// </para>
    /// </remarks>
    internal static class XiphTagFields
    {
        private static readonly string[] _KnownKeys =
        [
            "TITLE",
            "ALBUM",
            "ARTIST",
            "ALBUMARTIST",
            "COMPOSER",
            "GENRE",
            "DESCRIPTION",
            "COMMENT",
            "LYRICS",
            "UNSYNCEDLYRICS",
            "COPYRIGHT",
            "GROUPING",
            "CONTENTGROUP",
            "DATE",
            "YEAR",
            "TRACKNUMBER",
            "TRACKTOTAL",
            "TOTALTRACKS",
            "DISCNUMBER",
            "DISCTOTAL",
            "TOTALDISCS",
            "BPM",
            "TEMPO",
            "CONDUCTOR",
            "MUSICBRAINZ_ARTISTID",
            "MUSICBRAINZ_ALBUMID",
            "MUSICBRAINZ_ALBUMARTISTID",
            "MUSICBRAINZ_TRACKID",
            "MUSICBRAINZ_DISCID",
            "MUSICBRAINZ_ALBUMSTATUS",
            "MUSICBRAINZ_ALBUMTYPE",
            "MUSICBRAINZ_RELEASECOUNTRY",
            "MUSICIP_PUID",
            "ASIN",
        ];

        /// <summary>
        /// Reads the file's known Xiph fields.
        /// </summary>
        /// <param name="file">Open TagLib file.</param>
        /// <returns>Block data, or <see langword="null"/> when the comment is absent or holds no known key.</returns>
        public static XiphTagData? Read(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Xiph, false) is not XiphComment live || live.IsEmpty)
                return null;

            var rows = new List<TextFieldRow>();
            foreach (var key in _KnownKeys)
            {
                var values = DelimitedText.TrimNonEmpty(live.GetField(key));
                if (values.Length == 0)
                    continue;

                rows.Add(new TextFieldRow(key, values));
            }

            if (rows.Count == 0)
                return null;

            rows.Sort(_CompareRows);
            return new XiphTagData { Fields = [.. rows] };
        }

        /// <summary>
        /// Creates or patches the file's known Xiph keys from <paramref name="original"/> → <paramref name="preview"/>.
        /// </summary>
        /// <param name="file">Open TagLib file.</param>
        /// <param name="original">Block as read from disk, or <see langword="null"/> to create.</param>
        /// <param name="preview">Block the preview wants on disk.</param>
        public static void Apply(TagLib.File file, XiphTagData? original, XiphTagData preview)
        {
            if (Equals(original, preview))
                return;

            var live = (XiphComment)file.GetTag(TagTypes.Xiph, true);
            if (original is null)
            {
                _WriteAll(live, preview);
                return;
            }

            TagFieldDiff.Apply(
                TagFieldDiff.IndexTextFields(original.Fields),
                TagFieldDiff.IndexTextFields(preview.Fields),
                valuesEqual: OrdinalSequence.AreEqual,
                remove: key => live.RemoveField(key),
                set: (key, values) => live.SetField(key, [.. values])
            );
        }

        private static void _WriteAll(XiphComment live, XiphTagData data)
        {
            foreach (var key in _KnownKeys)
                live.RemoveField(key);

            foreach (var row in data.Fields)
            {
                if (row.Values.Length == 0)
                    continue;

                live.SetField(row.Key, [.. row.Values]);
            }
        }

        private static int _CompareRows(TextFieldRow a, TextFieldRow b)
        {
            var byKey = string.CompareOrdinal(a.Key, b.Key);
            return byKey != 0 ? byKey : OrdinalSequence.Compare(a.Values, b.Values);
        }
    }
}

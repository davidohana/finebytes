using System.Collections.Immutable;
using Mfr.Models.Tags.RiffInfo;
using Mfr.Utils;
using TagLib;
using TagLib.Riff;

namespace Mfr.Metadata.TagFields
{
    /// <summary>
    /// Reads and field-patches the modeled RIFF LIST INFO chunks on a live TagLib tag.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Chunks are addressed by their standard fourCC (<see cref="RiffInfoKnownKeys.All"/>) rather than through
    /// TagLib's <see cref="InfoTag"/> façade properties, which map several common fields to non-standard ids
    /// (Album→<c>DIRC</c>, Performers→<c>ISTR</c>, Track→<c>IPRT</c>) that other taggers do not read. A chunk
    /// holds a single string, so multi-value semantics stay inside that string verbatim.
    /// </para>
    /// </remarks>
    internal static class RiffInfoTagFields
    {
        /// <summary>
        /// Reads the file's known INFO chunks.
        /// </summary>
        /// <param name="file">Open TagLib file.</param>
        /// <returns>Block data, or <see langword="null"/> when the tag is absent or holds no known chunk.</returns>
        public static RiffInfoTagData? Read(TagLib.File file)
        {
            if (file.GetTag(TagTypes.RiffInfo, false) is not InfoTag live || live.IsEmpty)
            {
                return null;
            }

            var rows = new List<RiffInfoFieldRow>();
            foreach (var key in RiffInfoKnownKeys.All)
            {
                var value = DelimitedText.JoinOrNull(live.GetValuesAsStrings(key));
                if (value is null)
                {
                    continue;
                }

                rows.Add(new RiffInfoFieldRow(key, value));
            }

            if (rows.Count == 0)
            {
                return null;
            }

            rows.Sort(RiffInfoFieldRow.Compare);
            return new RiffInfoTagData { Fields = [.. rows] };
        }

        /// <summary>
        /// Creates or patches the file's INFO chunks from <paramref name="original"/> → <paramref name="preview"/>.
        /// </summary>
        /// <param name="file">Open TagLib file.</param>
        /// <param name="original">Block as read from disk, or <see langword="null"/> to create.</param>
        /// <param name="preview">Block the preview wants on disk.</param>
        public static void Apply(TagLib.File file, RiffInfoTagData? original, RiffInfoTagData preview)
        {
            if (Equals(original, preview))
            {
                return;
            }

            var live = (InfoTag)file.GetTag(TagTypes.RiffInfo, true);
            if (original is null)
            {
                _WriteAll(live, preview);
                return;
            }

            TagFieldDiff.Apply(
                _IndexFields(original.Fields),
                _IndexFields(preview.Fields),
                valuesEqual: static (prior, value) => string.Equals(prior, value, StringComparison.Ordinal),
                remove: key => live.RemoveValue(key),
                set: (key, value) => live.SetValue(key, value)
            );
        }

        private static void _WriteAll(InfoTag live, RiffInfoTagData data)
        {
            foreach (var key in RiffInfoKnownKeys.All)
            {
                live.RemoveValue(key);
            }

            foreach (var row in data.Fields)
            {
                var value = row.Value.TrimmedOrNull();
                if (value is null)
                {
                    continue;
                }

                live.SetValue(row.Key, value);
            }
        }

        private static Dictionary<string, string> _IndexFields(ImmutableArray<RiffInfoFieldRow> fields)
        {
            var keyToValue = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var row in fields)
            {
                if (string.IsNullOrWhiteSpace(row.Value))
                {
                    continue;
                }

                keyToValue[row.Key] = row.Value;
            }

            return keyToValue;
        }
    }
}

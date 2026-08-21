using System.Collections.Immutable;
using Mfr.Models.Tags.Asf;
using Mfr.Utils;
using TagLib;
using TagLib.Asf;
using AsfTag = TagLib.Asf.Tag;

namespace Mfr.Metadata.TagFields
{
    /// <summary>
    /// Reads and field-patches ASF fields on a live TagLib tag.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Content Description fields (<see cref="AsfDescriptorNames.Title"/>, Author, Copyright) live outside the
    /// extended-descriptor enumerator and are routed through TagLib façade properties; everything else is an
    /// extended content descriptor. The tag is never cleared wholesale.
    /// </para>
    /// </remarks>
    internal static class AsfTagFields
    {
        /// <summary>
        /// Reads the file's ASF fields.
        /// </summary>
        /// <param name="file">Open TagLib file.</param>
        /// <returns>Block data, or <see langword="null"/> when the tag is absent or empty.</returns>
        public static AsfTagData? Read(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Asf, false) is not AsfTag live || live.IsEmpty)
            {
                return null;
            }

            var rows = new List<AsfDescriptorRow>();
            _AddIfPresent(rows, AsfDescriptorNames.Title, live.Title);
            _AddIfPresent(rows, AsfDescriptorNames.Author, DelimitedText.JoinOrNull(live.Performers));
            _AddIfPresent(rows, AsfDescriptorNames.Copyright, live.Copyright);

            foreach (var descriptor in live)
            {
                if (string.IsNullOrEmpty(descriptor.Name))
                {
                    continue;
                }

                // Prefer Content Description for Title/Author/Copyright when both somehow exist.
                var isDuplicateContentDescription =
                    _IsContentDescriptionName(descriptor.Name)
                    && rows.Exists(r => string.Equals(r.Name, descriptor.Name, StringComparison.Ordinal));
                if (isDuplicateContentDescription)
                {
                    continue;
                }

                rows.Add(new AsfDescriptorRow(descriptor.Name, descriptor.ToString()));
            }

            rows.Sort(_CompareRows);
            return new AsfTagData { Descriptors = [.. rows] };
        }

        /// <summary>
        /// Creates or patches the file's ASF fields from <paramref name="original"/> → <paramref name="preview"/>.
        /// </summary>
        /// <param name="file">Open TagLib file.</param>
        /// <param name="original">Block as read from disk, or <see langword="null"/> to create.</param>
        /// <param name="preview">Block the preview wants on disk.</param>
        public static void Apply(TagLib.File file, AsfTagData? original, AsfTagData preview)
        {
            if (Equals(original, preview))
            {
                return;
            }

            var live = (AsfTag)file.GetTag(TagTypes.Asf, true);
            if (original is null)
            {
                foreach (var row in preview.Descriptors)
                {
                    if (string.IsNullOrEmpty(row.Name))
                    {
                        continue;
                    }

                    _SetNamedValue(live, row.Name, row.Value);
                }

                return;
            }

            TagFieldDiff.Apply(
                _IndexDescriptors(original.Descriptors),
                _IndexDescriptors(preview.Descriptors),
                valuesEqual: static (prior, value) => string.Equals(prior, value, StringComparison.Ordinal),
                remove: name => _SetNamedValue(live, name, null),
                set: (name, value) => _SetNamedValue(live, name, value)
            );
        }

        /// <remarks>
        /// A <see langword="null"/> or empty <paramref name="value"/> clears the field.
        /// </remarks>
        private static void _SetNamedValue(AsfTag live, string name, string? value)
        {
            var text = value.TrimmedOrNull();
            switch (name)
            {
                case AsfDescriptorNames.Title:
                    live.Title = text;
                    return;
                case AsfDescriptorNames.Author:
                    live.Performers = text is null ? [] : [.. DelimitedText.Split(text)];
                    return;
                case AsfDescriptorNames.Copyright:
                    live.Copyright = text;
                    return;
                default:
                    live.RemoveDescriptors(name);
                    if (text is not null)
                    {
                        live.AddDescriptor(new ContentDescriptor(name, text));
                    }

                    return;
            }
        }

        private static void _AddIfPresent(List<AsfDescriptorRow> rows, string name, string? value)
        {
            var text = value.TrimmedOrNull();
            if (text is null)
            {
                return;
            }

            rows.Add(new AsfDescriptorRow(name, text));
        }

        private static bool _IsContentDescriptionName(string name)
        {
            return string.Equals(name, AsfDescriptorNames.Title, StringComparison.Ordinal)
                || string.Equals(name, AsfDescriptorNames.Author, StringComparison.Ordinal)
                || string.Equals(name, AsfDescriptorNames.Copyright, StringComparison.Ordinal);
        }

        private static Dictionary<string, string> _IndexDescriptors(ImmutableArray<AsfDescriptorRow> rows)
        {
            var nameToValue = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (string.IsNullOrEmpty(row.Name))
                {
                    continue;
                }

                nameToValue[row.Name] = row.Value;
            }

            return nameToValue;
        }

        private static int _CompareRows(AsfDescriptorRow a, AsfDescriptorRow b)
        {
            var byName = string.CompareOrdinal(a.Name, b.Name);
            return byName != 0 ? byName : string.CompareOrdinal(a.Value, b.Value);
        }
    }
}

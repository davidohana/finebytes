using System.Collections.Immutable;
using Mfr.Models.Tags.Apple;
using TagLib;
using AppleTag = TagLib.Mpeg4.AppleTag;

namespace Mfr.Metadata.TagFields
{
    /// <summary>
    /// Reads and field-patches the text atoms of a live Apple <c>ilst</c> tag.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rows are keyed by the four-byte box type. Binary atoms (for example the packed track/disc atoms) are not
    /// modeled and are left on disk.
    /// </para>
    /// </remarks>
    internal static class AppleTagFields
    {
        /// <summary>
        /// Reads the file's Apple text atoms.
        /// </summary>
        /// <param name="file">Open TagLib file.</param>
        /// <returns>Block data, or <see langword="null"/> when the tag is absent or has no text atom.</returns>
        public static AppleTagData? Read(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Apple, false) is not AppleTag live || live.IsEmpty)
                return null;

            var hexToBoxType = new SortedDictionary<string, ByteVector>(StringComparer.Ordinal);
            foreach (var box in live)
            {
                var typeData = box.BoxType.Data;
                if (typeData is null || typeData.Length != 4)
                    continue;

                var hex = Convert.ToHexString(typeData);
                if (hexToBoxType.ContainsKey(hex))
                    continue;

                hexToBoxType[hex] = box.BoxType;
            }

            var rows = new List<AppleAtomRow>();
            foreach (var boxType in hexToBoxType.Values)
            {
                var texts = live.GetText(boxType);
                if (texts is null || texts.Length == 0)
                    continue;

                rows.Add(new AppleAtomRow
                {
                    AtomType = ImmutableArray.Create(boxType.Data),
                    Values = [.. texts.Select(static t => t.Trim())],
                });
            }

            if (rows.Count == 0)
                return null;

            rows.Sort(_CompareRows);
            return new AppleTagData { Atoms = [.. rows] };
        }

        /// <summary>
        /// Creates or patches the file's Apple text atoms from <paramref name="original"/> → <paramref name="preview"/>.
        /// </summary>
        /// <remarks>
        /// Atoms the preview dropped are cleared by writing an empty text array, which removes the box.
        /// </remarks>
        /// <param name="file">Open TagLib file.</param>
        /// <param name="original">Block as read from disk, or <see langword="null"/> to create.</param>
        /// <param name="preview">Block the preview wants on disk.</param>
        public static void Apply(TagLib.File file, AppleTagData? original, AppleTagData preview)
        {
            if (Equals(original, preview))
                return;

            var live = (AppleTag)file.GetTag(TagTypes.Apple, true);
            if (original is null)
            {
                foreach (var row in preview.Atoms)
                    _SetAtom(live, row);

                return;
            }

            TagFieldDiff.Apply(
                _IndexAtoms(original.Atoms),
                _IndexAtoms(preview.Atoms),
                valuesEqual: static (prior, row) => prior.Equals(row),
                remove: hex => live.SetText(Convert.FromHexString(hex), []),
                set: (_, row) => _SetAtom(live, row));
        }

        private static void _SetAtom(AppleTag live, AppleAtomRow row)
        {
            live.SetText(row.AtomType.ToArray(), [.. row.Values]);
        }

        private static Dictionary<string, AppleAtomRow> _IndexAtoms(ImmutableArray<AppleAtomRow> atoms)
        {
            var hexToRow = new Dictionary<string, AppleAtomRow>(StringComparer.Ordinal);
            foreach (var row in atoms)
                hexToRow[Convert.ToHexString(row.AtomType.AsSpan())] = row;

            return hexToRow;
        }

        private static int _CompareRows(AppleAtomRow a, AppleAtomRow b)
        {
            var byType = a.AtomType.AsSpan().SequenceCompareTo(b.AtomType.AsSpan());
            return byType != 0 ? byType : TagFieldText.CompareSequence(a.Values, b.Values);
        }
    }
}

using System.Collections.Immutable;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Ape;
using Mfr.Models.Tags.Apple;
using Mfr.Models.Tags.Asf;
using Mfr.Models.Tags.Id3v1;
using Mfr.Models.Tags.Id3v2;
using Mfr.Models.Tags.RiffInfo;
using Mfr.Models.Tags.Xiph;
using TagLib;
using TagLib.Mpeg;
using TagLib.Ogg;
using TagLib.Riff;
using AppleTag = TagLib.Mpeg4.AppleTag;

namespace Mfr.Metadata
{
    /// <summary>
    /// Loads and saves structured <see cref="AudioTagOverlay"/> field blocks via TagLibSharp across supported formats.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call <see cref="Apply(string, AudioTagOverlay, AudioTagOverlay)"/> only when the rename row’s embedded-tag
    /// preview differs from its original snapshot; compare outside this type (for example in <c>CommitExecutor</c>)
    /// before calling. That Apply overload diffs Original → Preview per tag block: remove dropped types, create new
    /// blocks, and field-patch only changed modeled fields (unmodeled frames such as APIC stay unless the whole type
    /// is removed).
    /// </para>
    /// <para>
    /// Overlay blocks hold parsed fields (not binary blobs). There is no merged <c>file.Tag</c> dual write.
    /// </para>
    /// <para>
    /// A preview may not introduce a tag block the container cannot hold; see
    /// <see cref="Models.Tags.AudioTagContainerPolicy"/>.
    /// </para>
    /// </remarks>
    public static class AudioTagPersistence
    {
        /// <summary>
        /// Reads embedded audio tags into a detached <see cref="AudioTagOverlay"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The container detected during the same open is stamped on <see cref="AudioTagOverlay.ContainerFormat"/>,
        /// so later capability checks and recommended-block creates never reopen the file.
        /// </para>
        /// </remarks>
        /// <param name="absolutePath">Fully qualified filesystem path to an existing file.</param>
        /// <returns>A new overlay built from embedded tags.</returns>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="IOException">TagLib cannot open or read the file.</exception>
        /// <exception cref="CorruptFileException">Thrown by TagLib when the embedded structure is unreadable.</exception>
        /// <exception cref="UnsupportedFormatException">Thrown by TagLib when the format cannot be loaded.</exception>
        public static AudioTagOverlay Read(string absolutePath)
        {
            _ValidateExistingRegularFile(absolutePath);

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            var overlay = _ReadOverlay(file, file.TagTypesOnDisk);
            overlay.ContainerFormat = AudioTagContainerPolicy.DetectFrom(file);
            return overlay;
        }

        /// <summary>
        /// Field-patches the file’s tags from <paramref name="originalOverlay"/> to <paramref name="previewOverlay"/> and saves.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Per tag type: Original present and Preview null → <c>RemoveTags</c>; Original null and Preview present →
        /// create and write all Preview fields; both present → diff modeled fields only. Unchanged blocks are skipped.
        /// ASF is never cleared wholesale.
        /// </para>
        /// </remarks>
        /// <param name="absolutePath">Path to an existing regular file (typically the post-move destination).</param>
        /// <param name="originalOverlay">Session / pre-edit overlay (usually the row’s Original snapshot).</param>
        /// <param name="previewOverlay">Desired tag values after filters.</param>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="IOException">The file cannot be opened or saved.</exception>
        /// <exception cref="NotSupportedException">The preview introduces a tag block the container cannot hold.</exception>
        public static void Apply(
            string absolutePath,
            AudioTagOverlay originalOverlay,
            AudioTagOverlay previewOverlay)
        {
            ArgumentNullException.ThrowIfNull(originalOverlay);
            ArgumentNullException.ThrowIfNull(previewOverlay);
            _ValidateExistingRegularFile(absolutePath);

            if (previewOverlay.Equals(originalOverlay))
                return;

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            var containerFormat = AudioTagContainerPolicy.DetectFrom(file);
            _EnsureIntroducedBlocksSupported(containerFormat, originalOverlay, previewOverlay);
            _RemoveDroppedTagBlocks(file, originalOverlay, previewOverlay);
            _PatchPresentTagBlocks(file, originalOverlay, previewOverlay);
            file.Save();
        }

        /// <summary>
        /// Convenience overload: treats the current on-disk overlay as Original.
        /// </summary>
        /// <param name="absolutePath">Path to an existing regular file.</param>
        /// <param name="previewOverlay">Desired tag values.</param>
        public static void Apply(string absolutePath, AudioTagOverlay previewOverlay)
        {
            Apply(absolutePath, Read(absolutePath), previewOverlay);
        }

        /// <remarks>
        /// Only blocks the preview adds are checked. A block already on disk stays writable even when the container
        /// policy would not create it, so an odd-but-existing tag is patched rather than rejected.
        /// </remarks>
        private static void _EnsureIntroducedBlocksSupported(
            AudioContainerFormat containerFormat,
            AudioTagOverlay baselineOverlay,
            AudioTagOverlay previewOverlay)
        {
            foreach (var kind in previewOverlay.GetPresentBlockKinds())
            {
                if (baselineOverlay.HasBlock(kind))
                    continue;

                Models.Tags.AudioTagContainerPolicy.EnsureSupported(containerFormat, kind);
            }
        }

        /// <remarks>
        /// A block the file carries but the preview dropped is a tag-type removal. The whole <c>TagTypes</c> entry goes,
        /// so unmodeled content on that block (embedded art, unknown frames) is deleted with it.
        /// </remarks>
        private static void _RemoveDroppedTagBlocks(
            TagLib.File file,
            AudioTagOverlay baselineOverlay,
            AudioTagOverlay previewOverlay)
        {
            foreach (var kind in baselineOverlay.GetPresentBlockKinds())
            {
                if (previewOverlay.HasBlock(kind))
                    continue;

                file.RemoveTags(_ToTagTypes(kind));
            }
        }

        private static TagTypes _ToTagTypes(AudioTagBlockKind kind)
        {
            return kind switch
            {
                AudioTagBlockKind.Id3v1 => TagTypes.Id3v1,
                AudioTagBlockKind.Id3v2 => TagTypes.Id3v2,
                AudioTagBlockKind.Xiph => TagTypes.Xiph,
                AudioTagBlockKind.Ape => TagTypes.Ape,
                AudioTagBlockKind.Apple => TagTypes.Apple,
                AudioTagBlockKind.Asf => TagTypes.Asf,
                AudioTagBlockKind.RiffInfo => TagTypes.RiffInfo,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown audio tag block kind."),
            };
        }

        /// <summary>
        /// Removes all embedded tags TagLib associates with the file.
        /// </summary>
        /// <param name="absolutePath">Path to an existing regular file (typically after rename, at the preview destination).</param>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="IOException">The file cannot be opened or saved.</exception>
        public static void RemoveAllEmbeddedTags(string absolutePath)
        {
            _ValidateExistingRegularFile(absolutePath);

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            file.RemoveTags(TagTypes.AllTags);
            file.Save();
        }

        /// <remarks>
        /// <paramref name="presentTypes"/> decides which blocks the logical tag carries. TagLib materializes empty
        /// sibling tags while loading (an MPEG file always exposes ID3v1 and ID3v2, seeded from whichever tag was on
        /// disk), so callers reading disk state pass <c>TagTypesOnDisk</c>; callers re-reading a file they just wrote
        /// in memory pass <c>TagTypes</c>.
        /// </remarks>
        private static AudioTagOverlay _ReadOverlay(TagLib.File file, TagTypes presentTypes)
        {
            var overlay = new AudioTagOverlay();

            if (file is AudioFile && presentTypes.HasFlag(TagTypes.Id3v2))
                overlay.Id3v2 = _ReadId3v2Snapshot(file);

            if (file is AudioFile && presentTypes.HasFlag(TagTypes.Id3v1))
                overlay.Id3v1 = _ReadId3v1Snapshot(file);

            if (presentTypes.HasFlag(TagTypes.Xiph))
                overlay.Xiph = _ReadXiph(file);

            if (presentTypes.HasFlag(TagTypes.Ape))
                overlay.Ape = _ReadApe(file);

            if (presentTypes.HasFlag(TagTypes.RiffInfo))
                overlay.RiffInfo = _ReadRiffInfo(file);

            if (presentTypes.HasFlag(TagTypes.Apple))
                overlay.Apple = _ReadAppleSnapshot(file);

            if (presentTypes.HasFlag(TagTypes.Asf))
                overlay.Asf = _ReadAsfSnapshot(file);

            return overlay;
        }

        private static XiphTagData? _ReadXiph(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Xiph, false) is not XiphComment xc || xc.IsEmpty)
                return null;

            return TagBlockFieldMapper.ReadXiph(xc);
        }

        private static ApeTagData? _ReadApe(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Ape, false) is not TagLib.Ape.Tag ape || ape.IsEmpty)
                return null;

            return TagBlockFieldMapper.ReadApe(ape);
        }

        private static RiffInfoTagData? _ReadRiffInfo(TagLib.File file)
        {
            if (file.GetTag(TagTypes.RiffInfo, false) is not InfoTag info || info.IsEmpty)
                return null;

            return TagBlockFieldMapper.ReadRiffInfo(info);
        }

        private static AppleTagData? _ReadAppleSnapshot(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Apple, false) is not AppleTag apple || apple.IsEmpty)
                return null;

            return _ReadAppleTagData(apple);
        }

        private static AppleTagData? _ReadAppleTagData(AppleTag apple)
        {
            if (apple.IsEmpty)
                return null;

            var uniqueTypes = new SortedDictionary<string, ByteVector>(StringComparer.Ordinal);

            foreach (var box in apple)
            {
                var typeData = box.BoxType.Data;
                if (typeData is null || typeData.Length != 4)
                    continue;

                var hex = Convert.ToHexString(typeData);
                if (uniqueTypes.ContainsKey(hex))
                    continue;

                uniqueTypes[hex] = box.BoxType;
            }

            var rows = new List<AppleAtomRow>();

            foreach (var kvp in uniqueTypes)
            {
                var boxType = kvp.Value;
                var texts = apple.GetText(boxType);
                if (texts is null || texts.Length == 0)
                    continue;

                var vals = ImmutableArray.CreateRange(texts.Select(static s => s.Trim()));
                var atomType = ImmutableArray.Create(boxType.Data);
                rows.Add(new AppleAtomRow { AtomType = atomType, Values = vals });
            }

            rows.Sort(static (a, b) =>
            {
                var byType = a.AtomType.AsSpan().SequenceCompareTo(b.AtomType.AsSpan());
                if (byType != 0)
                    return byType;

                return _CompareImmutableStringSeq(a.Values, b.Values);
            });

            return rows.Count == 0 ? null : new AppleTagData { Atoms = [.. rows] };
        }

        private static AsfTagData? _ReadAsfSnapshot(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Asf, false) is not TagLib.Asf.Tag asf || asf.IsEmpty)
                return null;

            return _ReadAsfTagData(asf);
        }

        private static AsfTagData _ReadAsfTagData(TagLib.Asf.Tag asf)
        {
            var rows = new List<AsfDescriptorRow>();

            // Content Description Object fields are not in the extended-descriptor enumerator.
            _AddAsfIfPresent(rows, AsfDescriptorNames.Title, asf.Title);
            _AddAsfIfPresent(rows, AsfDescriptorNames.Author, _JoinPerformers(asf.Performers));
            _AddAsfIfPresent(rows, AsfDescriptorNames.Copyright, asf.Copyright);

            foreach (var d in asf)
            {
                if (string.IsNullOrEmpty(d.Name))
                    continue;

                // Prefer Content Description for Title/Author/Copyright when both somehow exist.
                if (_IsAsfContentDescriptionName(d.Name)
                    && rows.Exists(r => string.Equals(r.Name, d.Name, StringComparison.Ordinal)))
                    continue;

                rows.Add(new AsfDescriptorRow(d.Name, d.ToString()));
            }

            rows.Sort(static (a, b) =>
            {
                var byName = string.CompareOrdinal(a.Name, b.Name);
                if (byName != 0)
                    return byName;

                return string.CompareOrdinal(a.Value, b.Value);
            });

            return new AsfTagData { Descriptors = [.. rows] };
        }

        private static void _AddAsfIfPresent(List<AsfDescriptorRow> rows, string name, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            rows.Add(new AsfDescriptorRow(name, value.Trim()));
        }

        private static string? _JoinPerformers(string[] performers)
        {
            if (performers.Length == 0)
                return null;

            var parts = performers
                .Select(static p => p.Trim())
                .Where(static p => p.Length > 0)
                .ToArray();
            return parts.Length == 0 ? null : string.Join("; ", parts);
        }

        private static bool _IsAsfContentDescriptionName(string name)
        {
            return string.Equals(name, AsfDescriptorNames.Title, StringComparison.Ordinal)
                || string.Equals(name, AsfDescriptorNames.Author, StringComparison.Ordinal)
                || string.Equals(name, AsfDescriptorNames.Copyright, StringComparison.Ordinal);
        }

        private static int _CompareImmutableStringSeq(ImmutableArray<string> a, ImmutableArray<string> b)
        {
            var len = Math.Min(a.Length, b.Length);
            for (var i = 0; i < len; i++)
            {
                var c = string.CompareOrdinal(a[i], b[i]);
                if (c != 0)
                    return c;
            }

            return a.Length.CompareTo(b.Length);
        }

        private static void _PatchPresentTagBlocks(
            TagLib.File file,
            AudioTagOverlay originalOverlay,
            AudioTagOverlay previewOverlay)
        {
            if (previewOverlay.Xiph is not null)
            {
                var xiph = (XiphComment)file.GetTag(TagTypes.Xiph, true);
                TagBlockFieldPatcher.ApplyXiph(xiph, originalOverlay.Xiph, previewOverlay.Xiph);
            }

            if (previewOverlay.Ape is not null)
            {
                var ape = (TagLib.Ape.Tag)file.GetTag(TagTypes.Ape, true);
                TagBlockFieldPatcher.ApplyApe(ape, originalOverlay.Ape, previewOverlay.Ape);
            }

            if (previewOverlay.RiffInfo is not null)
            {
                var info = (InfoTag)file.GetTag(TagTypes.RiffInfo, true);
                TagBlockFieldPatcher.ApplyRiffInfo(info, originalOverlay.RiffInfo, previewOverlay.RiffInfo);
            }

            if (previewOverlay.Apple is not null)
            {
                var apple = (AppleTag)file.GetTag(TagTypes.Apple, true);
                TagBlockFieldPatcher.ApplyApple(apple, originalOverlay.Apple, previewOverlay.Apple);
            }

            if (previewOverlay.Asf is not null)
            {
                var asf = (TagLib.Asf.Tag)file.GetTag(TagTypes.Asf, true);
                TagBlockFieldPatcher.ApplyAsf(asf, originalOverlay.Asf, previewOverlay.Asf);
            }

            if (file is not AudioFile)
                return;

            if (previewOverlay.Id3v2 is not null)
            {
                var id3v2 = (TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2, true);
                TagBlockFieldPatcher.ApplyId3v2(id3v2, originalOverlay.Id3v2, previewOverlay.Id3v2);
            }

            if (previewOverlay.Id3v1 is not null)
            {
                var id3v1 = (TagLib.Id3v1.Tag)file.GetTag(TagTypes.Id3v1, true);
                TagBlockFieldPatcher.ApplyId3v1(id3v1, originalOverlay.Id3v1, previewOverlay.Id3v1);
            }
        }

        private static Id3v1TagData? _ReadId3v1Snapshot(TagLib.File file)
        {
            var tag = file.GetTag(TagTypes.Id3v1, false);
            if (tag is not TagLib.Id3v1.Tag id3v1)
                return null;

            if (_IsId3v1EffectivelyEmpty(id3v1))
                return null;

            var genreByte = id3v1.FirstGenre is null
                ? (byte)0
                : Genres.AudioToIndex(id3v1.FirstGenre);

            return new Id3v1TagData
            {
                Title = _NullIfEmpty(id3v1.Title),
                Artist = _NullIfEmpty(id3v1.FirstPerformer),
                Album = _NullIfEmpty(id3v1.Album),
                Year = id3v1.Year == 0 ? null : id3v1.Year,
                Comment = _NullIfEmpty(id3v1.Comment),
                Track = id3v1.Track == 0 ? null : (byte)Math.Min(id3v1.Track, 255u),
                Genre = genreByte,
            };
        }

        private static bool _IsId3v1EffectivelyEmpty(TagLib.Id3v1.Tag id3v1)
        {
            return string.IsNullOrWhiteSpace(id3v1.Title)
                && (id3v1.Performers.Length == 0 || string.IsNullOrWhiteSpace(id3v1.FirstPerformer))
                && string.IsNullOrWhiteSpace(id3v1.Album)
                && id3v1.Year == 0
                && string.IsNullOrWhiteSpace(id3v1.Comment)
                && id3v1.Track == 0
                && (id3v1.Genres.Length == 0 || string.IsNullOrWhiteSpace(id3v1.FirstGenre));
        }

        private static Id3v2TagData? _ReadId3v2Snapshot(TagLib.File file)
        {
            var raw = file.GetTag(TagTypes.Id3v2, false);
            if (raw is not TagLib.Id3v2.Tag id3v2)
                return null;

            return TagBlockFieldMapper.ReadId3v2(id3v2);
        }

        private static void _ValidateExistingRegularFile(string absolutePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

            if (!Path.IsPathFullyQualified(absolutePath))
                throw new ArgumentException("Path must be fully qualified.", nameof(absolutePath));

            if (Directory.Exists(absolutePath))
                throw new ArgumentException($"'{absolutePath}' is a directory.", nameof(absolutePath));

            if (!System.IO.File.Exists(absolutePath))
                throw new ArgumentException($"File does not exist: '{absolutePath}'.", nameof(absolutePath));
        }

        private static string? _NullIfEmpty(string? text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
    }
}

using System.Collections.Immutable;
using Mfr.Models.Tags;
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
    /// Call <see cref="Apply"/> only when the rename row’s embedded-tag preview differs from its original snapshot;
    /// compare outside this type (for example in <c>CommitExecutor</c>) before calling. <see cref="Apply"/>
    /// opens the file, builds an overlay snapshot from TagLib (<see cref="Read"/> normalization), compares it to the
    /// preview in full, returns without saving when they match, and otherwise writes blocks and merged TagLib-visible
    /// semantics before saving.
    /// </para>
    /// <para>
    /// Overlay blocks hold parsed fields (not binary blobs). Unmodeled frames/keys stay on disk until a whole tag type
    /// is removed or Phase E field-patch Apply replaces coarse writers.
    /// </para>
    /// </remarks>
    public static class AudioTagPersistence
    {
        /// <summary>
        /// Reads embedded audio tags into a detached <see cref="AudioTagOverlay"/>.
        /// </summary>
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
            var ambientCombinedBeforeBlockReads = CommonAudioTag.FromCombinedTag(file.Tag);
            var overlay = _ReadOverlay(file);
            _MergeAmbientCombinedTagFacadeIntoBlocks(file, overlay, absolutePath, ambientCombinedBeforeBlockReads);
            return overlay;
        }

        /// <summary>
        /// Like <see cref="NormalizeNativeBlocks"/> but returns <see langword="false"/> when TagLib cannot open the path.
        /// </summary>
        public static bool TryNormalizeNativeBlocks(AudioTagOverlay overlay, string embeddedTagSourcePath)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            if (string.IsNullOrWhiteSpace(embeddedTagSourcePath))
                return false;

            try
            {
                NormalizeNativeBlocks(overlay, embeddedTagSourcePath);
                return true;
            }
            catch (UnsupportedFormatException)
            {
                return false;
            }
            catch (CorruptFileException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <summary>
        /// Re-snaps native tag blocks using the semantic projection derived from current blocks (end-of-preview reconcile).
        /// </summary>
        public static void NormalizeNativeBlocks(AudioTagOverlay overlay, string embeddedTagSourcePath)
        {
            ArgumentNullException.ThrowIfNull(overlay);
            _ValidateExistingRegularFile(embeddedTagSourcePath);
            var merged = CommonAudioTag.FromOverlay(overlay);
            MergeSemanticOntoNativeBlocks(overlay, merged, embeddedTagSourcePath);
        }

        /// <summary>
        /// Like <see cref="MergeSemanticOntoNativeBlocks"/> but ignores TagLib failures.
        /// </summary>
        public static bool TryMergeSemanticOntoNativeBlocks(AudioTagOverlay overlay, CommonAudioTag merged, string embeddedTagSourcePath)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            try
            {
                MergeSemanticOntoNativeBlocks(overlay, merged, embeddedTagSourcePath);
                return true;
            }
            catch (UnsupportedFormatException)
            {
                return false;
            }
            catch (CorruptFileException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Merges a semantic projection into structured per–<c>TagTypes</c> field blocks on <paramref name="overlay"/>.
        /// </summary>
        /// <remarks>
        /// When <paramref name="embeddedTagSourcePath"/> is missing or the file cannot be opened, empty-overlay façade
        /// materialization is skipped; present blocks still update in memory with empty→absent pruning.
        /// </remarks>
        public static void MergeSemanticOntoNativeBlocks(
            AudioTagOverlay overlay,
            CommonAudioTag merged,
            string? embeddedTagSourcePath)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            var hadAnyNativeBlockBeforeSemanticMerge =
                overlay.Id3v1 is not null
                || overlay.Id3v2 is not null
                || overlay.Xiph is not null
                || overlay.Ape is not null
                || overlay.RiffInfo is not null
                || overlay.Apple is not null
                || overlay.Asf is not null;

            TagBlockFieldMapper.MergeSemanticIntoBlocks(overlay, merged);

            if (hadAnyNativeBlockBeforeSemanticMerge || !merged.ContainsRenderableSemantics())
                return;

            if (string.IsNullOrWhiteSpace(embeddedTagSourcePath)
                || !Path.IsPathFullyQualified(embeddedTagSourcePath)
                || !System.IO.File.Exists(embeddedTagSourcePath)
                || Directory.Exists(embeddedTagSourcePath))
                return;

            TagLib.File? file;
            try
            {
                file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(embeddedTagSourcePath));
            }
            catch (UnsupportedFormatException)
            {
                return;
            }
            catch (CorruptFileException)
            {
                return;
            }
            catch (IOException)
            {
                return;
            }

            try
            {
                TagBlockFieldMapper.WriteCommonToTag(file.Tag, merged);
                try
                {
                    var refreshed = _ReadOverlay(file);
                    overlay.Id3v1 = refreshed.Id3v1;
                    overlay.Id3v2 = refreshed.Id3v2;
                    overlay.Xiph = refreshed.Xiph;
                    overlay.Ape = refreshed.Ape;
                    overlay.RiffInfo = refreshed.RiffInfo;
                    overlay.Apple = refreshed.Apple;
                    overlay.Asf = refreshed.Asf;
                    TagBlockFieldMapper.MergeSemanticIntoBlocks(overlay, merged);
                }
                catch (CorruptFileException)
                {
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
            finally
            {
                file.Dispose();
            }
        }

        /// <summary>
        /// Loads the file’s normalized tag overlay via TagLib and, when <paramref name="previewOverlay"/> differs from that overlay, assigns modeled fields from <paramref name="previewOverlay"/> to TagLib tags and saves.
        /// </summary>
        /// <param name="absolutePath">Path to an existing regular file (typically the post-move destination).</param>
        /// <param name="previewOverlay">Desired tag values.</param>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="IOException">The file cannot be opened or saved.</exception>
        public static void Apply(string absolutePath, AudioTagOverlay previewOverlay)
        {
            _ValidateExistingRegularFile(absolutePath);

            var baselineOverlay = Read(absolutePath);
            if (previewOverlay.Equals(baselineOverlay))
                return;

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            _ApplyNativeTagBlocks(file, previewOverlay);

            if (file is AudioFile)
                _ApplyToMpeg(file, previewOverlay);
            else if (!_HasAnyNativeBlock(previewOverlay))
                TagBlockFieldMapper.WriteCommonToTag(file.Tag, CommonAudioTag.FromOverlay(previewOverlay));

            file.Save();
        }

        private static bool _HasAnyNativeBlock(AudioTagOverlay overlay)
        {
            return overlay.Id3v1 is not null
                || overlay.Id3v2 is not null
                || overlay.Xiph is not null
                || overlay.Ape is not null
                || overlay.RiffInfo is not null
                || overlay.Apple is not null
                || overlay.Asf is not null;
        }

        /// <summary>
        /// Removes all embedded tag blobs TagLib associates with the file.
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

        private static AudioTagOverlay _ReadOverlay(TagLib.File file)
        {
            Id3v1TagData? id3v1 = null;
            Id3v2TagData? id3v2 = null;
            if (file is AudioFile)
            {
                id3v2 = _ReadId3v2Snapshot(file);
                id3v1 = _ReadId3v1Snapshot(file);
            }

            return new AudioTagOverlay
            {
                Id3v1 = id3v1,
                Id3v2 = id3v2,
                Xiph = _ReadXiph(file),
                Ape = _ReadApe(file),
                RiffInfo = _ReadRiffInfo(file),
                Apple = _ReadAppleSnapshot(file),
                Asf = _ReadAsfSnapshot(file),
            };
        }

        private static void _MergeAmbientCombinedTagFacadeIntoBlocks(
            TagLib.File file,
            AudioTagOverlay overlay,
            string absolutePath,
            CommonAudioTag ambientCombinedBeforeBlockReads)
        {
            var ambient = ambientCombinedBeforeBlockReads;
            if (!ambient.ContainsRenderableSemantics())
                return;

            var projected = CommonAudioTag.FromOverlay(overlay);
            var merged = projected.WithMissingFieldsFilledFrom(ambient);
            if (merged.Equals(projected))
                return;

            MergeSemanticOntoNativeBlocks(overlay, merged, absolutePath);
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
            foreach (var d in asf)
                rows.Add(new AsfDescriptorRow(d.Name, d.ToString()));

            var hasWmTitle = rows.Exists(static r =>
                string.Equals(r.Name, "WM/Title", StringComparison.Ordinal));

            var titleFromFaçade = string.IsNullOrWhiteSpace(asf.Title) ? null : asf.Title.Trim();
            if (!hasWmTitle && titleFromFaçade is not null)
                rows.Add(new AsfDescriptorRow("WM/Title", titleFromFaçade));

            rows.Sort(static (a, b) =>
            {
                var byName = string.CompareOrdinal(a.Name, b.Name);
                if (byName != 0)
                    return byName;

                return string.CompareOrdinal(a.Value, b.Value);
            });

            return new AsfTagData { Descriptors = [.. rows] };
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

        private static void _ApplyNativeTagBlocks(TagLib.File file, AudioTagOverlay overlay)
        {
            if (overlay.Xiph is not null && file.GetTag(TagTypes.Xiph, true) is XiphComment xiph)
                TagBlockFieldMapper.WriteXiph(xiph, overlay.Xiph);

            if (overlay.Ape is not null && file.GetTag(TagTypes.Ape, true) is TagLib.Ape.Tag ape)
                TagBlockFieldMapper.WriteApe(ape, overlay.Ape);

            if (overlay.RiffInfo is not null && file.GetTag(TagTypes.RiffInfo, true) is InfoTag info)
                TagBlockFieldMapper.WriteRiffInfo(info, overlay.RiffInfo);

            if (overlay.Apple is not null && file.GetTag(TagTypes.Apple, true) is AppleTag apple)
                TagBlockFieldMapper.WriteApple(apple, overlay.Apple);

            if (overlay.Asf is not null && file.GetTag(TagTypes.Asf, true) is TagLib.Asf.Tag asf)
                TagBlockFieldMapper.WriteAsf(asf, overlay.Asf);
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

        private static void _ApplyToMpeg(TagLib.File file, AudioTagOverlay overlay)
        {
            if (overlay.Id3v2 is not null)
            {
                var id3v2 = (TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2, true);
                TagBlockFieldMapper.WriteId3v2(id3v2, overlay.Id3v2);
            }

            if (overlay.Id3v1 is not null)
            {
                var id3v1 = (TagLib.Id3v1.Tag)file.GetTag(TagTypes.Id3v1, true);
                TagBlockFieldMapper.WriteId3v1(id3v1, overlay.Id3v1);
            }
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

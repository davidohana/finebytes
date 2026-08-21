using Mfr.Metadata.TagFields;
using Mfr.Models.Tags;
using Mfr.Utils;
using TagLib;
using TagLib.Mpeg;

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
    /// Which fields a block models, and how they are read and patched, lives in the matching
    /// <c>*TagFields</c> class (for example <see cref="Id3v2TagFields"/>); this type only decides which blocks
    /// to visit.
    /// </para>
    /// <para>
    /// A preview may not introduce a tag block the container cannot hold; see
    /// <see cref="AudioTagContainerPolicy"/>.
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
            absolutePath.RequireExistingRegularFile();

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            return ReadFrom(file);
        }

        /// <summary>
        /// Maps embedded tags from an already-open TagLib file.
        /// </summary>
        /// <param name="file">Open TagLib file.</param>
        /// <returns>A new overlay built from embedded tags, with <see cref="AudioTagOverlay.ContainerFormat"/> stamped.</returns>
        internal static AudioTagOverlay ReadFrom(TagLib.File file)
        {
            ArgumentNullException.ThrowIfNull(file);

            var overlay = _ReadOverlay(file, file.TagTypesOnDisk);
            overlay.ContainerFormat = AudioTagContainerDetector.DetectFrom(file);
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
        public static void Apply(string absolutePath, AudioTagOverlay originalOverlay, AudioTagOverlay previewOverlay)
        {
            ArgumentNullException.ThrowIfNull(originalOverlay);
            ArgumentNullException.ThrowIfNull(previewOverlay);
            absolutePath.RequireExistingRegularFile();

            if (previewOverlay.Equals(originalOverlay))
            {
                return;
            }

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            var containerFormat = AudioTagContainerDetector.DetectFrom(file);
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

        /// <summary>
        /// Removes all embedded tags TagLib associates with the file (<c>RemoveTags(AllTags)</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Used when <c>TagRemover</c> ran with <c>options.all</c>. Unlike selective block removal, this also deletes
        /// TagLib types outside the modeled overlay (<c>MovieId</c>, <c>DivX</c>, <c>FlacMetadata</c>, <c>TiffIFD</c>,
        /// <c>XMP</c>, <c>JpegComment</c>, <c>GifComment</c>, <c>Png</c>, <c>IPTCIIM</c>, <c>AudibleMetadata</c>,
        /// <c>Matroska</c>), not only <c>Id3v1</c>/<c>Id3v2</c>/<c>Xiph</c>/<c>Ape</c>/<c>Apple</c>/<c>Asf</c>/<c>RiffInfo</c>.
        /// </para>
        /// </remarks>
        /// <param name="absolutePath">Path to an existing regular file (typically after rename, at the preview destination).</param>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="IOException">The file cannot be opened or saved.</exception>
        public static void RemoveAllEmbeddedTags(string absolutePath)
        {
            absolutePath.RequireExistingRegularFile();

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            file.RemoveTags(TagTypes.AllTags);
            file.Save();
        }

        /// <remarks>
        /// Only blocks the preview adds are checked. A block already on disk stays writable even when the container
        /// policy would not create it, so an odd-but-existing tag is patched rather than rejected.
        /// </remarks>
        private static void _EnsureIntroducedBlocksSupported(
            AudioContainerFormat containerFormat,
            AudioTagOverlay baselineOverlay,
            AudioTagOverlay previewOverlay
        )
        {
            foreach (var kind in previewOverlay.GetPresentBlockKinds())
            {
                if (baselineOverlay.HasBlock(kind))
                {
                    continue;
                }

                AudioTagContainerPolicy.EnsureSupported(containerFormat, kind);
            }
        }

        /// <remarks>
        /// A block the file carries but the preview dropped is a tag-type removal. The whole <c>TagTypes</c> entry goes,
        /// so unmodeled content on that block (embedded art, unknown frames) is deleted with it.
        /// </remarks>
        private static void _RemoveDroppedTagBlocks(
            TagLib.File file,
            AudioTagOverlay baselineOverlay,
            AudioTagOverlay previewOverlay
        )
        {
            foreach (var kind in baselineOverlay.GetPresentBlockKinds())
            {
                if (previewOverlay.HasBlock(kind))
                {
                    continue;
                }

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
            {
                overlay.Id3v2 = Id3v2TagFields.Read(file);
            }

            if (file is AudioFile && presentTypes.HasFlag(TagTypes.Id3v1))
            {
                overlay.Id3v1 = Id3v1TagFields.Read(file);
            }

            if (presentTypes.HasFlag(TagTypes.Xiph))
            {
                overlay.Xiph = XiphTagFields.Read(file);
            }

            if (presentTypes.HasFlag(TagTypes.Ape))
            {
                overlay.Ape = ApeTagFields.Read(file);
            }

            if (presentTypes.HasFlag(TagTypes.RiffInfo))
            {
                overlay.RiffInfo = RiffInfoTagFields.Read(file);
            }

            if (presentTypes.HasFlag(TagTypes.Apple))
            {
                overlay.Apple = AppleTagFields.Read(file);
            }

            if (presentTypes.HasFlag(TagTypes.Asf))
            {
                overlay.Asf = AsfTagFields.Read(file);
            }

            return overlay;
        }

        /// <remarks>
        /// ID3v1 and ID3v2 are only reachable on MPEG audio files; TagLib refuses to create them elsewhere.
        /// </remarks>
        private static void _PatchPresentTagBlocks(
            TagLib.File file,
            AudioTagOverlay originalOverlay,
            AudioTagOverlay previewOverlay
        )
        {
            if (previewOverlay.Xiph is not null)
            {
                XiphTagFields.Apply(file, originalOverlay.Xiph, previewOverlay.Xiph);
            }

            if (previewOverlay.Ape is not null)
            {
                ApeTagFields.Apply(file, originalOverlay.Ape, previewOverlay.Ape);
            }

            if (previewOverlay.RiffInfo is not null)
            {
                RiffInfoTagFields.Apply(file, originalOverlay.RiffInfo, previewOverlay.RiffInfo);
            }

            if (previewOverlay.Apple is not null)
            {
                AppleTagFields.Apply(file, originalOverlay.Apple, previewOverlay.Apple);
            }

            if (previewOverlay.Asf is not null)
            {
                AsfTagFields.Apply(file, originalOverlay.Asf, previewOverlay.Asf);
            }

            if (file is not AudioFile)
            {
                return;
            }

            if (previewOverlay.Id3v2 is not null)
            {
                Id3v2TagFields.Apply(file, originalOverlay.Id3v2, previewOverlay.Id3v2);
            }

            if (previewOverlay.Id3v1 is not null)
            {
                Id3v1TagFields.Apply(file, originalOverlay.Id3v1, previewOverlay.Id3v1);
            }
        }
    }
}

using Mfr.Models.Media;
using Mfr.Models.Tags;

namespace Mfr.Models.Rename
{
    /// <summary>
    /// Lightweight file metadata used during rename planning and execution.
    /// </summary>
    /// <param name="renameListIndex">Zero-based index across all scanned files.</param>
    /// <param name="inFolderIndex">Zero-based index within the parent folder.</param>
    /// <param name="directoryPath">Absolute path to the parent directory.</param>
    /// <param name="fileName">File name without extension.</param>
    /// <param name="extension">File extension without the leading dot.</param>
    /// <param name="attributes">Filesystem attributes for this entry.</param>
    /// <param name="creationTime">File creation time (local), from scan or synthetic tests.</param>
    /// <param name="lastWriteTime">Last write time (local), from scan or synthetic tests.</param>
    /// <param name="lastAccessTime">Last access time (local), from scan or synthetic tests.</param>
    /// <param name="fileSize">File size in bytes; 0 for directories or when not applicable.</param>
    /// <param name="renameListTotalCount">Rename-list length when snapshot was taken; used by <c>&lt;counter&gt;</c> automatic padding.</param>
    /// <param name="renameListFolderSiblingCount">Rename-list items sharing this directory; used when counter resets per folder.</param>
    public sealed class FileMeta(
        int renameListIndex,
        int inFolderIndex,
        string directoryPath,
        string fileName,
        string extension,
        FileAttributes attributes = FileAttributes.Normal,
        DateTime creationTime = default,
        DateTime lastWriteTime = default,
        DateTime lastAccessTime = default,
        long fileSize = 0,
        int renameListTotalCount = 0,
        int renameListFolderSiblingCount = 0
    )
    {
        /// <summary>
        /// Gets or sets the zero-based index across all scanned files.
        /// </summary>
        public int RenameListIndex { get; set; } = renameListIndex;

        /// <summary>
        /// Gets or sets the zero-based index within the parent folder.
        /// </summary>
        public int InFolderIndex { get; set; } = inFolderIndex;

        /// <summary>
        /// Gets the absolute file path.
        /// </summary>
        public string FullPath => Path.Combine(DirectoryPath, FullFileName);

        /// <summary>
        /// Gets the file name including extension (<see cref="FileName"/>, a separator dot when needed, and <see cref="Extension"/>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Uses <see cref="ComposeFullFileName"/> so a leading-dot <see cref="Extension"/> does not insert an extra
        /// separator (<c>song</c> + <c>.mp.3</c> → <c>song.mp.3</c>, not <c>song..mp.3</c>).
        /// </para>
        /// </remarks>
        public string FullFileName => ComposeFullFileName(FileName, Extension);

        /// <summary>
        /// Gets or sets the absolute parent directory path.
        /// </summary>
        public string DirectoryPath { get; set; } = directoryPath;

        /// <summary>
        /// Gets or sets the file name without extension.
        /// </summary>
        public string FileName { get; set; } = fileName;

        /// <summary>
        /// Gets or sets the file extension without the leading dot.
        /// </summary>
        public string Extension { get; set; } = extension;

        /// <summary>
        /// Composes a full file name from prefix and extension fields.
        /// </summary>
        /// <param name="fileName">File name without extension (may contain dots).</param>
        /// <param name="extension">Extension; empty, without a leading dot, or with a leading dot.</param>
        /// <returns>
        /// <paramref name="fileName"/> alone when <paramref name="extension"/> is empty; otherwise
        /// <paramref name="fileName"/> plus <paramref name="extension"/> when it already starts with
        /// <c>.</c>, else <paramref name="fileName"/> + <c>.</c> + <paramref name="extension"/>.
        /// </returns>
        public static string ComposeFullFileName(string fileName, string extension)
        {
            ArgumentNullException.ThrowIfNull(fileName);
            ArgumentNullException.ThrowIfNull(extension);

            if (extension.Length == 0)
            {
                return fileName;
            }

            if (extension[0] == '.')
            {
                return fileName + extension;
            }

            return fileName + "." + extension;
        }

        /// <summary>
        /// Rewrites <see cref="FileName"/> and <see cref="Extension"/> so they Path-round-trip the composed full name.
        /// </summary>
        /// <remarks>
        /// <para>
        /// After filter / override / undo OldValue writes, multi-dot or leading-dot Extension values (and empty
        /// Extension with dots in FileName) are folded via <c>Path.GetFileNameWithoutExtension</c> so
        /// preview columns match post-reload disk shape. Skips rewrite when the composed name ends with a space
        /// or period so Windows illegal-name detection still sees the attempted name.
        /// </para>
        /// </remarks>
        public void CanonicalizeFileNameAndExtension()
        {
            var fullFileName = ComposeFullFileName(FileName, Extension);
            if (fullFileName.Length > 0)
            {
                var last = fullFileName[^1];
                if (last is ' ' or '.')
                {
                    return;
                }
            }

            FileName = Path.GetFileNameWithoutExtension(fullFileName);
            Extension = ExtensionWithoutDot(fullFileName);
        }

        /// <summary>
        /// Returns the extension of <paramref name="path"/> without a leading dot (empty when none).
        /// </summary>
        /// <param name="path">File path or file name.</param>
        /// <returns>Extension text suitable for <see cref="Extension"/> storage.</returns>
        public static string ExtensionWithoutDot(string path)
        {
            var extension = Path.GetExtension(path);
            return extension.Length == 0 ? string.Empty : extension[1..];
        }

        /// <summary>
        /// Gets or sets filesystem attributes (preview may differ from scan-time original).
        /// </summary>
        public FileAttributes Attributes { get; set; } = attributes;

        /// <summary>
        /// Gets or sets the creation time (local) for preview/commit.
        /// </summary>
        public DateTime CreationTime { get; set; } = creationTime;

        /// <summary>
        /// Gets or sets the last write time (local) for preview/commit.
        /// </summary>
        public DateTime LastWriteTime { get; set; } = lastWriteTime;

        /// <summary>
        /// Gets or sets the last access time (local) for preview/commit.
        /// </summary>
        public DateTime LastAccessTime { get; set; } = lastAccessTime;

        /// <summary>
        /// Gets the file size in bytes. Zero for directories or when not yet populated.
        /// </summary>
        public long FileSize { get; init; } = fileSize;

        /// <summary>
        /// Gets or sets the total number of items in the rename list when this snapshot applies.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Zero means unset (tests or callers that did not populate context). Preview assigns this from
        /// the rename list before filters run.
        /// </para>
        /// </remarks>
        public int RenameListTotalCount { get; set; } = renameListTotalCount;

        /// <summary>
        /// Gets or sets how many rename-list items share <see cref="DirectoryPath"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Zero means unset. Preview assigns this from the rename list before filters run.
        /// </para>
        /// </remarks>
        public int RenameListFolderSiblingCount { get; set; } = renameListFolderSiblingCount;

        /// <summary>
        /// Gets or sets preview/commit overlay for embedded audio tags (canonical fields mirrored from TagLib read/write).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Not populated at ingest. Hosted rename lists pass a TagLib-backed reader to the <see cref="RenameItem"/> constructor for on-disk hydration on first <c>audio-*</c> formatter use; overlays clear after commit so later previews reload.
        /// Omitting that reader skips disk-backed hydration and leaves in-memory overlays unchanged until a reader is supplied. Directories combined with embedded-audio formatter tokens yield preview errors rather than silently empty overlays.
        /// </para>
        /// </remarks>
        public AudioTagOverlay AudioTagOverlay { get; set; } = new();

        /// <summary>
        /// Gets or sets the lazy TagLib media-properties read cache (duration, bitrate, video frame size, optional MPEG header).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Read-only; never written on commit. <see langword="null"/> until first <c>media-*</c> or <c>mp3-*</c>
        /// formatter load. When present, <see cref="MediaProperties.Mp3"/> is set only if an MPEG audio header exists.
        /// </para>
        /// </remarks>
        public MediaProperties? Media { get; set; }

        /// <summary>
        /// Gets or sets the lazy MetadataExtractor image-properties read cache (dims, bit depth, DPI, frames).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Read-only; never written on commit. <see langword="null"/> until first <c>image-*</c> or <c>exif-*</c>
        /// formatter load (one MetadataExtractor open fills <see cref="Image"/> and <see cref="Exif"/>).
        /// Separate from TagLib <see cref="Media"/> stream facts (including video frame size); values may differ.
        /// </para>
        /// </remarks>
        public ImageProperties? Image { get; set; }

        /// <summary>
        /// Gets or sets the lazy MetadataExtractor EXIF read cache (camera fields, DateTaken, extended tags).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Read-only; never written on commit. <see langword="null"/> until first <c>image-*</c> or <c>exif-*</c>
        /// formatter load. Mapped rasters with no EXIF store an empty snapshot, not <see langword="null"/>.
        /// </para>
        /// </remarks>
        public ExifData? Exif { get; set; }

        /// <summary>
        /// Gets or sets the lazy PdfPig PDF Info + page-count read cache.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Read-only; never written on commit. <see langword="null"/> until first <c>pdf-*</c> formatter load.
        /// Successful opens with missing Info fields store an empty/partial snapshot, not <see langword="null"/>.
        /// </para>
        /// </remarks>
        public PdfDocumentInfo? Pdf { get; set; }

        /// <summary>
        /// Gets or sets the lazy VersOne.Epub Dublin Core document Info read cache.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Read-only; never written on commit. <see langword="null"/> until first <c>epub-*</c> formatter load.
        /// Successful opens with missing DC fields store an empty/partial snapshot, not <see langword="null"/>.
        /// </para>
        /// </remarks>
        public EpubDocumentInfo? Epub { get; set; }

        /// <summary>
        /// Gets or sets the lazy OpenXml Office PackageProperties document Info read cache.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Read-only; never written on commit. <see langword="null"/> until first <c>office-*</c> formatter load.
        /// Successful opens with missing PackageProperties fields store an empty/partial snapshot, not
        /// <see langword="null"/>.
        /// </para>
        /// </remarks>
        public OfficeDocumentInfo? Office { get; set; }

        /// <summary>
        /// Creates a detached copy of this metadata instance.
        /// </summary>
        /// <returns>A cloned metadata instance.</returns>
        public FileMeta Clone()
        {
            return new FileMeta(
                renameListIndex: RenameListIndex,
                inFolderIndex: InFolderIndex,
                directoryPath: DirectoryPath,
                fileName: FileName,
                extension: Extension,
                attributes: Attributes,
                creationTime: CreationTime,
                lastWriteTime: LastWriteTime,
                lastAccessTime: LastAccessTime,
                fileSize: FileSize,
                renameListTotalCount: RenameListTotalCount,
                renameListFolderSiblingCount: RenameListFolderSiblingCount
            )
            {
                AudioTagOverlay = AudioTagOverlay.Clone(),
                Media = Media,
                Image = Image,
                Exif = Exif,
                Pdf = Pdf,
                Epub = Epub,
                Office = Office,
            };
        }
    }
}

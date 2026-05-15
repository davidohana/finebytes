using Mfr.Models.Tags;

namespace Mfr.Models
{
    /// <summary>
    /// Lightweight file metadata used during rename planning and execution.
    /// </summary>
    /// <param name="renameListIndex">Zero-based index across all scanned files.</param>
    /// <param name="inFolderIndex">Zero-based index within the parent folder.</param>
    /// <param name="directoryPath">Absolute path to the parent directory.</param>
    /// <param name="prefix">File name without extension.</param>
    /// <param name="extension">File extension including the leading dot.</param>
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
        string prefix,
        string extension,
        FileAttributes attributes = FileAttributes.Normal,
        DateTime creationTime = default,
        DateTime lastWriteTime = default,
        DateTime lastAccessTime = default,
        long fileSize = 0,
        int renameListTotalCount = 0,
        int renameListFolderSiblingCount = 0)
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
        public string FullPath => Path.Combine(DirectoryPath, Prefix + Extension);

        /// <summary>
        /// Gets or sets the absolute parent directory path.
        /// </summary>
        public string DirectoryPath { get; set; } = directoryPath;

        /// <summary>
        /// Gets or sets the file name without extension.
        /// </summary>
        public string Prefix { get; set; } = prefix;

        /// <summary>
        /// Gets or sets the file extension including the leading dot.
        /// </summary>
        public string Extension { get; set; } = extension;

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
        /// Creates a detached copy of this metadata instance.
        /// </summary>
        /// <returns>A cloned metadata instance.</returns>
        public FileMeta Clone()
        {
            return new FileMeta(
                renameListIndex: RenameListIndex,
                inFolderIndex: InFolderIndex,
                directoryPath: DirectoryPath,
                prefix: Prefix,
                extension: Extension,
                attributes: Attributes,
                creationTime: CreationTime,
                lastWriteTime: LastWriteTime,
                lastAccessTime: LastAccessTime,
                fileSize: FileSize,
                renameListTotalCount: RenameListTotalCount,
                renameListFolderSiblingCount: RenameListFolderSiblingCount)
            {
                AudioTagOverlay = AudioTagOverlay.Clone(),
            };
        }
    }
}

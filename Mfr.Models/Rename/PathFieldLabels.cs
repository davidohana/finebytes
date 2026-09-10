namespace Mfr.Models.Rename
{
    /// <summary>
    /// Shared user-facing labels for file-name and path fields (Apply-To, Rename List, Format Editor).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ParentDirectory"/> is the absolute containing directory; <see cref="ParentFolder"/> is the
    /// immediate ancestor folder <em>segment</em> (level 1). Keep those words distinct.
    /// </para>
    /// </remarks>
    public static class PathFieldLabels
    {
        /// <summary>File name without extension (Apply-To File Prefix target).</summary>
        public const string FileName = "File Name";

        /// <summary>File extension without a leading dot.</summary>
        public const string FileExtension = "File Extension";

        /// <summary>Full file name including extension.</summary>
        public const string FullFileName = "Full File Name";

        /// <summary>Absolute full path.</summary>
        public const string FullPath = "Full Path";

        /// <summary>Character length of the absolute full path.</summary>
        public const string FullPathLength = "Full Path Length";

        /// <summary>Absolute path of the containing directory.</summary>
        public const string ParentDirectory = "Parent Directory";

        /// <summary>Immediate parent folder segment name (ancestor level 1).</summary>
        public const string ParentFolder = "Parent Folder";

        /// <summary>Whether the item is a file or a folder.</summary>
        public const string FileOrFolder = "File/Folder";

        /// <summary>Character length of the full file name including extension.</summary>
        public const string FileNameLength = "File Name Length";

        /// <summary>First digit run in the full file name.</summary>
        public const string FileNameNumericValue = "File Name Numeric Value";
    }
}

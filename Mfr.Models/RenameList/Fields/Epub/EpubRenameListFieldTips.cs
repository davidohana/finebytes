namespace Mfr.Models.RenameList.Fields.Epub
{
    /// <summary>
    /// Clarifying tooltips for EPUB Document Rename List columns.
    /// </summary>
    public static class EpubRenameListFieldTips
    {
        /// <summary>EPUB dc:creator column (author vs PDF creating-app).</summary>
        public const string Creator =
            "Dublin Core creator / author from the EPUB package (person or organization), not a creating application.";

        /// <summary>EPUB identifier column (unique-id preference).</summary>
        public const string Identifier =
            "Package unique-identifier when set; otherwise the first non-blank dc:identifier.";

        /// <summary>EPUB dc:description column.</summary>
        public const string Description = "First non-blank dc:description from the EPUB package.";
    }
}

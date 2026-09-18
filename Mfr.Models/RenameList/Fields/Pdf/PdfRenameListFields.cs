namespace Mfr.Models.RenameList.Fields.Pdf
{
    /// <summary>
    /// All PDF Document Rename List fields (read-only originals).
    /// </summary>
    public static class PdfRenameListFields
    {
        /// <summary>
        /// PDF property group id.
        /// </summary>
        public const string Group = "Pdf";

        /// <summary>
        /// User-visible group label in the field shuttle groups list.
        /// </summary>
        public const string GroupLabel = "PDF Document";

        /// <summary>
        /// Property keys within <see cref="Group"/>.
        /// </summary>
        public static class Key
        {
            /// <summary>Info Title.</summary>
            public const string Title = "Title";

            /// <summary>Info Author.</summary>
            public const string Author = "Author";

            /// <summary>Info Subject.</summary>
            public const string Subject = "Subject";

            /// <summary>Info Keywords.</summary>
            public const string Keywords = "Keywords";

            /// <summary>Info Creator (creating app).</summary>
            public const string Creator = "Creator";

            /// <summary>Info Producer.</summary>
            public const string Producer = "Producer";

            /// <summary>Info CreationDate.</summary>
            public const string Created = "Created";

            /// <summary>Info ModDate.</summary>
            public const string Modified = "Modified";

            /// <summary>Page count.</summary>
            public const string PageCount = "PageCount";
        }

        /// <summary>
        /// PDF Document group fields in catalog order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            new PdfPropertyRenameListField(Key.Title, "Title", PdfRenameListProperty.Title, defaultWidth: 160),
            new PdfPropertyRenameListField(
                Key.Author,
                "Author",
                PdfRenameListProperty.Author,
                defaultWidth: 120,
                tip: PdfRenameListFieldTips.Author
            ),
            new PdfPropertyRenameListField(Key.Subject, "Subject", PdfRenameListProperty.Subject, defaultWidth: 160),
            new PdfPropertyRenameListField(Key.Keywords, "Keywords", PdfRenameListProperty.Keywords, defaultWidth: 160),
            new PdfPropertyRenameListField(
                Key.Creator,
                "Creator",
                PdfRenameListProperty.Creator,
                defaultWidth: 120,
                tip: PdfRenameListFieldTips.Creator
            ),
            new PdfPropertyRenameListField(
                Key.Producer,
                "Producer",
                PdfRenameListProperty.Producer,
                defaultWidth: 120,
                tip: PdfRenameListFieldTips.Producer
            ),
            new PdfPropertyRenameListField(Key.Created, "Created", PdfRenameListProperty.Created, defaultWidth: 140),
            new PdfPropertyRenameListField(Key.Modified, "Modified", PdfRenameListProperty.Modified, defaultWidth: 140),
            new PdfPropertyRenameListField(Key.PageCount, "Page Count", PdfRenameListProperty.PageCount),
        ];
    }
}

namespace Mfr.Models.RenameList.Fields.Epub
{
    /// <summary>
    /// All EPUB Document Rename List fields (read-only originals).
    /// </summary>
    public static class EpubRenameListFields
    {
        /// <summary>
        /// EPUB property group id.
        /// </summary>
        public const string Group = "Epub";

        /// <summary>
        /// User-visible group label in the field shuttle groups list.
        /// </summary>
        public const string GroupLabel = "EPUB Document";

        /// <summary>
        /// Property keys within <see cref="Group"/>.
        /// </summary>
        public static class Key
        {
            /// <summary>dc:title.</summary>
            public const string Title = "Title";

            /// <summary>dc:creator (author / organization).</summary>
            public const string Creator = "Creator";

            /// <summary>dc:publisher.</summary>
            public const string Publisher = "Publisher";

            /// <summary>dc:language.</summary>
            public const string Language = "Language";

            /// <summary>dc:date (literal string).</summary>
            public const string Date = "Date";

            /// <summary>Preferred dc:identifier.</summary>
            public const string Identifier = "Identifier";

            /// <summary>dc:subject.</summary>
            public const string Subject = "Subject";

            /// <summary>dc:description.</summary>
            public const string Description = "Description";
        }

        /// <summary>
        /// EPUB Document group fields in catalog order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            new EpubPropertyRenameListField(Key.Title, "Title", EpubDocumentField.Title, defaultWidth: 160),
            new EpubPropertyRenameListField(
                Key.Creator,
                "Creator",
                EpubDocumentField.Creator,
                defaultWidth: 120,
                tip: EpubRenameListFieldTips.Creator
            ),
            new EpubPropertyRenameListField(Key.Publisher, "Publisher", EpubDocumentField.Publisher, defaultWidth: 120),
            new EpubPropertyRenameListField(Key.Language, "Language", EpubDocumentField.Language, defaultWidth: 80),
            new EpubPropertyRenameListField(Key.Date, "Date", EpubDocumentField.Date, defaultWidth: 100),
            new EpubPropertyRenameListField(
                Key.Identifier,
                "Identifier",
                EpubDocumentField.Identifier,
                defaultWidth: 160,
                tip: EpubRenameListFieldTips.Identifier
            ),
            new EpubPropertyRenameListField(Key.Subject, "Subject", EpubDocumentField.Subject, defaultWidth: 160),
            new EpubPropertyRenameListField(
                Key.Description,
                "Description",
                EpubDocumentField.Description,
                defaultWidth: 200,
                tip: EpubRenameListFieldTips.Description
            ),
        ];
    }
}

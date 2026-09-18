namespace Mfr.Models.RenameList.Fields.Office
{
    /// <summary>
    /// All Office Document Rename List fields (read-only originals).
    /// </summary>
    public static class OfficeRenameListFields
    {
        /// <summary>
        /// Office property group id.
        /// </summary>
        public const string Group = "Office";

        /// <summary>
        /// User-visible group label in the field shuttle groups list.
        /// </summary>
        public const string GroupLabel = "Office Document";

        /// <summary>
        /// Property keys within <see cref="Group"/>.
        /// </summary>
        public static class Key
        {
            /// <summary>PackageProperties Title.</summary>
            public const string Title = "Title";

            /// <summary>Author (maps from PackageProperties Creator).</summary>
            public const string Author = "Author";

            /// <summary>PackageProperties Subject.</summary>
            public const string Subject = "Subject";

            /// <summary>PackageProperties Keywords.</summary>
            public const string Keywords = "Keywords";

            /// <summary>PackageProperties Category.</summary>
            public const string Category = "Category";

            /// <summary>PackageProperties Description.</summary>
            public const string Description = "Description";

            /// <summary>PackageProperties LastModifiedBy.</summary>
            public const string LastModifiedBy = "LastModifiedBy";

            /// <summary>PackageProperties Created.</summary>
            public const string Created = "Created";

            /// <summary>PackageProperties Modified.</summary>
            public const string Modified = "Modified";
        }

        /// <summary>
        /// Office Document group fields in catalog order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            new OfficePropertyRenameListField(Key.Title, "Title", OfficeDocumentField.Title, defaultWidth: 160),
            new OfficePropertyRenameListField(
                Key.Author,
                "Author",
                OfficeDocumentField.Author,
                defaultWidth: 120,
                tip: OfficeRenameListFieldTips.Author
            ),
            new OfficePropertyRenameListField(Key.Subject, "Subject", OfficeDocumentField.Subject, defaultWidth: 160),
            new OfficePropertyRenameListField(
                Key.Keywords,
                "Keywords",
                OfficeDocumentField.Keywords,
                defaultWidth: 160
            ),
            new OfficePropertyRenameListField(
                Key.Category,
                "Category",
                OfficeDocumentField.Category,
                defaultWidth: 120,
                tip: OfficeRenameListFieldTips.Category
            ),
            new OfficePropertyRenameListField(
                Key.Description,
                "Description",
                OfficeDocumentField.Description,
                defaultWidth: 200
            ),
            new OfficePropertyRenameListField(
                Key.LastModifiedBy,
                "Last Modified By",
                OfficeDocumentField.LastModifiedBy,
                defaultWidth: 140,
                tip: OfficeRenameListFieldTips.LastModifiedBy
            ),
            new OfficePropertyRenameListField(Key.Created, "Created", OfficeDocumentField.Created, defaultWidth: 140),
            new OfficePropertyRenameListField(
                Key.Modified,
                "Modified",
                OfficeDocumentField.Modified,
                defaultWidth: 140
            ),
        ];
    }
}

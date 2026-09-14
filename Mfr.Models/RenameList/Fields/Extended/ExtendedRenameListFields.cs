using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Extended
{
    /// <summary>
    /// All MFR7 Extended ("File Properties") Rename List fields.
    /// </summary>
    public static class ExtendedRenameListFields
    {
        /// <summary>
        /// MFR7 Extended property group id.
        /// </summary>
        public const string Group = "Extended";

        /// <summary>
        /// User-visible group label in the field shuttle groups list.
        /// </summary>
        public const string GroupLabel = "File Properties";

        /// <summary>
        /// Property keys within <see cref="Group"/>.
        /// </summary>
        public static class Key
        {
            /// <summary>Creation timestamp column.</summary>
            public const string CreationDate = "CreationDate";

            /// <summary>Last-write timestamp column.</summary>
            public const string LastWriteDate = "LastWriteDate";

            /// <summary>Last-access timestamp column.</summary>
            public const string LastAccessDate = "LastAccessDate";

            /// <summary>File size column.</summary>
            public const string Size = "Size";

            /// <summary>Filesystem attributes column.</summary>
            public const string Attrs = "Attrs";

            /// <summary>Folder file-count column.</summary>
            public const string FileCount = "FileCount";
        }

        /// <summary>
        /// Extended group fields in catalog order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            new ExtendedCreationDateField(),
            new ExtendedLastWriteDateField(),
            new ExtendedLastAccessDateField(),
            new ExtendedSizeField(),
            new ExtendedAttributesField(),
            new ExtendedFileCountField(),
        ];
    }

    /// <summary>
    /// Shared base for MFR7 Extended ("File Properties") Rename List fields.
    /// </summary>
    /// <param name="propertyKey">Property key within the Extended group.</param>
    /// <param name="displayName">User-visible column label.</param>
    /// <param name="defaultWidth">Optional grid column width override in pixels.</param>
    /// <param name="supportsPreview">
    /// When <see langword="true"/>, a preview column variant may be added (MFR7 <c>ReadWrite</c> dates/attrs).
    /// </param>
    internal abstract class ExtendedRenameListField(
        string propertyKey,
        string displayName,
        int? defaultWidth = null,
        bool supportsPreview = false
    )
        : RenameListField(
            ExtendedRenameListFields.Group,
            ExtendedRenameListFields.GroupLabel,
            propertyKey,
            displayName,
            defaultWidth,
            isSortable: true,
            supportsPreview
        );

    internal sealed class ExtendedCreationDateField()
        : ExtendedRenameListField(
            ExtendedRenameListFields.Key.CreationDate,
            "Creation Date",
            defaultWidth: 110,
            supportsPreview: true
        )
    {
        /// <summary>Forwards to <see cref="ExtendedRenameListFields.Key.CreationDate"/>.</summary>
        public const string CreationDateKey = ExtendedRenameListFields.Key.CreationDate;

        public override string Resolve(FileMeta meta)
        {
            return RenameListFieldDisplay.FormatFileDate(meta.CreationTime);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            return RenameListFieldSortCompare.DateTime(left.CreationTime, right.CreationTime);
        }
    }

    internal sealed class ExtendedLastWriteDateField()
        : ExtendedRenameListField(
            ExtendedRenameListFields.Key.LastWriteDate,
            "Last Write Date",
            defaultWidth: 110,
            supportsPreview: true
        )
    {
        /// <summary>Forwards to <see cref="ExtendedRenameListFields.Key.LastWriteDate"/>.</summary>
        public const string LastWriteDateKey = ExtendedRenameListFields.Key.LastWriteDate;

        public override string Resolve(FileMeta meta)
        {
            return RenameListFieldDisplay.FormatFileDate(meta.LastWriteTime);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            return RenameListFieldSortCompare.DateTime(left.LastWriteTime, right.LastWriteTime);
        }
    }

    internal sealed class ExtendedLastAccessDateField()
        : ExtendedRenameListField(
            ExtendedRenameListFields.Key.LastAccessDate,
            "Last Access Date",
            defaultWidth: 110,
            supportsPreview: true
        )
    {
        /// <summary>Forwards to <see cref="ExtendedRenameListFields.Key.LastAccessDate"/>.</summary>
        public const string LastAccessDateKey = ExtendedRenameListFields.Key.LastAccessDate;

        public override string Resolve(FileMeta meta)
        {
            return RenameListFieldDisplay.FormatFileDate(meta.LastAccessTime);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            return RenameListFieldSortCompare.DateTime(left.LastAccessTime, right.LastAccessTime);
        }
    }

    internal sealed class ExtendedSizeField()
        : ExtendedRenameListField(ExtendedRenameListFields.Key.Size, "Size", defaultWidth: 75)
    {
        /// <summary>Forwards to <see cref="ExtendedRenameListFields.Key.Size"/>.</summary>
        public const string SizeKey = ExtendedRenameListFields.Key.Size;

        public override string Resolve(FileMeta meta)
        {
            return RenameListFieldDisplay.FormatFileSizeBytes(meta.FileSize);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            return RenameListFieldSortCompare.Int64(left.FileSize, right.FileSize);
        }
    }

    internal sealed class ExtendedAttributesField()
        : ExtendedRenameListField(
            ExtendedRenameListFields.Key.Attrs,
            "Attributes",
            defaultWidth: 65,
            supportsPreview: true
        )
    {
        /// <summary>Forwards to <see cref="ExtendedRenameListFields.Key.Attrs"/>.</summary>
        public const string AttributesKey = ExtendedRenameListFields.Key.Attrs;

        public override string Resolve(FileMeta meta)
        {
            return RenameListFieldDisplay.FormatAttributes(meta.Attributes);
        }
    }

    internal sealed class ExtendedFileCountField()
        : ExtendedRenameListField(ExtendedRenameListFields.Key.FileCount, "Folder File Count", defaultWidth: 65)
    {
        /// <summary>Forwards to <see cref="ExtendedRenameListFields.Key.FileCount"/>.</summary>
        public const string FileCountKey = ExtendedRenameListFields.Key.FileCount;

        public override string Resolve(FileMeta meta)
        {
            return RenameListFieldDisplay.FormatFolderFileCount(meta);
        }

        /// <inheritdoc />
        public override int CompareForSort(FileMeta left, FileMeta right)
        {
            return RenameListFieldSortCompare.ParsedInt64(Resolve(left), Resolve(right));
        }
    }
}

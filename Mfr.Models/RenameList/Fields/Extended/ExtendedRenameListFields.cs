using System.Globalization;
using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Extended
{
    /// <summary>
    /// All MFR7 Extended ("File Properties") Rename List fields.
    /// </summary>
    public static class ExtendedRenameListFields
    {
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

    internal sealed class ExtendedCreationDateField()
        : ExtendedRenameListField(CreationDateKey, "Creation Date", defaultWidth: 110)
    {
        public const string CreationDateKey = "CreationDate";

        public override string Resolve(FileMeta meta)
        {
            return RenameListFieldDisplay.FormatFileDate(meta.CreationTime);
        }
    }

    internal sealed class ExtendedLastWriteDateField()
        : ExtendedRenameListField(LastWriteDateKey, "Last Write Date", defaultWidth: 110)
    {
        public const string LastWriteDateKey = "LastWriteDate";

        public override string Resolve(FileMeta meta)
        {
            return RenameListFieldDisplay.FormatFileDate(meta.LastWriteTime);
        }
    }

    internal sealed class ExtendedLastAccessDateField()
        : ExtendedRenameListField(LastAccessDateKey, "Last Access Date", defaultWidth: 110)
    {
        public const string LastAccessDateKey = "LastAccessDate";

        public override string Resolve(FileMeta meta)
        {
            return RenameListFieldDisplay.FormatFileDate(meta.LastAccessTime);
        }
    }

    internal sealed class ExtendedSizeField() : ExtendedRenameListField(SizeKey, "Size", defaultWidth: 75)
    {
        public const string SizeKey = "Size";

        public override string Resolve(FileMeta meta)
        {
            return RenameListFieldDisplay.FormatFileSizeBytes(meta.FileSize);
        }
    }

    internal sealed class ExtendedAttributesField()
        : ExtendedRenameListField(AttributesKey, "Attributes", defaultWidth: 65)
    {
        public const string AttributesKey = "Attrs";

        public override string Resolve(FileMeta meta)
        {
            return RenameListFieldDisplay.FormatAttributes(meta.Attributes);
        }
    }

    internal sealed class ExtendedFileCountField()
        : ExtendedRenameListField(FileCountKey, "Folder File Count", defaultWidth: 65)
    {
        public const string FileCountKey = "FileCount";

        public override string Resolve(FileMeta meta)
        {
            var directoryPath = meta.Attributes.HasFlag(FileAttributes.Directory) ? meta.FullPath : meta.DirectoryPath;

            if (!Directory.Exists(directoryPath))
            {
                return string.Empty;
            }

            return Directory.GetFiles(directoryPath).Length.ToString(CultureInfo.InvariantCulture);
        }
    }
}

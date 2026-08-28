using Mfr.Models.Rename;
using Mfr.Utils;

namespace Mfr.Models.RenameList.Fields.Basic
{
    /// <summary>
    /// All MFR7 Basic ("File Name") Rename List fields.
    /// </summary>
    public static class BasicRenameListFields
    {
        /// <summary>
        /// MFR7 property keys within <see cref="BasicRenameListField.Group"/>.
        /// </summary>
        public static class Key
        {
            /// <summary>File vs folder label.</summary>
            public const string ItemType = "ItemType";

            /// <summary>Parent directory path.</summary>
            public const string Folder = "Folder";

            /// <summary>Full file name including extension.</summary>
            public const string FullName = "FullName";

            /// <summary>Absolute full path.</summary>
            public const string FullPath = "FullPath";

            /// <summary>File name without extension.</summary>
            public const string Name = "Name";

            /// <summary>File extension without a leading dot.</summary>
            public const string Extension = "Extension";

            /// <summary>First digit run in the full file name.</summary>
            public const string FileNameNumeric = "FileNameNumeric";

            /// <summary>Length of the full file name including extension.</summary>
            public const string FileNameLength = "FileNameLength";

            /// <summary>Length of the absolute full path.</summary>
            public const string FullPathLength = "FullPathLength";
        }

        /// <summary>
        /// Basic group fields in catalog order.
        /// </summary>
        public static IReadOnlyList<RenameListField> All { get; } =
        [
            new BasicItemTypeField(),
            new BasicFolderField(),
            new BasicFullNameField(),
            new BasicFullPathField(),
            new BasicNameField(),
            new BasicExtensionField(),
            new BasicFileNameNumericField(),
            new BasicFileNameLengthField(),
            new BasicFullPathLengthField(),
        ];
    }

    internal sealed class BasicItemTypeField()
        : BasicRenameListField(
            propertyKey: BasicRenameListFields.Key.ItemType,
            displayName: "File/Folder",
            supportsPreview: false,
            sortColumn: RenameListSortColumn.FileFolder
        )
    {
        public override string Resolve(FileMeta meta)
        {
            return meta.Attributes.IsDirectory() ? "Folder" : "File";
        }
    }

    internal sealed class BasicFolderField()
        : BasicRenameListField(
            propertyKey: BasicRenameListFields.Key.Folder,
            displayName: "Parent Folder",
            defaultWidth: 240,
            sortColumn: RenameListSortColumn.ParentFolder
        )
    {
        public override string Resolve(FileMeta meta)
        {
            return meta.DirectoryPath;
        }
    }

    internal sealed class BasicFullNameField()
        : BasicRenameListField(
            propertyKey: BasicRenameListFields.Key.FullName,
            displayName: "Full File Name",
            defaultWidth: 180,
            sortColumn: RenameListSortColumn.FullFileName
        )
    {
        public override string Resolve(FileMeta meta)
        {
            return meta.Prefix + meta.Extension;
        }
    }

    internal sealed class BasicFullPathField()
        : BasicRenameListField(
            propertyKey: BasicRenameListFields.Key.FullPath,
            displayName: "Full File Path",
            defaultWidth: 180,
            sortColumn: RenameListSortColumn.FullPath
        )
    {
        public override string Resolve(FileMeta meta)
        {
            return meta.FullPath;
        }
    }

    internal sealed class BasicNameField()
        : BasicRenameListField(propertyKey: BasicRenameListFields.Key.Name, displayName: "File Name", defaultWidth: 150)
    {
        public override string Resolve(FileMeta meta)
        {
            return meta.Prefix;
        }
    }

    internal sealed class BasicExtensionField()
        : BasicRenameListField(propertyKey: BasicRenameListFields.Key.Extension, displayName: "File Extension")
    {
        public override string Resolve(FileMeta meta)
        {
            var extension = meta.Extension;
            if (extension.Length == 0)
            {
                return string.Empty;
            }

            return extension.StartsWith('.') ? extension[1..] : extension;
        }
    }

    internal sealed class BasicFileNameNumericField()
        : BasicRenameListField(
            propertyKey: BasicRenameListFields.Key.FileNameNumeric,
            displayName: "File Name Numeric Value",
            supportsPreview: false
        )
    {
        public override string Resolve(FileMeta meta)
        {
            return _FirstDigitRun(meta.Prefix + meta.Extension);
        }

        private static string _FirstDigitRun(string fullFileName)
        {
            for (var i = 0; i < fullFileName.Length; i++)
            {
                if (!char.IsAsciiDigit(fullFileName[i]))
                {
                    continue;
                }

                var end = i + 1;
                while (end < fullFileName.Length && char.IsAsciiDigit(fullFileName[end]) && end - i < 10)
                {
                    end++;
                }

                return long.Parse(fullFileName.AsSpan(i, end - i)).ToString();
            }

            return "0";
        }
    }

    internal sealed class BasicFileNameLengthField()
        : BasicRenameListField(propertyKey: BasicRenameListFields.Key.FileNameLength, displayName: "File Name Length")
    {
        public override string Resolve(FileMeta meta)
        {
            return (meta.Prefix + meta.Extension).Length.ToString();
        }
    }

    internal sealed class BasicFullPathLengthField()
        : BasicRenameListField(
            propertyKey: BasicRenameListFields.Key.FullPathLength,
            displayName: "Full Path Name Length"
        )
    {
        public override string Resolve(FileMeta meta)
        {
            return meta.FullPath.Length.ToString();
        }
    }
}

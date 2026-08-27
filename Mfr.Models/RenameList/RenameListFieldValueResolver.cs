using Mfr.Models.Rename;
using Mfr.Utils;

namespace Mfr.Models.RenameList
{
    /// <summary>
    /// Resolves Rename List field values from engine rename items.
    /// </summary>
    public static class RenameListFieldValueResolver
    {
        /// <summary>
        /// Returns the display text for one field on a rename item.
        /// </summary>
        /// <param name="item">Engine rename item.</param>
        /// <param name="key">Field key (original or preview).</param>
        /// <returns>Display string for the grid or sort shuttle.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is not registered in the catalog.</exception>
        public static string Resolve(RenameItem item, RenameListFieldKey key)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (!RenameListFieldCatalog.TryGetDefinition(key, out _))
            {
                throw new ArgumentException(
                    $"Unknown Rename List field '{key.GroupId}/{key.PropertyKey}'.",
                    nameof(key)
                );
            }

            var meta = key.IsPreview ? item.Preview : item.Original;
            return _ResolveBasic(meta, key.PropertyKey);
        }

        private static string _ResolveBasic(FileMeta meta, string propertyKey)
        {
            var fullFileName = meta.Prefix + meta.Extension;
            var extensionWithoutDot = _FormatExtensionWithoutDot(meta.Extension);

            return propertyKey switch
            {
                RenameListBasicPropertyKeys.ItemType => meta.Attributes.IsDirectory() ? "Folder" : "File",
                RenameListBasicPropertyKeys.Name => meta.Prefix,
                RenameListBasicPropertyKeys.Extension => extensionWithoutDot,
                RenameListBasicPropertyKeys.FullName => fullFileName,
                RenameListBasicPropertyKeys.Folder => meta.DirectoryPath,
                RenameListBasicPropertyKeys.FullPath => meta.FullPath,
                RenameListBasicPropertyKeys.FileNameNumeric => _FormatFileNameNumeric(fullFileName),
                RenameListBasicPropertyKeys.FileNameLength => fullFileName.Length.ToString(),
                RenameListBasicPropertyKeys.FullPathLength => meta.FullPath.Length.ToString(),
                _ => throw new ArgumentException(
                    $"Unsupported Basic property key '{propertyKey}'.",
                    nameof(propertyKey)
                ),
            };
        }

        private static string _FormatExtensionWithoutDot(string extension)
        {
            if (extension.Length == 0)
            {
                return string.Empty;
            }

            return extension.StartsWith('.') ? extension[1..] : extension;
        }

        private static string _FormatFileNameNumeric(string fullFileName)
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
}

using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Basic
{
    /// <summary>
    /// File extension without a leading dot (MFR7 BasicPG).
    /// </summary>
    public sealed class BasicExtensionField()
        : BasicRenameListField(propertyKey: Key, displayName: "File Extension")
    {
        /// <summary>
        /// MFR7 property key.
        /// </summary>
        public const string Key = "Extension";

        /// <inheritdoc />
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
}

using Mfr.Models.Rename;

namespace Mfr.Models.RenameList.Fields.Basic
{
    /// <summary>
    /// Length of the absolute full path.
    /// </summary>
    public sealed class BasicFullPathLengthField()
        : BasicRenameListField(propertyKey: Key, displayName: "Full Path Name Length", order: 8, defaultWidth: 40)
    {
        /// <summary>
        /// MFR7 property key.
        /// </summary>
        public const string Key = "FullPathLength";

        /// <inheritdoc />
        public override string Resolve(FileMeta meta)
        {
            return meta.FullPath.Length.ToString();
        }
    }
}

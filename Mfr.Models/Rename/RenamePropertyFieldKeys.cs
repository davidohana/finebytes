using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;

namespace Mfr.Models.Rename
{
    /// <summary>
    /// Maps rename-log <see cref="RenamePropertyChange.Property"/> names to preview-side Rename List field keys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used by PrepareUndo for column replace and writable ForceValue mirrors. Unrestorable strip and
    /// <c>AudioTag.Block.*</c> properties are unmapped (sticky OldValue seed remains the fidelity path).
    /// </para>
    /// </remarks>
    public static class RenamePropertyFieldKeys
    {
        /// <summary>
        /// Tries to map a logged property name to its preview-side catalog field key.
        /// </summary>
        /// <param name="property">Canonical <see cref="RenamePropertyNames"/> value (or other logged name).</param>
        /// <param name="key">Preview field key when mapped; otherwise default.</param>
        /// <returns><see langword="true"/> when <paramref name="property"/> maps to a catalog preview field.</returns>
        public static bool TryMapPreview(string property, out RenameListFieldKey key)
        {
            if (string.IsNullOrEmpty(property))
            {
                key = default;
                return false;
            }

            if (property == RenamePropertyNames.Prefix)
            {
                key = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
                return true;
            }

            if (property == RenamePropertyNames.Extension)
            {
                key = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Extension);
                return true;
            }

            if (property == RenamePropertyNames.DirectoryPath)
            {
                key = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Folder);
                return true;
            }

            if (property == RenamePropertyNames.Attributes)
            {
                key = RenameListFieldKey.Preview(ExtendedRenameListFields.Group, ExtendedRenameListFields.Key.Attrs);
                return true;
            }

            if (property == RenamePropertyNames.CreationTime)
            {
                key = RenameListFieldKey.Preview(
                    ExtendedRenameListFields.Group,
                    ExtendedRenameListFields.Key.CreationDate
                );
                return true;
            }

            if (property == RenamePropertyNames.LastWriteTime)
            {
                key = RenameListFieldKey.Preview(
                    ExtendedRenameListFields.Group,
                    ExtendedRenameListFields.Key.LastWriteDate
                );
                return true;
            }

            if (property == RenamePropertyNames.LastAccessTime)
            {
                key = RenameListFieldKey.Preview(
                    ExtendedRenameListFields.Group,
                    ExtendedRenameListFields.Key.LastAccessDate
                );
                return true;
            }

            key = default;
            return false;
        }

        /// <summary>
        /// Collects distinct preview field keys for mapped properties across <paramref name="changes"/> (first-seen order).
        /// </summary>
        /// <param name="changes">Logged property deltas.</param>
        /// <returns>Preview keys for columns / override mirrors; empty when nothing mapped.</returns>
        public static IReadOnlyList<RenameListFieldKey> CollectPreviewKeys(IEnumerable<RenamePropertyChange> changes)
        {
            ArgumentNullException.ThrowIfNull(changes);

            var keys = new List<RenameListFieldKey>();
            var seen = new HashSet<RenameListFieldKey>();
            foreach (var change in changes)
            {
                if (!TryMapPreview(change.Property, out var key) || !seen.Add(key))
                {
                    continue;
                }

                keys.Add(key);
            }

            return keys;
        }

        /// <summary>
        /// Collects distinct preview field keys for mapped properties across undoable log entries.
        /// </summary>
        /// <param name="log">Rename log (entries with errors are skipped via <see cref="RenameLogEntry.IsUndoable"/>).</param>
        /// <returns>Preview keys in first-seen order across undoable entries.</returns>
        public static IReadOnlyList<RenameListFieldKey> CollectPreviewKeysFromLog(RenameLog log)
        {
            ArgumentNullException.ThrowIfNull(log);

            return CollectPreviewKeys(
                log.Entries.Where(static entry => entry.IsUndoable).SelectMany(static entry => entry.Changes)
            );
        }
    }
}

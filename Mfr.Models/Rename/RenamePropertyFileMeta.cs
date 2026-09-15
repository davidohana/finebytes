using System.Globalization;
using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;

namespace Mfr.Models.Rename
{
    /// <summary>
    /// Path and filesystem rename-log properties: preview field keys and OldValue apply onto <see cref="FileMeta"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Shared by PrepareUndo column replace, ForceValue mirrors, and undo OldValue seeding. Unrestorable strip and
    /// <c>AudioTag.Block.*</c> stay outside this map (sticky fidelity / dedicated appliers).
    /// </para>
    /// </remarks>
    public static class RenamePropertyFileMeta
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
        /// Applies a logged OldValue onto <paramref name="preview"/> for path/filesystem properties.
        /// </summary>
        /// <param name="preview">Preview snapshot to mutate.</param>
        /// <param name="property">Canonical <see cref="RenamePropertyNames"/> value.</param>
        /// <param name="oldValue">Logged OldValue string.</param>
        /// <returns><see langword="true"/> when <paramref name="property"/> was handled here.</returns>
        public static bool TryApplyOldValue(FileMeta preview, string property, string oldValue)
        {
            ArgumentNullException.ThrowIfNull(preview);
            ArgumentNullException.ThrowIfNull(property);
            ArgumentNullException.ThrowIfNull(oldValue);

            if (property == RenamePropertyNames.Prefix)
            {
                preview.Prefix = oldValue;
                return true;
            }

            if (property == RenamePropertyNames.Extension)
            {
                preview.Extension = oldValue;
                return true;
            }

            if (property == RenamePropertyNames.DirectoryPath)
            {
                preview.DirectoryPath = oldValue;
                return true;
            }

            if (property == RenamePropertyNames.Attributes)
            {
                preview.Attributes = Enum.Parse<FileAttributes>(oldValue);
                return true;
            }

            if (property == RenamePropertyNames.CreationTime)
            {
                preview.CreationTime = _ParseRoundtripLocal(oldValue);
                return true;
            }

            if (property == RenamePropertyNames.LastWriteTime)
            {
                preview.LastWriteTime = _ParseRoundtripLocal(oldValue);
                return true;
            }

            if (property == RenamePropertyNames.LastAccessTime)
            {
                preview.LastAccessTime = _ParseRoundtripLocal(oldValue);
                return true;
            }

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

        private static DateTime _ParseRoundtripLocal(string oldValue)
        {
            return DateTime.Parse(oldValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
    }
}

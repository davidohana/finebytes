using System.Globalization;
using System.Text.Json;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Ape;
using Mfr.Models.Tags.Apple;
using Mfr.Models.Tags.Asf;
using Mfr.Models.Tags.Id3v1;
using Mfr.Models.Tags.Id3v2;
using Mfr.Models.Tags.RiffInfo;
using Mfr.Utils;
using Serilog;

namespace Mfr.Engine.Preview
{
    /// <summary>
    /// Applies logged <see cref="RenamePropertyChange.OldValue"/> strings onto a row's <see cref="RenameItem.Preview"/>
    /// so undo can re-commit through the normal path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>StripAllEmbeddedTagsOnCommit</c> rows are skipped — Tag Remover strip is unrestorable.
    /// </para>
    /// </remarks>
    internal static class RenamePropertyOldValueApplier
    {
        private const string _Absent = "absent";
        private const string _Present = "present";

        /// <summary>
        /// Mutates <paramref name="item"/>.<see cref="RenameItem.Preview"/> (and strip flag) from OldValues.
        /// </summary>
        /// <param name="item">Row whose Original matches the post-GO path; Preview starts as a clone of Original.</param>
        /// <param name="changes">Property deltas from the rename log (Old → New at GO time).</param>
        internal static void Apply(RenameItem item, IReadOnlyList<RenamePropertyChange> changes)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(changes);

            foreach (var change in changes)
            {
                _ApplyOne(item, change);
            }

            item.Preview.CanonicalizeFileNameAndExtension();
        }

        private static void _ApplyOne(RenameItem item, RenamePropertyChange change)
        {
            var property = change.Property;
            var oldValue = change.OldValue;

            if (RenamePropertyFileMeta.TryApplyOldValue(item.Preview, property, oldValue))
            {
                return;
            }

            if (property == RenamePropertyNames.StripAllEmbeddedTagsOnCommit)
            {
                // Unrestorable — strip already removed tags from disk.
                return;
            }

            if (property.StartsWith(RenamePropertyNames.AudioTagBlockPrefix, StringComparison.Ordinal))
            {
                _ApplyAudioTagProperty(
                    item.Preview.AudioTagOverlay,
                    property[RenamePropertyNames.AudioTagBlockPrefix.Length..],
                    oldValue
                );
                return;
            }

            Log.Warning("Ignoring unrecognized rename-log property '{Property}' during undo OldValue apply.", property);
        }

        private static void _ApplyAudioTagProperty(AudioTagOverlay overlay, string blockRelative, string oldValue)
        {
            var firstDot = blockRelative.IndexOf('.');
            if (firstDot < 0)
            {
                _ApplyBlockPresence(overlay, blockRelative, oldValue);
                return;
            }

            var blockName = blockRelative[..firstDot];
            var fieldKey = blockRelative[(firstDot + 1)..];
            if (blockName == RenamePropertyNames.Id3v1)
            {
                _ApplyId3v1Field(overlay, fieldKey, oldValue);
                return;
            }

            if (blockName == RenamePropertyNames.Id3v2)
            {
                _ApplyId3v2Field(overlay, fieldKey, oldValue);
                return;
            }

            if (blockName == RenamePropertyNames.Xiph)
            {
                _ApplyXiphField(overlay, fieldKey, oldValue);
                return;
            }

            if (blockName == RenamePropertyNames.Ape)
            {
                _ApplyApeField(overlay, fieldKey, oldValue);
                return;
            }

            if (blockName == RenamePropertyNames.RiffInfo)
            {
                _ApplyRiffInfoField(overlay, fieldKey, oldValue);
                return;
            }

            if (blockName == RenamePropertyNames.Apple)
            {
                _ApplyAppleField(overlay, fieldKey, oldValue);
                return;
            }

            if (blockName == RenamePropertyNames.Asf)
            {
                _ApplyAsfField(overlay, fieldKey, oldValue);
            }
        }

        private static void _ApplyBlockPresence(AudioTagOverlay overlay, string blockName, string oldValue)
        {
            if (!_TryParseBlockKind(blockName, out var kind))
            {
                return;
            }

            if (string.Equals(oldValue, _Absent, StringComparison.Ordinal))
            {
                overlay.ClearBlock(kind);
                return;
            }

            if (string.Equals(oldValue, _Present, StringComparison.Ordinal))
            {
                // Presence-only rows cannot restore field contents; materialize an empty block.
                overlay.EnsureEmptyBlock(kind);
            }
        }

        private static bool _TryParseBlockKind(string blockName, out AudioTagBlockKind kind)
        {
            return Enum.TryParse(blockName, ignoreCase: false, result: out kind);
        }

        private static void _ApplyId3v1Field(AudioTagOverlay overlay, string fieldKey, string oldValue)
        {
            if (!Enum.TryParse<Id3v1Field>(fieldKey, ignoreCase: false, result: out var field))
            {
                return;
            }

            var text = _AbsentOrJsonNullToEmpty(oldValue);
            if (field is Id3v1Field.Year or Id3v1Field.Track or Id3v1Field.Genre)
            {
                text = _NormalizeJsonScalarToFieldText(oldValue);
            }

            AudioOverlayBlockFieldIo.SetId3v1FieldString(overlay, field, text);
        }

        private static void _ApplyId3v2Field(AudioTagOverlay overlay, string fieldKey, string oldValue)
        {
            if (string.Equals(fieldKey, "Version", StringComparison.Ordinal))
            {
                overlay.EnsureEmptyBlock(AudioTagBlockKind.Id3v2);
                var version = byte.Parse(oldValue, CultureInfo.InvariantCulture);
                var existing = overlay.Id3v2!;
                overlay.Id3v2 = new Id3v2TagData { Version = version, Frames = existing.Frames };
                return;
            }

            _ParseId3v2FrameKey(fieldKey, out var frameId, out var language, out var description);
            var text = string.Equals(oldValue, _Absent, StringComparison.Ordinal) ? string.Empty : oldValue;
            AudioOverlayBlockFieldIo.SetId3v2FrameString(overlay, frameId, text, language, description);
        }

        private static void _ParseId3v2FrameKey(
            string fieldKey,
            out string frameId,
            out string? language,
            out string? description
        )
        {
            var bracket = fieldKey.IndexOf('[');
            if (bracket < 0)
            {
                frameId = fieldKey;
                language = null;
                description = null;
                return;
            }

            frameId = fieldKey[..bracket];
            var inside = fieldKey[(bracket + 1)..].TrimEnd(']');
            var pipe = inside.IndexOf('|');
            if (pipe < 0)
            {
                language = inside.Length == 0 ? null : inside;
                description = null;
                return;
            }

            language = inside[..pipe];
            description = inside[(pipe + 1)..];
            if (language.Length == 0)
            {
                language = null;
            }

            if (description.Length == 0)
            {
                description = null;
            }
        }

        private static void _ApplyXiphField(AudioTagOverlay overlay, string fieldKey, string oldValue)
        {
            var text = string.Equals(oldValue, _Absent, StringComparison.Ordinal) ? string.Empty : oldValue;
            AudioOverlayBlockFieldIo.SetXiphFieldString(overlay, fieldKey, text);
        }

        private static void _ApplyApeField(AudioTagOverlay overlay, string fieldKey, string oldValue)
        {
            overlay.EnsureEmptyBlock(AudioTagBlockKind.Ape);
            var existing = overlay.Ape!;
            var key = fieldKey.Trim();
            var rows = existing.Fields.Where(r => !string.Equals(r.Key, key, StringComparison.Ordinal)).ToList();
            if (!string.Equals(oldValue, _Absent, StringComparison.Ordinal))
            {
                var values = DelimitedText.Split(oldValue);
                if (values.Length > 0)
                {
                    rows.Add(new TextFieldRow(key, values));
                }
            }

            rows.Sort(TextFieldRow.Compare);
            overlay.Ape = rows.Count == 0 ? null : new ApeTagData { Fields = [.. rows] };
        }

        private static void _ApplyRiffInfoField(AudioTagOverlay overlay, string fieldKey, string oldValue)
        {
            overlay.EnsureEmptyBlock(AudioTagBlockKind.RiffInfo);
            var existing = overlay.RiffInfo!;
            var key = fieldKey.Trim();
            var rows = existing.Fields.Where(r => !string.Equals(r.Key, key, StringComparison.Ordinal)).ToList();
            if (!string.Equals(oldValue, _Absent, StringComparison.Ordinal))
            {
                rows.Add(new RiffInfoFieldRow(key, oldValue));
            }

            overlay.RiffInfo = rows.Count == 0 ? null : new RiffInfoTagData { Fields = [.. rows] };
        }

        private static void _ApplyAppleField(AudioTagOverlay overlay, string fieldKey, string oldValue)
        {
            overlay.EnsureEmptyBlock(AudioTagBlockKind.Apple);
            var existing = overlay.Apple!;
            var atomType = Convert.FromHexString(fieldKey);
            var rows = existing.Atoms.Where(a => !a.AtomType.AsSpan().SequenceEqual(atomType)).ToList();
            if (!string.Equals(oldValue, _Absent, StringComparison.Ordinal))
            {
                var values = DelimitedText.Split(oldValue);
                if (values.Length > 0)
                {
                    rows.Add(new AppleAtomRow { AtomType = [.. atomType], Values = values });
                }
            }

            overlay.Apple = rows.Count == 0 ? null : new AppleTagData { Atoms = [.. rows] };
        }

        private static void _ApplyAsfField(AudioTagOverlay overlay, string fieldKey, string oldValue)
        {
            overlay.EnsureEmptyBlock(AudioTagBlockKind.Asf);
            var existing = overlay.Asf!;
            var rows = existing
                .Descriptors.Where(r => !string.Equals(r.Name, fieldKey, StringComparison.Ordinal))
                .ToList();
            if (!string.Equals(oldValue, _Absent, StringComparison.Ordinal))
            {
                rows.Add(new AsfDescriptorRow(fieldKey, oldValue));
            }

            overlay.Asf = rows.Count == 0 ? null : new AsfTagData { Descriptors = [.. rows] };
        }

        private static string _AbsentOrJsonNullToEmpty(string oldValue)
        {
            if (
                string.Equals(oldValue, _Absent, StringComparison.Ordinal)
                || string.Equals(oldValue, "null", StringComparison.Ordinal)
            )
            {
                return string.Empty;
            }

            return oldValue;
        }

        private static string _NormalizeJsonScalarToFieldText(string oldValue)
        {
            if (
                string.Equals(oldValue, _Absent, StringComparison.Ordinal)
                || string.Equals(oldValue, "null", StringComparison.Ordinal)
            )
            {
                return string.Empty;
            }

            try
            {
                using var doc = JsonDocument.Parse(oldValue);
                return doc.RootElement.ValueKind switch
                {
                    JsonValueKind.Null => string.Empty,
                    JsonValueKind.Number => doc.RootElement.GetRawText(),
                    JsonValueKind.String => doc.RootElement.GetString() ?? string.Empty,
                    JsonValueKind.Undefined
                    or JsonValueKind.Object
                    or JsonValueKind.Array
                    or JsonValueKind.True
                    or JsonValueKind.False => oldValue,
                    _ => oldValue,
                };
            }
            catch (JsonException)
            {
                return oldValue;
            }
        }
    }
}

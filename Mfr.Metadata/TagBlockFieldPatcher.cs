using System.Collections.Immutable;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Ape;
using Mfr.Models.Tags.Apple;
using Mfr.Models.Tags.Asf;
using Mfr.Models.Tags.Id3v1;
using Mfr.Models.Tags.Id3v2;
using Mfr.Models.Tags.RiffInfo;
using Mfr.Models.Tags.Xiph;
using TagLib.Id3v2;
using TagLib.Ogg;
using TagLib.Riff;
using AppleTag = TagLib.Mpeg4.AppleTag;

namespace Mfr.Metadata
{
    /// <summary>
    /// Original→Preview field patches onto live TagLib tags (modeled fields only; never Clear whole tags).
    /// </summary>
    internal static class TagBlockFieldPatcher
    {
        /// <summary>
        /// Creates or patches an ID3v2 tag from <paramref name="original"/> → <paramref name="preview"/>.
        /// </summary>
        /// <remarks>
        /// Create (<paramref name="original"/> null) writes all preview frames and sets <see cref="Id3v2TagData.Version"/>.
        /// Patch preserves the on-disk version and only adds/removes/replaces frames whose identity or text changed.
        /// Unmodeled frames (for example APIC) are never removed by this path.
        /// </remarks>
        public static void ApplyId3v2(Tag live, Id3v2TagData? original, Id3v2TagData preview)
        {
            if (original is null)
            {
                TagBlockFieldMapper.WriteId3v2(live, preview);
                return;
            }

            if (Equals(original, preview))
                return;

            // Preserve the version already on disk; do not silently upgrade on patch.
            var originalById = _IndexFrames(original.Frames);
            var previewById = _IndexFrames(preview.Frames);

            foreach (var (identity, _) in originalById)
            {
                if (previewById.ContainsKey(identity))
                    continue;

                _RemoveFrameByIdentity(live, identity);
            }

            foreach (var (identity, frame) in previewById)
            {
                if (originalById.TryGetValue(identity, out var prior) && Equals(prior, frame))
                    continue;

                _RemoveFrameByIdentity(live, identity);
                TagBlockFieldMapper.AddModeledFrame(live, frame);
            }
        }

        /// <summary>
        /// Creates or patches ID3v1 scalars.
        /// </summary>
        public static void ApplyId3v1(TagLib.Id3v1.Tag live, Id3v1TagData? original, Id3v1TagData preview)
        {
            if (original is not null && Equals(original, preview))
                return;

            TagBlockFieldMapper.WriteId3v1(live, preview);
        }

        /// <summary>
        /// Creates or patches known Xiph keys only.
        /// </summary>
        public static void ApplyXiph(XiphComment live, XiphTagData? original, XiphTagData preview)
        {
            if (original is null)
            {
                TagBlockFieldMapper.WriteXiph(live, preview);
                return;
            }

            if (Equals(original, preview))
                return;

            var originalMap = _ToMap(original.Fields);
            var previewMap = _ToMap(preview.Fields);

            foreach (var key in originalMap.Keys)
            {
                if (previewMap.ContainsKey(key))
                    continue;

                live.RemoveField(key);
            }

            foreach (var (key, values) in previewMap)
            {
                if (originalMap.TryGetValue(key, out var prior) && prior.SequenceEqual(values))
                    continue;

                live.SetField(key, [.. values]);
            }
        }

        /// <summary>
        /// Creates or patches known APE text items only.
        /// </summary>
        public static void ApplyApe(TagLib.Ape.Tag live, ApeTagData? original, ApeTagData preview)
        {
            if (original is null)
            {
                TagBlockFieldMapper.WriteApe(live, preview);
                return;
            }

            if (Equals(original, preview))
                return;

            var originalMap = _ToMap(original.Fields);
            var previewMap = _ToMap(preview.Fields);

            foreach (var key in originalMap.Keys)
            {
                if (previewMap.ContainsKey(key))
                    continue;

                live.RemoveItem(key);
            }

            foreach (var (key, values) in previewMap)
            {
                if (originalMap.TryGetValue(key, out var prior) && prior.SequenceEqual(values))
                    continue;

                live.SetValue(key, [.. values]);
            }
        }

        /// <summary>
        /// Creates or patches RIFF INFO fields via façade setters (only changed modeled keys).
        /// </summary>
        public static void ApplyRiffInfo(InfoTag live, RiffInfoTagData? original, RiffInfoTagData preview)
        {
            if (original is null)
            {
                TagBlockFieldMapper.WriteRiffInfo(live, preview);
                return;
            }

            if (Equals(original, preview))
                return;

            var originalCommon = TagBlockFieldMapper.CommonFromRiffRows(original.Fields);
            var previewCommon = TagBlockFieldMapper.CommonFromRiffRows(preview.Fields);
            TagBlockFieldMapper.WriteCommonDiffToTag(live, originalCommon, previewCommon);
        }

        /// <summary>
        /// Creates or patches Apple text atoms (sets changed atoms; clears atoms dropped from preview).
        /// </summary>
        public static void ApplyApple(AppleTag live, AppleTagData? original, AppleTagData preview)
        {
            if (original is null)
            {
                TagBlockFieldMapper.WriteApple(live, preview);
                return;
            }

            if (Equals(original, preview))
                return;

            var originalByType = _IndexApple(original.Atoms);
            var previewByType = _IndexApple(preview.Atoms);

            foreach (var (hex, _) in originalByType)
            {
                if (previewByType.ContainsKey(hex))
                    continue;

                var typeBytes = Convert.FromHexString(hex);
                live.SetText(typeBytes, []);
            }

            foreach (var (hex, row) in previewByType)
            {
                if (originalByType.TryGetValue(hex, out var prior) && Equals(prior, row))
                    continue;

                live.SetText([.. row.AtomType.ToArray()], [.. row.Values]);
            }
        }

        /// <summary>
        /// Creates or patches ASF fields without clearing the tag.
        /// </summary>
        /// <remarks>
        /// Content Description fields are applied via TagLib façade properties; extended descriptors use
        /// add/remove. Never calls <c>Clear()</c>.
        /// </remarks>
        public static void ApplyAsf(TagLib.Asf.Tag live, AsfTagData? original, AsfTagData preview)
        {
            if (original is null)
            {
                TagBlockFieldMapper.WriteAsf(live, preview);
                return;
            }

            if (Equals(original, preview))
                return;

            var originalByName = _IndexAsf(original.Descriptors);
            var previewByName = _IndexAsf(preview.Descriptors);

            foreach (var name in originalByName.Keys)
            {
                if (previewByName.ContainsKey(name))
                    continue;

                TagBlockFieldMapper.ClearAsfNamedValue(live, name);
            }

            foreach (var (name, value) in previewByName)
            {
                if (originalByName.TryGetValue(name, out var prior)
                    && string.Equals(prior, value, StringComparison.Ordinal))
                    continue;

                TagBlockFieldMapper.ApplyAsfNamedValue(live, name, value);
            }
        }

        private static Dictionary<string, Id3v2ModeledFrame> _IndexFrames(ImmutableArray<Id3v2ModeledFrame> frames)
        {
            var map = new Dictionary<string, Id3v2ModeledFrame>(StringComparer.Ordinal);
            foreach (var frame in frames)
                map[_FrameIdentity(frame)] = frame;

            return map;
        }

        private static string _FrameIdentity(Id3v2ModeledFrame frame)
        {
            if (!Id3v2ModeledFrame.MultiInstanceFrameIds.Contains(frame.FrameId))
                return frame.FrameId;

            return frame.FrameId
                + '\0'
                + (frame.Language ?? string.Empty)
                + '\0'
                + (frame.Description ?? string.Empty);
        }

        private static void _RemoveFrameByIdentity(Tag live, string identity)
        {
            var parts = identity.Split('\0');
            var frameId = parts[0];

            if (parts.Length == 1)
            {
                live.RemoveFrames(frameId);
                return;
            }

            var language = parts.Length > 1 ? parts[1] : string.Empty;
            var description = parts.Length > 2 ? parts[2] : string.Empty;

            foreach (var frame in live.GetFrames(frameId).ToArray())
            {
                if (!_LiveFrameMatches(frame, frameId, language, description))
                    continue;

                live.RemoveFrame(frame);
            }
        }

        private static bool _LiveFrameMatches(Frame frame, string frameId, string language, string description)
        {
            return frame switch
            {
                CommentsFrame comment when frameId == "COMM" =>
                    string.Equals(comment.Language ?? string.Empty, language, StringComparison.Ordinal)
                    && string.Equals(comment.Description ?? string.Empty, description, StringComparison.Ordinal),
                UnsynchronisedLyricsFrame lyrics when frameId == "USLT" =>
                    string.Equals(lyrics.Language ?? string.Empty, language, StringComparison.Ordinal)
                    && string.Equals(lyrics.Description ?? string.Empty, description, StringComparison.Ordinal),
                UserTextInformationFrame userText when frameId == "TXXX" =>
                    string.Equals(userText.Description ?? string.Empty, description, StringComparison.Ordinal),
                _ => false,
            };
        }

        private static Dictionary<string, ImmutableArray<string>> _ToMap(ImmutableArray<TextFieldRow> fields)
        {
            var map = new Dictionary<string, ImmutableArray<string>>(StringComparer.Ordinal);
            foreach (var row in fields)
                map[row.Key] = row.Values;

            return map;
        }

        private static Dictionary<string, AppleAtomRow> _IndexApple(ImmutableArray<AppleAtomRow> atoms)
        {
            var map = new Dictionary<string, AppleAtomRow>(StringComparer.Ordinal);
            foreach (var row in atoms)
                map[Convert.ToHexString(row.AtomType.AsSpan())] = row;

            return map;
        }

        private static Dictionary<string, string> _IndexAsf(ImmutableArray<AsfDescriptorRow> rows)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (string.IsNullOrEmpty(row.Name))
                    continue;

                map[row.Name] = row.Value;
            }

            return map;
        }
    }
}

using System.Collections.Immutable;
using Mfr.Models.Tags.Id3v2;
using Mfr.Utils;
using TagLib;
using TagLib.Id3v2;
using Id3v2Tag = TagLib.Id3v2.Tag;

namespace Mfr.Metadata.TagFields
{
    /// <summary>
    /// Reads and field-patches modeled ID3v2 text frames on a live TagLib tag.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only the singleton frame ids in <c>_SingletonFrameIds</c> plus <c>COMM</c> / <c>USLT</c> / <c>TXXX</c>
    /// are modeled; anything else (for example <c>APIC</c>, <c>UFID</c>, URL frames) stays on disk untouched.
    /// Multi-instance frames are identified by frame id plus language and description, so clearing one comment
    /// never removes its siblings.
    /// </para>
    /// </remarks>
    internal static class Id3v2TagFields
    {
        private static readonly HashSet<string> _SingletonFrameIds = new(StringComparer.Ordinal)
        {
            "TALB",
            "TBPM",
            "TCOM",
            "TCON",
            "TCOP",
            "TDAT",
            "TDEN",
            "TDOR",
            "TDRC",
            "TDRL",
            "TDTG",
            "TENC",
            "TEXT",
            "TFLT",
            "TIPL",
            "TIT1",
            "TIT2",
            "TIT3",
            "TKEY",
            "TLAN",
            "TLEN",
            "TMED",
            "TMOO",
            "TOAL",
            "TOFN",
            "TOLY",
            "TOPE",
            "TORY",
            "TOWN",
            "TPE1",
            "TPE2",
            "TPE3",
            "TPE4",
            "TPOS",
            "TPUB",
            "TRCK",
            "TRDA",
            "TRSN",
            "TRSO",
            "TSIZ",
            "TSOA",
            "TSOP",
            "TSSE",
            "TSST",
            "TYER",
        };

        /// <summary>
        /// Reads the file's modeled ID3v2 frames.
        /// </summary>
        /// <param name="file">Open TagLib file.</param>
        /// <returns>Block data, or <see langword="null"/> when the tag is absent or has no modeled text.</returns>
        public static Id3v2TagData? Read(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Id3v2, false) is not Id3v2Tag live)
                return null;

            var frames = _CollectFrames(live);
            if (frames.Count == 0)
                return null;

            frames.Sort(_CompareFrames);
            return new Id3v2TagData { Version = live.Version, Frames = [.. frames] };
        }

        /// <summary>
        /// Creates or patches the file's ID3v2 tag from <paramref name="original"/> → <paramref name="preview"/>.
        /// </summary>
        /// <remarks>
        /// Create (<paramref name="original"/> null) writes all preview frames and sets
        /// <see cref="Id3v2TagData.Version"/>. Patch preserves the on-disk version and only adds, removes, or
        /// replaces frames whose identity or text changed.
        /// </remarks>
        /// <param name="file">Open TagLib file.</param>
        /// <param name="original">Block as read from disk, or <see langword="null"/> to create.</param>
        /// <param name="preview">Block the preview wants on disk.</param>
        public static void Apply(TagLib.File file, Id3v2TagData? original, Id3v2TagData preview)
        {
            if (Equals(original, preview))
                return;

            var live = (Id3v2Tag)file.GetTag(TagTypes.Id3v2, true);
            if (original is null)
            {
                _WriteAll(live, preview);
                return;
            }

            // Preserve the version already on disk; do not silently upgrade on patch.
            TagFieldDiff.Apply(
                _IndexFrames(original.Frames),
                _IndexFrames(preview.Frames),
                valuesEqual: static (prior, frame) => prior.Equals(frame),
                remove: identity => _RemoveByIdentity(live, identity),
                set: (identity, frame) =>
                {
                    _RemoveByIdentity(live, identity);
                    _AddFrame(live, frame);
                }
            );
        }

        private static void _WriteAll(Id3v2Tag live, Id3v2TagData data)
        {
            live.Version = data.Version;

            foreach (var frameId in _SingletonFrameIds)
                live.RemoveFrames(frameId);

            foreach (var frameId in Id3v2ModeledFrame.MultiInstanceFrameIds)
                live.RemoveFrames(frameId);

            foreach (var modeled in data.Frames)
                _AddFrame(live, modeled);
        }

        private static List<Id3v2ModeledFrame> _CollectFrames(Id3v2Tag live)
        {
            var list = new List<Id3v2ModeledFrame>();

            foreach (var frame in live)
            {
                switch (frame)
                {
                    case CommentsFrame comment:
                        list.Add(
                            new Id3v2ModeledFrame
                            {
                                FrameId = "COMM",
                                Language = comment.Language.TrimmedOrNull(),
                                Description = comment.Description.TrimmedOrNull(),
                                TextValues = _SingleText(comment.Text),
                            }
                        );
                        break;

                    case UnsynchronisedLyricsFrame lyrics:
                        list.Add(
                            new Id3v2ModeledFrame
                            {
                                FrameId = "USLT",
                                Language = lyrics.Language.TrimmedOrNull(),
                                Description = lyrics.Description.TrimmedOrNull(),
                                TextValues = _SingleText(lyrics.Text),
                            }
                        );
                        break;

                    case UserTextInformationFrame userText:
                        list.Add(
                            new Id3v2ModeledFrame
                            {
                                FrameId = "TXXX",
                                Description = userText.Description.TrimmedOrNull(),
                                TextValues = DelimitedText.TrimNonEmpty(userText.Text),
                            }
                        );
                        break;

                    case TextInformationFrame text:
                    {
                        var frameId = text.FrameId.ToString(StringType.Latin1);
                        if (!_SingletonFrameIds.Contains(frameId))
                            break;

                        var values = DelimitedText.TrimNonEmpty(text.Text);
                        if (values.Length == 0)
                            break;

                        list.Add(new Id3v2ModeledFrame { FrameId = frameId, TextValues = values });
                        break;
                    }

                    default:
                        break;
                }
            }

            return list;
        }

        private static void _AddFrame(Id3v2Tag live, Id3v2ModeledFrame modeled)
        {
            if (modeled.TextValues.Length == 0)
                return;

            switch (modeled.FrameId)
            {
                case "COMM":
                {
                    var frame = new CommentsFrame(
                        modeled.Description ?? string.Empty,
                        modeled.Language ?? "eng",
                        StringType.UTF8
                    )
                    {
                        Text = modeled.TextValues[0],
                    };
                    live.AddFrame(frame);
                    break;
                }

                case "USLT":
                {
                    var frame = new UnsynchronisedLyricsFrame(
                        modeled.Description ?? string.Empty,
                        modeled.Language ?? "eng",
                        StringType.UTF8
                    )
                    {
                        Text = modeled.TextValues[0],
                    };
                    live.AddFrame(frame);
                    break;
                }

                case "TXXX":
                {
                    var frame = new UserTextInformationFrame(modeled.Description ?? string.Empty, StringType.UTF8)
                    {
                        Text = [.. modeled.TextValues],
                    };
                    live.AddFrame(frame);
                    break;
                }

                default:
                {
                    var frame = new TextInformationFrame(modeled.FrameId, StringType.UTF8)
                    {
                        Text = [.. modeled.TextValues],
                    };
                    live.AddFrame(frame);
                    break;
                }
            }
        }

        private static Dictionary<string, Id3v2ModeledFrame> _IndexFrames(ImmutableArray<Id3v2ModeledFrame> frames)
        {
            var identityToFrame = new Dictionary<string, Id3v2ModeledFrame>(StringComparer.Ordinal);
            foreach (var frame in frames)
                identityToFrame[_FrameIdentity(frame)] = frame;

            return identityToFrame;
        }

        private static string _FrameIdentity(Id3v2ModeledFrame frame)
        {
            if (!Id3v2ModeledFrame.MultiInstanceFrameIds.Contains(frame.FrameId))
                return frame.FrameId;

            return frame.FrameId + '\0' + (frame.Language ?? string.Empty) + '\0' + (frame.Description ?? string.Empty);
        }

        private static void _RemoveByIdentity(Id3v2Tag live, string identity)
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
                CommentsFrame comment when frameId == "COMM" => string.Equals(
                    comment.Language ?? string.Empty,
                    language,
                    StringComparison.Ordinal
                ) && string.Equals(comment.Description ?? string.Empty, description, StringComparison.Ordinal),
                UnsynchronisedLyricsFrame lyrics when frameId == "USLT" => string.Equals(
                    lyrics.Language ?? string.Empty,
                    language,
                    StringComparison.Ordinal
                ) && string.Equals(lyrics.Description ?? string.Empty, description, StringComparison.Ordinal),
                UserTextInformationFrame userText when frameId == "TXXX" => string.Equals(
                    userText.Description ?? string.Empty,
                    description,
                    StringComparison.Ordinal
                ),
                _ => false,
            };
        }

        private static int _CompareFrames(Id3v2ModeledFrame a, Id3v2ModeledFrame b)
        {
            var byId = string.CompareOrdinal(a.FrameId, b.FrameId);
            if (byId != 0)
                return byId;

            var byLang = string.CompareOrdinal(a.Language, b.Language);
            if (byLang != 0)
                return byLang;

            var byDesc = string.CompareOrdinal(a.Description, b.Description);
            if (byDesc != 0)
                return byDesc;

            return OrdinalSequence.Compare(a.TextValues, b.TextValues);
        }

        /// <remarks>
        /// <c>COMM</c> and <c>USLT</c> carry a single text payload, unlike the multi-value text frames.
        /// </remarks>
        private static ImmutableArray<string> _SingleText(string? text)
        {
            var trimmed = text.TrimmedOrNull();
            return trimmed is null ? [] : [trimmed];
        }
    }
}

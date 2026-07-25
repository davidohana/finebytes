using System.Collections.Immutable;
using System.Globalization;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Ape;
using Mfr.Models.Tags.Apple;
using Mfr.Models.Tags.Asf;
using Mfr.Models.Tags.Id3v1;
using Mfr.Models.Tags.Id3v2;
using Mfr.Models.Tags.RiffInfo;
using Mfr.Models.Tags.Xiph;
using TagLib;
using TagLib.Id3v2;
using TagLib.Ogg;
using TagLib.Riff;
using AppleTag = TagLib.Mpeg4.AppleTag;

namespace Mfr.Metadata
{
    /// <summary>
    /// Maps TagLib tags ↔ parsed overlay field blocks (and SemanticAudioTag ↔ those blocks).
    /// </summary>
    internal static class TagBlockFieldMapper
    {
        private static readonly string[] _ListSeparators = [";"];

        private static readonly HashSet<string> _Id3v2SingletonFrameIds = new(StringComparer.Ordinal)
        {
            "TIT1", "TIT2", "TALB", "TPE1", "TPE2", "TCOM", "TCON", "TCOP", "TYER", "TDRC", "TRCK", "TPOS",
        };

        private static readonly string[] _KnownXiphKeys =
        [
            "TITLE", "ALBUM", "ARTIST", "ALBUMARTIST", "COMPOSER", "GENRE",
            "DESCRIPTION", "COMMENT", "LYRICS", "UNSYNCEDLYRICS", "COPYRIGHT",
            "GROUPING", "CONTENTGROUP", "DATE", "YEAR",
            "TRACKNUMBER", "TRACKTOTAL", "TOTALTRACKS",
            "DISCNUMBER", "DISCTOTAL", "TOTALDISCS",
        ];

        private static readonly string[] _KnownApeKeys =
        [
            "Title", "Album", "Artist", "Album Artist", "Composer", "Genre",
            "Comment", "Lyrics", "Copyright", "Grouping",
            "Year", "Track", "TrackCount", "Disc", "DiscCount",
        ];

        /// <summary>
        /// Reads modeled ID3v2 frames from a live TagLib tag, or <see langword="null"/> when empty of modeled text.
        /// </summary>
        public static Id3v2TagData? ReadId3v2(TagLib.Id3v2.Tag id3v2)
        {
            var frames = _CollectId3v2Frames(id3v2);
            if (frames.Count == 0)
                return null;

            frames.Sort(_CompareId3v2Frames);
            return new Id3v2TagData
            {
                Version = id3v2.Version,
                Frames = [.. frames],
            };
        }

        /// <summary>
        /// Reads known Xiph keys from a live comment, or <see langword="null"/> when none are present.
        /// </summary>
        public static XiphTagData? ReadXiph(XiphComment xc)
        {
            var rows = _ReadKnownMultimap(xc, _KnownXiphKeys, uppercaseKeys: true);
            return rows.Count == 0 ? null : new XiphTagData { Fields = [.. rows] };
        }

        /// <summary>
        /// Reads known APE text items, or <see langword="null"/> when none are present.
        /// </summary>
        public static ApeTagData? ReadApe(TagLib.Ape.Tag ape)
        {
            var rows = new List<TextFieldRow>();
            foreach (var key in _KnownApeKeys)
            {
                var item = ape.GetItem(key);
                if (item is null || item.IsEmpty)
                    continue;

                var values = _TrimNonEmpty(item.ToStringArray());
                if (values.Length == 0)
                    continue;

                rows.Add(new TextFieldRow(key, values));
            }

            // Also capture TagLib façade-backed values when item keys differ.
            _AddCommonAsRows(rows, SemanticAudioTagTagLib.FromCombinedTag(ape), preferExistingKeys: true);
            if (rows.Count == 0)
                return null;

            rows.Sort(_CompareTextFieldRows);
            return new ApeTagData { Fields = [.. rows] };
        }

        /// <summary>
        /// Reads known RIFF INFO fields from TagLib façade mapping, or <see langword="null"/> when empty.
        /// </summary>
        public static RiffInfoTagData? ReadRiffInfo(InfoTag info)
        {
            var semantic = SemanticAudioTagTagLib.FromCombinedTag(info);
            var rows = _RiffRowsFromCommon(semantic);
            return rows.Length == 0 ? null : new RiffInfoTagData { Fields = rows };
        }

        /// <summary>
        /// Writes modeled ID3v2 frames onto a live tag (clears modeled frame ids first; leaves unmodeled frames).
        /// </summary>
        /// <remarks>
        /// Used for create paths. Patch paths use <see cref="TagBlockFieldPatcher.ApplyId3v2"/> so only changed
        /// frame identities are touched.
        /// </remarks>
        public static void WriteId3v2(TagLib.Id3v2.Tag live, Id3v2TagData data)
        {
            live.Version = data.Version;

            foreach (var frameId in _Id3v2SingletonFrameIds)
                live.RemoveFrames(frameId);

            foreach (var frameId in Id3v2ModeledFrame.MultiInstanceFrameIds)
                live.RemoveFrames(frameId);

            foreach (var modeled in data.Frames)
                AddModeledFrame(live, modeled);
        }

        /// <summary>
        /// Adds one modeled frame instance to a live ID3v2 tag.
        /// </summary>
        internal static void AddModeledFrame(TagLib.Id3v2.Tag live, Id3v2ModeledFrame modeled)
        {
            _AddModeledFrame(live, modeled);
        }

        /// <summary>
        /// Writes known Xiph fields onto a live comment (sets/removes only known keys).
        /// </summary>
        public static void WriteXiph(XiphComment live, XiphTagData data)
        {
            foreach (var key in _KnownXiphKeys)
                live.RemoveField(key);

            foreach (var row in data.Fields)
            {
                if (row.Values.Length == 0)
                    continue;

                live.SetField(row.Key, [.. row.Values]);
            }
        }

        /// <summary>
        /// Writes known APE text items onto a live tag (sets/removes only known keys).
        /// </summary>
        public static void WriteApe(TagLib.Ape.Tag live, ApeTagData data)
        {
            foreach (var key in _KnownApeKeys)
                live.RemoveItem(key);

            foreach (var row in data.Fields)
            {
                if (row.Values.Length == 0)
                    continue;

                live.SetValue(row.Key, [.. row.Values]);
            }
        }

        /// <summary>
        /// Writes known RIFF INFO fields via TagLib façade setters.
        /// </summary>
        public static void WriteRiffInfo(InfoTag live, RiffInfoTagData data)
        {
            var common = _CommonFromRiffRows(data.Fields);
            _WriteCommonToTag(live, common);
        }

        /// <summary>
        /// Writes Apple text atoms from overlay rows.
        /// </summary>
        public static void WriteApple(AppleTag live, AppleTagData data)
        {
            foreach (var row in data.Atoms)
                live.SetText([.. row.AtomType.ToArray()], [.. row.Values]);
        }

        /// <summary>
        /// Writes ASF rows onto a live tag without clearing unmodeled fields.
        /// </summary>
        /// <remarks>
        /// Content Description fields (<see cref="AsfDescriptorNames.Title"/>, Author, Copyright) are applied
        /// via TagLib façade properties; other names go through extended descriptors.
        /// </remarks>
        public static void WriteAsf(TagLib.Asf.Tag live, AsfTagData data)
        {
            foreach (var row in data.Descriptors)
            {
                if (string.IsNullOrEmpty(row.Name))
                    continue;

                ApplyAsfNamedValue(live, row.Name, row.Value);
            }
        }

        /// <summary>
        /// Sets or clears one ASF overlay field on a live tag (Content Description or extended descriptor).
        /// </summary>
        /// <param name="live">Live ASF tag.</param>
        /// <param name="name">Canonical overlay name from <see cref="AsfDescriptorNames"/>.</param>
        /// <param name="value">Text to store; <see langword="null"/> or empty clears the field.</param>
        internal static void ApplyAsfNamedValue(TagLib.Asf.Tag live, string name, string? value)
        {
            var text = _NullIfEmpty(value);
            switch (name)
            {
                case AsfDescriptorNames.Title:
                    live.Title = text;
                    return;
                case AsfDescriptorNames.Author:
                    live.Performers = text is null ? [] : _SplitJoinedList(text);
                    return;
                case AsfDescriptorNames.Copyright:
                    live.Copyright = text;
                    return;
                default:
                    live.RemoveDescriptors(name);
                    if (text is not null)
                        live.AddDescriptor(new TagLib.Asf.ContentDescriptor(name, text));
                    return;
            }
        }

        /// <summary>
        /// Clears one ASF overlay field on a live tag.
        /// </summary>
        /// <param name="live">Live ASF tag.</param>
        /// <param name="name">Canonical overlay name from <see cref="AsfDescriptorNames"/>.</param>
        internal static void ClearAsfNamedValue(TagLib.Asf.Tag live, string name)
        {
            ApplyAsfNamedValue(live, name, null);
        }

        /// <summary>
        /// Writes ID3v1 scalars onto a live tag.
        /// </summary>
        public static void WriteId3v1(TagLib.Id3v1.Tag live, Id3v1TagData data)
        {
            live.Title = data.Title ?? string.Empty;
            live.Performers = string.IsNullOrWhiteSpace(data.Artist) ? [] : [data.Artist.Trim()];
            live.Album = data.Album ?? string.Empty;
            live.Year = data.Year ?? 0;
            live.Comment = data.Comment ?? string.Empty;
            live.Track = data.Track ?? 0;

            var genreName = Id3v1Genres.IndexToAudio(data.Genre);
            live.Genres = string.IsNullOrEmpty(genreName) ? [] : [genreName];
        }

        /// <summary>
        /// Writes only fields that differ between <paramref name="original"/> and <paramref name="preview"/> onto a façade tag.
        /// </summary>
        internal static void WriteCommonDiffToTag(TagLib.Tag tag, SemanticAudioTag original, SemanticAudioTag preview)
        {
            if (!string.Equals(original.Title, preview.Title, StringComparison.Ordinal))
                tag.Title = _EmptyStringToNull(preview.Title);

            if (!string.Equals(original.Album, preview.Album, StringComparison.Ordinal))
                tag.Album = _EmptyStringToNull(preview.Album);

            if (!string.Equals(original.Performers, preview.Performers, StringComparison.Ordinal))
                tag.Performers = _SplitJoinedList(preview.Performers);

            if (!string.Equals(original.AlbumArtists, preview.AlbumArtists, StringComparison.Ordinal))
                tag.AlbumArtists = _SplitJoinedList(preview.AlbumArtists);

            if (!string.Equals(original.Composers, preview.Composers, StringComparison.Ordinal))
                tag.Composers = _SplitJoinedList(preview.Composers);

            if (!string.Equals(original.Genre, preview.Genre, StringComparison.Ordinal))
                tag.Genres = string.IsNullOrWhiteSpace(preview.Genre) ? [] : [preview.Genre.Trim()];

            if (!string.Equals(original.Comment, preview.Comment, StringComparison.Ordinal))
                tag.Comment = _EmptyStringToNull(preview.Comment);

            if (!string.Equals(original.Lyrics, preview.Lyrics, StringComparison.Ordinal))
                tag.Lyrics = _EmptyStringToNull(preview.Lyrics);

            if (!string.Equals(original.Copyright, preview.Copyright, StringComparison.Ordinal))
                tag.Copyright = _EmptyStringToNull(preview.Copyright);

            if (!string.Equals(original.Grouping, preview.Grouping, StringComparison.Ordinal))
                tag.Grouping = _EmptyStringToNull(preview.Grouping);

            if (original.Year != preview.Year)
                tag.Year = preview.Year ?? 0;

            if (original.Track != preview.Track)
                tag.Track = preview.Track ?? 0;

            if (original.TrackCount != preview.TrackCount)
                tag.TrackCount = preview.TrackCount ?? 0;

            if (original.Disc != preview.Disc)
                tag.Disc = preview.Disc ?? 0;

            if (original.DiscCount != preview.DiscCount)
                tag.DiscCount = preview.DiscCount ?? 0;
        }

        /// <summary>
        /// Projects RIFF INFO rows into a <see cref="SemanticAudioTag"/> for façade patching.
        /// </summary>
        internal static SemanticAudioTag CommonFromRiffRows(ImmutableArray<RiffInfoFieldRow> fields)
        {
            return _CommonFromRiffRows(fields);
        }

        private static void _WriteCommonToTag(TagLib.Tag tag, SemanticAudioTag common)
        {
            tag.Title = _EmptyStringToNull(common.Title);
            tag.Album = _EmptyStringToNull(common.Album);
            tag.Performers = _SplitJoinedList(common.Performers);
            tag.AlbumArtists = _SplitJoinedList(common.AlbumArtists);
            tag.Composers = _SplitJoinedList(common.Composers);
            tag.Genres = string.IsNullOrWhiteSpace(common.Genre) ? [] : [common.Genre.Trim()];
            tag.Comment = _EmptyStringToNull(common.Comment);
            tag.Lyrics = _EmptyStringToNull(common.Lyrics);
            tag.Copyright = _EmptyStringToNull(common.Copyright);
            tag.Grouping = _EmptyStringToNull(common.Grouping);
            tag.Year = common.Year ?? 0;
            tag.Track = common.Track ?? 0;
            tag.TrackCount = common.TrackCount ?? 0;
            tag.Disc = common.Disc ?? 0;
            tag.DiscCount = common.DiscCount ?? 0;
        }

        private static List<Id3v2ModeledFrame> _CollectId3v2Frames(TagLib.Id3v2.Tag id3v2)
        {
            var list = new List<Id3v2ModeledFrame>();

            foreach (var frame in id3v2)
            {
                switch (frame)
                {
                    case CommentsFrame comment:
                        list.Add(new Id3v2ModeledFrame
                        {
                            FrameId = "COMM",
                            Language = _NullIfEmpty(comment.Language),
                            Description = _NullIfEmpty(comment.Description),
                            TextValues = _SingleText(comment.Text),
                        });
                        break;

                    case UnsynchronisedLyricsFrame lyrics:
                        list.Add(new Id3v2ModeledFrame
                        {
                            FrameId = "USLT",
                            Language = _NullIfEmpty(lyrics.Language),
                            Description = _NullIfEmpty(lyrics.Description),
                            TextValues = _SingleText(lyrics.Text),
                        });
                        break;

                    case UserTextInformationFrame userText:
                        list.Add(new Id3v2ModeledFrame
                        {
                            FrameId = "TXXX",
                            Description = _NullIfEmpty(userText.Description),
                            TextValues = _TrimNonEmpty(userText.Text),
                        });
                        break;

                    case TextInformationFrame text:
                        {
                            var frameId = text.FrameId.ToString(StringType.Latin1);
                            if (!_Id3v2SingletonFrameIds.Contains(frameId))
                                break;

                            var values = _TrimNonEmpty(text.Text);
                            if (values.Length == 0)
                                break;

                            list.Add(new Id3v2ModeledFrame
                            {
                                FrameId = frameId,
                                TextValues = values,
                            });
                            break;
                        }

                    default:
                        break;
                }
            }

            return list;
        }

        private static void _AddModeledFrame(TagLib.Id3v2.Tag live, Id3v2ModeledFrame modeled)
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
                            StringType.UTF8)
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
                            StringType.UTF8)
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

        private static List<TextFieldRow> _ReadKnownMultimap(XiphComment xc, string[] keys, bool uppercaseKeys)
        {
            var rows = new List<TextFieldRow>();
            foreach (var key in keys)
            {
                var values = _TrimNonEmpty(xc.GetField(key));
                if (values.Length == 0)
                    continue;

                var storedKey = uppercaseKeys ? key.ToUpperInvariant() : key;
                rows.Add(new TextFieldRow(storedKey, values));
            }

            rows.Sort(_CompareTextFieldRows);
            return rows;
        }

        private static void _AddCommonAsRows(List<TextFieldRow> rows, SemanticAudioTag common, bool preferExistingKeys)
        {
            var map = _ToMutableMultimap([.. rows]);
            void Set(string key, string? value)
            {
                if (preferExistingKeys && map.ContainsKey(key))
                    return;

                _SetMapScalar(map, key, value);
            }

            Set("Title", common.Title);
            Set("Album", common.Album);
            if (!preferExistingKeys || !map.ContainsKey("Artist"))
                _SetMapList(map, "Artist", common.Performers);

            Set("Genre", common.Genre);
            Set("Comment", common.Comment);
            Set("Lyrics", common.Lyrics);
            Set("Copyright", common.Copyright);
            Set("Grouping", common.Grouping);
            Set("Year", common.Year?.ToString(CultureInfo.InvariantCulture));
            Set("Track", common.Track?.ToString(CultureInfo.InvariantCulture));
            Set("TrackCount", common.TrackCount?.ToString(CultureInfo.InvariantCulture));
            Set("Disc", common.Disc?.ToString(CultureInfo.InvariantCulture));
            Set("DiscCount", common.DiscCount?.ToString(CultureInfo.InvariantCulture));

            rows.Clear();
            rows.AddRange(_SortedRows(map));
        }

        private static ImmutableArray<RiffInfoFieldRow> _RiffRowsFromCommon(SemanticAudioTag common)
        {
            var rows = new List<RiffInfoFieldRow>();
            _AddRiff(rows, "INAM", common.Title);
            _AddRiff(rows, "IPRD", common.Album);
            _AddRiff(rows, "IART", common.Performers);
            _AddRiff(rows, "IGNR", common.Genre);
            _AddRiff(rows, "ICMT", common.Comment);
            _AddRiff(rows, "ICOP", common.Copyright);
            _AddRiff(rows, "ICRD", common.Year?.ToString(CultureInfo.InvariantCulture));
            _AddRiff(rows, "ITRK", common.Track?.ToString(CultureInfo.InvariantCulture));
            rows.Sort(static (a, b) => string.CompareOrdinal(a.Key, b.Key));
            return [.. rows];
        }

        private static SemanticAudioTag _CommonFromRiffRows(ImmutableArray<RiffInfoFieldRow> fields)
        {
            string? Get(string key)
            {
                foreach (var row in fields)
                {
                    if (string.Equals(row.Key, key, StringComparison.Ordinal))
                        return row.Value;
                }

                return null;
            }

            uint? ParseUInt(string? text)
            {
                return text is not null && uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var u)
                    ? u
                    : null;
            }

            return new SemanticAudioTag(
                Title: Get("INAM"),
                Album: Get("IPRD"),
                Performers: Get("IART"),
                AlbumArtists: null,
                Composers: null,
                Genre: Get("IGNR"),
                Comment: Get("ICMT"),
                Lyrics: null,
                Copyright: Get("ICOP"),
                Grouping: null,
                Year: ParseUInt(Get("ICRD")),
                Track: ParseUInt(Get("ITRK")),
                TrackCount: null,
                Disc: null,
                DiscCount: null);
        }

        private static void _AddRiff(List<RiffInfoFieldRow> rows, string key, string? value)
        {
            var text = _NullIfEmpty(value);
            if (text is null)
                return;

            rows.Add(new RiffInfoFieldRow(key, text));
        }

        private static Dictionary<string, ImmutableArray<string>> _ToMutableMultimap(ImmutableArray<TextFieldRow> fields)
        {
            var map = new Dictionary<string, ImmutableArray<string>>(StringComparer.Ordinal);
            foreach (var row in fields)
                map[row.Key] = row.Values;

            return map;
        }

        private static void _SetMapScalar(Dictionary<string, ImmutableArray<string>> map, string key, string? value)
        {
            var text = _NullIfEmpty(value);
            if (text is null)
            {
                map.Remove(key);
                return;
            }

            map[key] = [text];
        }

        private static void _SetMapList(Dictionary<string, ImmutableArray<string>> map, string key, string? joined)
        {
            var values = _TrimNonEmpty(_SplitJoinedList(joined));
            if (values.Length == 0)
            {
                map.Remove(key);
                return;
            }

            map[key] = values;
        }

        private static ImmutableArray<TextFieldRow> _SortedRows(Dictionary<string, ImmutableArray<string>> map)
        {
            var rows = map
                .Select(static kvp => new TextFieldRow(kvp.Key, kvp.Value))
                .ToList();
            rows.Sort(_CompareTextFieldRows);
            return [.. rows];
        }

        private static int _CompareTextFieldRows(TextFieldRow a, TextFieldRow b)
        {
            var byKey = string.CompareOrdinal(a.Key, b.Key);
            if (byKey != 0)
                return byKey;

            return _CompareStringSeq(a.Values, b.Values);
        }

        private static int _CompareId3v2Frames(Id3v2ModeledFrame a, Id3v2ModeledFrame b)
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

            return _CompareStringSeq(a.TextValues, b.TextValues);
        }

        private static int _CompareStringSeq(ImmutableArray<string> a, ImmutableArray<string> b)
        {
            var len = Math.Min(a.Length, b.Length);
            for (var i = 0; i < len; i++)
            {
                var c = string.CompareOrdinal(a[i], b[i]);
                if (c != 0)
                    return c;
            }

            return a.Length.CompareTo(b.Length);
        }

        private static ImmutableArray<string> _SingleText(string? text)
        {
            var trimmed = _NullIfEmpty(text);
            return trimmed is null ? [] : [trimmed];
        }

        private static ImmutableArray<string> _TrimNonEmpty(IEnumerable<string>? values)
        {
            if (values is null)
                return [];

            return [.. values
                .Where(static v => !string.IsNullOrWhiteSpace(v))
                .Select(static v => v.Trim())];
        }

        private static string[] _SplitJoinedList(string? joined)
        {
            if (string.IsNullOrWhiteSpace(joined))
                return [];

            return [.. joined.Split(_ListSeparators, StringSplitOptions.TrimEntries)
                .Where(static part => !string.IsNullOrEmpty(part))
                .Select(static part => part.Trim())];
        }

        private static string? _NullIfEmpty(string? text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private static string? _EmptyStringToNull(string? text)
        {
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }
}

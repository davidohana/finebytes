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
            _AddCommonAsRows(rows, SemanticAudioTag.FromCombinedTag(ape), preferExistingKeys: true);
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
            var common = SemanticAudioTag.FromCombinedTag(info);
            var rows = _RiffRowsFromCommon(common);
            return rows.Length == 0 ? null : new RiffInfoTagData { Fields = rows };
        }

        /// <summary>
        /// Applies <paramref name="semantic"/> onto every present block (broadcast write); empty→absent; prunes empty modeled blocks to <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Does not create blocks. When the overlay carries none, callers create the container's recommended empty
        /// block first (<see cref="AudioTagContainerPolicy.GetRecommendedBlock"/>), then call this method.
        /// Sibling types are never invented (for example ID3v1 is not added because ID3v2 already exists).
        /// </remarks>
        public static void MergeSemanticIntoBlocks(AudioTagOverlay overlay, SemanticAudioTag semantic)
        {
            if (overlay.Id3v1 is not null)
                overlay.Id3v1 = _MergeId3v1(overlay.Id3v1, semantic);

            if (overlay.Id3v2 is not null)
                overlay.Id3v2 = _MergeId3v2(overlay.Id3v2, semantic);

            if (overlay.Xiph is not null)
                overlay.Xiph = _MergeXiph(overlay.Xiph, semantic);

            if (overlay.Ape is not null)
                overlay.Ape = _MergeApe(overlay.Ape, semantic);

            if (overlay.RiffInfo is not null)
                overlay.RiffInfo = _MergeRiff(semantic);

            if (overlay.Asf is not null)
                overlay.Asf = _MergeAsf(overlay.Asf, semantic);

            if (overlay.Apple is not null)
                overlay.Apple = _MergeApple(overlay.Apple, semantic);
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
        /// Writes ASF descriptors from overlay rows without clearing the tag.
        /// </summary>
        public static void WriteAsf(TagLib.Asf.Tag live, AsfTagData data)
        {
            foreach (var row in data.Descriptors)
            {
                if (string.IsNullOrEmpty(row.Name))
                    continue;

                live.RemoveDescriptors(row.Name);
                live.AddDescriptor(new TagLib.Asf.ContentDescriptor(row.Name, row.Value));
            }
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

            var genreName = Genres.IndexToAudio(data.Genre);
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

        private static Id3v1TagData? _MergeId3v1(Id3v1TagData existing, SemanticAudioTag common)
        {
            var parts = _SplitJoinedList(common.Performers);
            var artist = parts.Length > 0 ? parts[0] : null;
            var genreByte = string.IsNullOrWhiteSpace(common.Genre)
                ? (byte)0
                : Genres.AudioToIndex(common.Genre.Trim());
            byte? track = common.Track is null ? null : (byte)Math.Min(common.Track.Value, 255u);

            var merged = new Id3v1TagData
            {
                Title = _NullIfEmpty(common.Title),
                Artist = _NullIfEmpty(artist),
                Album = _NullIfEmpty(common.Album),
                Year = common.Year,
                Comment = _NullIfEmpty(common.Comment),
                Track = track,
                Genre = genreByte,
            };

            return _IsId3v1Empty(merged) ? null : merged;
        }

        private static Id3v2TagData? _MergeId3v2(Id3v2TagData existing, SemanticAudioTag common)
        {
            var frames = existing.Frames.ToList();
            _SetSingleton(frames, "TIT2", common.Title);
            _SetSingleton(frames, "TALB", common.Album);
            _SetList(frames, "TPE1", common.Performers);
            _SetList(frames, "TPE2", common.AlbumArtists);
            _SetList(frames, "TCOM", common.Composers);
            _SetSingleton(frames, "TCON", common.Genre);
            _SetSingleton(frames, "TCOP", common.Copyright);
            _SetSingleton(frames, "TIT1", common.Grouping);
            _SetPrimaryMulti(frames, "COMM", common.Comment);
            _SetPrimaryMulti(frames, "USLT", common.Lyrics);
            _SetYear(frames, existing.Version, common.Year);
            _SetTrackPair(frames, "TRCK", common.Track, common.TrackCount);
            _SetTrackPair(frames, "TPOS", common.Disc, common.DiscCount);

            frames.Sort(_CompareId3v2Frames);
            // Preserve an intentionally empty Id3v2 block (create/recommended target) until fields are set or the
            // block is explicitly nulled by a remover. Prune only when the prior snapshot already had modeled frames
            // and this merge cleared them all.
            if (frames.Count == 0 && existing.Frames.Length > 0)
                return null;

            return new Id3v2TagData { Version = existing.Version, Frames = [.. frames] };
        }

        private static XiphTagData? _MergeXiph(XiphTagData existing, SemanticAudioTag common)
        {
            var map = _ToMutableMultimap(existing.Fields);
            _SetMapScalar(map, "TITLE", common.Title);
            _SetMapScalar(map, "ALBUM", common.Album);
            _SetMapList(map, "ARTIST", common.Performers);
            _SetMapList(map, "ALBUMARTIST", common.AlbumArtists);
            _SetMapList(map, "COMPOSER", common.Composers);
            _SetMapScalar(map, "GENRE", common.Genre);
            _SetMapScalar(map, "DESCRIPTION", common.Comment);
            map.Remove("COMMENT");
            _SetMapScalar(map, "LYRICS", common.Lyrics);
            map.Remove("UNSYNCEDLYRICS");
            _SetMapScalar(map, "COPYRIGHT", common.Copyright);
            _SetMapScalar(map, "GROUPING", common.Grouping);
            map.Remove("CONTENTGROUP");
            _SetMapScalar(map, "DATE", common.Year?.ToString(CultureInfo.InvariantCulture));
            map.Remove("YEAR");
            _SetMapScalar(map, "TRACKNUMBER", common.Track?.ToString(CultureInfo.InvariantCulture));
            _SetMapScalar(map, "TRACKTOTAL", common.TrackCount?.ToString(CultureInfo.InvariantCulture));
            map.Remove("TOTALTRACKS");
            _SetMapScalar(map, "DISCNUMBER", common.Disc?.ToString(CultureInfo.InvariantCulture));
            _SetMapScalar(map, "DISCTOTAL", common.DiscCount?.ToString(CultureInfo.InvariantCulture));
            map.Remove("TOTALDISCS");

            var rows = _SortedRows(map);
            return rows.Length == 0 ? null : new XiphTagData { Fields = rows };
        }

        private static ApeTagData? _MergeApe(ApeTagData existing, SemanticAudioTag common)
        {
            var map = _ToMutableMultimap(existing.Fields);
            _SetMapScalar(map, "Title", common.Title);
            _SetMapScalar(map, "Album", common.Album);
            _SetMapList(map, "Artist", common.Performers);
            _SetMapList(map, "Album Artist", common.AlbumArtists);
            _SetMapList(map, "Composer", common.Composers);
            _SetMapScalar(map, "Genre", common.Genre);
            _SetMapScalar(map, "Comment", common.Comment);
            _SetMapScalar(map, "Lyrics", common.Lyrics);
            _SetMapScalar(map, "Copyright", common.Copyright);
            _SetMapScalar(map, "Grouping", common.Grouping);
            _SetMapScalar(map, "Year", common.Year?.ToString(CultureInfo.InvariantCulture));
            _SetMapScalar(map, "Track", common.Track?.ToString(CultureInfo.InvariantCulture));
            _SetMapScalar(map, "TrackCount", common.TrackCount?.ToString(CultureInfo.InvariantCulture));
            _SetMapScalar(map, "Disc", common.Disc?.ToString(CultureInfo.InvariantCulture));
            _SetMapScalar(map, "DiscCount", common.DiscCount?.ToString(CultureInfo.InvariantCulture));

            var rows = _SortedRows(map);
            return rows.Length == 0 ? null : new ApeTagData { Fields = rows };
        }

        private static RiffInfoTagData? _MergeRiff(SemanticAudioTag common)
        {
            var rows = _RiffRowsFromCommon(common);
            return rows.Length == 0 ? null : new RiffInfoTagData { Fields = rows };
        }

        private static AsfTagData? _MergeAsf(AsfTagData existing, SemanticAudioTag common)
        {
            var rows = existing.Descriptors.ToList();
            _SetAsf(rows, "WM/Title", common.Title);
            _SetAsf(rows, "WM/AlbumTitle", common.Album);
            _SetAsf(rows, "WM/Author", _FirstListItem(common.Performers));
            _SetAsf(rows, "WM/AlbumArtist", _FirstListItem(common.AlbumArtists) ?? common.AlbumArtists);
            _SetAsf(rows, "WM/Composer", _FirstListItem(common.Composers) ?? common.Composers);
            _SetAsf(rows, "WM/Genre", common.Genre);
            _SetAsf(rows, "WM/Description", common.Comment);
            _SetAsf(rows, "WM/Lyrics", common.Lyrics);
            _SetAsf(rows, "WM/ProviderCopyright", common.Copyright);
            _SetAsf(rows, "WM/ContentGroupDescription", common.Grouping);
            _SetAsf(rows, "WM/Year", common.Year?.ToString(CultureInfo.InvariantCulture));
            _SetAsf(rows, "WM/TrackNumber", common.Track?.ToString(CultureInfo.InvariantCulture));
            _SetAsf(rows, "WM/TrackTotal", common.TrackCount?.ToString(CultureInfo.InvariantCulture));
            _SetAsf(rows, "WM/PartOfSet", common.Disc?.ToString(CultureInfo.InvariantCulture));
            _SetAsf(rows, "WM/TotalDiscs", common.DiscCount?.ToString(CultureInfo.InvariantCulture));

            if (rows.Count == 0)
                return null;

            rows.Sort(static (a, b) =>
            {
                var byName = string.CompareOrdinal(a.Name, b.Name);
                return byName != 0 ? byName : string.CompareOrdinal(a.Value, b.Value);
            });

            return new AsfTagData { Descriptors = [.. rows] };
        }

        private static AppleTagData? _MergeApple(AppleTagData existing, SemanticAudioTag common)
        {
            var atoms = existing.Atoms.ToList();
            _SetAppleAtom(atoms, AppleAtomIds.Title, common.Title);
            _SetAppleAtom(atoms, AppleAtomIds.Album, common.Album);
            _SetAppleAtomList(atoms, AppleAtomIds.Artist, common.Performers);
            _SetAppleAtomList(atoms, AppleAtomIds.AlbumArtist, common.AlbumArtists);
            _SetAppleAtomList(atoms, AppleAtomIds.Composer, common.Composers);
            _SetAppleAtom(atoms, AppleAtomIds.Genre, common.Genre);
            _SetAppleAtom(atoms, AppleAtomIds.Comment, common.Comment);
            _SetAppleAtom(atoms, AppleAtomIds.Lyrics, common.Lyrics);
            _SetAppleAtom(atoms, AppleAtomIds.Copyright, common.Copyright);
            _SetAppleAtom(atoms, AppleAtomIds.Grouping, common.Grouping);
            _SetAppleAtom(atoms, AppleAtomIds.Day, common.Year?.ToString(CultureInfo.InvariantCulture));

            if (atoms.Count == 0)
                return null;

            atoms.Sort(static (a, b) =>
            {
                var byType = a.AtomType.AsSpan().SequenceCompareTo(b.AtomType.AsSpan());
                if (byType != 0)
                    return byType;

                return _CompareStringSeq(a.Values, b.Values);
            });

            return new AppleTagData { Atoms = [.. atoms] };
        }

        private static void _SetSingleton(List<Id3v2ModeledFrame> frames, string frameId, string? value)
        {
            frames.RemoveAll(f => string.Equals(f.FrameId, frameId, StringComparison.Ordinal));
            var text = _NullIfEmpty(value);
            if (text is null)
                return;

            frames.Add(new Id3v2ModeledFrame { FrameId = frameId, TextValues = [text] });
        }

        private static void _SetList(List<Id3v2ModeledFrame> frames, string frameId, string? joined)
        {
            frames.RemoveAll(f => string.Equals(f.FrameId, frameId, StringComparison.Ordinal));
            var values = _TrimNonEmpty(_SplitJoinedList(joined));
            if (values.Length == 0)
                return;

            frames.Add(new Id3v2ModeledFrame { FrameId = frameId, TextValues = values });
        }

        private static void _SetPrimaryMulti(List<Id3v2ModeledFrame> frames, string frameId, string? value)
        {
            var primaryIndex = frames.FindIndex(f =>
                string.Equals(f.FrameId, frameId, StringComparison.Ordinal)
                && string.IsNullOrEmpty(f.Description));

            var text = _NullIfEmpty(value);
            if (text is null)
            {
                if (primaryIndex >= 0)
                    frames.RemoveAt(primaryIndex);

                return;
            }

            var replacement = new Id3v2ModeledFrame
            {
                FrameId = frameId,
                Language = primaryIndex >= 0 ? frames[primaryIndex].Language : "eng",
                Description = null,
                TextValues = [text],
            };

            if (primaryIndex >= 0)
                frames[primaryIndex] = replacement;
            else
                frames.Add(replacement);
        }

        private static void _SetYear(List<Id3v2ModeledFrame> frames, byte version, uint? year)
        {
            frames.RemoveAll(f =>
                string.Equals(f.FrameId, "TYER", StringComparison.Ordinal)
                || string.Equals(f.FrameId, "TDRC", StringComparison.Ordinal));

            if (year is null)
                return;

            var frameId = version >= 4 ? "TDRC" : "TYER";
            frames.Add(new Id3v2ModeledFrame
            {
                FrameId = frameId,
                TextValues = [year.Value.ToString(CultureInfo.InvariantCulture)],
            });
        }

        private static void _SetTrackPair(List<Id3v2ModeledFrame> frames, string frameId, uint? number, uint? count)
        {
            frames.RemoveAll(f => string.Equals(f.FrameId, frameId, StringComparison.Ordinal));
            if (number is null && count is null)
                return;

            var text = number is null
                ? "0/" + count!.Value.ToString(CultureInfo.InvariantCulture)
                : count is null
                ? number.Value.ToString(CultureInfo.InvariantCulture)
                : number.Value.ToString(CultureInfo.InvariantCulture) + "/" + count.Value.ToString(CultureInfo.InvariantCulture);
            frames.Add(new Id3v2ModeledFrame { FrameId = frameId, TextValues = [text] });
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

        private static void _SetAsf(List<AsfDescriptorRow> rows, string name, string? value)
        {
            rows.RemoveAll(r => string.Equals(r.Name, name, StringComparison.Ordinal));
            var text = _NullIfEmpty(value);
            if (text is null)
                return;

            rows.Add(new AsfDescriptorRow(name, text));
        }

        private static void _SetAppleAtom(List<AppleAtomRow> atoms, ReadOnlySpan<byte> atomType, string? value)
        {
            var typeBytes = atomType.ToArray();
            atoms.RemoveAll(a => a.AtomType.AsSpan().SequenceEqual(typeBytes));
            var text = _NullIfEmpty(value);
            if (text is null)
                return;

            atoms.Add(new AppleAtomRow
            {
                AtomType = ImmutableArray.Create(typeBytes),
                Values = [text],
            });
        }

        private static void _SetAppleAtomList(List<AppleAtomRow> atoms, ReadOnlySpan<byte> atomType, string? joined)
        {
            var typeBytes = atomType.ToArray();
            atoms.RemoveAll(a => a.AtomType.AsSpan().SequenceEqual(typeBytes));
            var values = _TrimNonEmpty(_SplitJoinedList(joined));
            if (values.Length == 0)
                return;

            atoms.Add(new AppleAtomRow
            {
                AtomType = ImmutableArray.Create(typeBytes),
                Values = values,
            });
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

        private static bool _IsId3v1Empty(Id3v1TagData data)
        {
            return string.IsNullOrWhiteSpace(data.Title)
                && string.IsNullOrWhiteSpace(data.Artist)
                && string.IsNullOrWhiteSpace(data.Album)
                && data.Year is null
                && string.IsNullOrWhiteSpace(data.Comment)
                && data.Track is null
                && data.Genre == 0;
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

        private static string? _FirstListItem(string? joined)
        {
            var parts = _SplitJoinedList(joined);
            return parts.Length == 0 ? null : parts[0];
        }

        private static string? _NullIfEmpty(string? text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private static string? _EmptyStringToNull(string? text)
        {
            return string.IsNullOrEmpty(text) ? null : text;
        }

        private static class AppleAtomIds
        {
            public static ReadOnlySpan<byte> Title => [0xA9, (byte)'n', (byte)'a', (byte)'m'];
            public static ReadOnlySpan<byte> Album => [0xA9, (byte)'a', (byte)'l', (byte)'b'];
            public static ReadOnlySpan<byte> Artist => [0xA9, (byte)'A', (byte)'R', (byte)'T'];
            public static ReadOnlySpan<byte> AlbumArtist => [(byte)'a', (byte)'A', (byte)'R', (byte)'T'];
            public static ReadOnlySpan<byte> Composer => [0xA9, (byte)'w', (byte)'r', (byte)'t'];
            public static ReadOnlySpan<byte> Genre => [0xA9, (byte)'g', (byte)'e', (byte)'n'];
            public static ReadOnlySpan<byte> Comment => [0xA9, (byte)'c', (byte)'m', (byte)'t'];
            public static ReadOnlySpan<byte> Lyrics => [0xA9, (byte)'l', (byte)'y', (byte)'r'];
            public static ReadOnlySpan<byte> Copyright => [(byte)'c', (byte)'p', (byte)'r', (byte)'t'];
            public static ReadOnlySpan<byte> Grouping => [0xA9, (byte)'g', (byte)'r', (byte)'p'];
            public static ReadOnlySpan<byte> Day => [0xA9, (byte)'d', (byte)'a', (byte)'y'];
        }
    }
}

using System.Collections.Immutable;
using System.Globalization;
using Mfr.Models.Tags.Id3v1;
using Mfr.Models.Tags.Id3v2;
using Mfr.Models.Tags.Xiph;
using Mfr.Utils;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// Reads and writes format-specific tag fields on <see cref="AudioTagOverlay"/> blocks (ID3v1 scalars, ID3v2 frames, Xiph keys).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Callers must capability-check the container before writing. Empty / whitespace clears the addressed field
    /// (absent map entry / null scalar); an empty modeled block is pruned to <see langword="null"/>.
    /// </para>
    /// </remarks>
    public static class AudioOverlayBlockFieldIo
    {
        /// <summary>
        /// Returns the filter/preview string for an ID3v1 scalar.
        /// </summary>
        /// <param name="overlay">Overlay whose <see cref="AudioTagOverlay.Id3v1"/> block is read.</param>
        /// <param name="field">Which scalar to read.</param>
        /// <returns>Text or decimal digits; empty when the block or field is absent.</returns>
        public static string GetId3v1FieldString(AudioTagOverlay overlay, Id3v1Field field)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            var block = overlay.Id3v1;
            if (block is null)
                return string.Empty;

            return field switch
            {
                Id3v1Field.Title => block.Title ?? string.Empty,
                Id3v1Field.Artist => block.Artist ?? string.Empty,
                Id3v1Field.Album => block.Album ?? string.Empty,
                Id3v1Field.Comment => block.Comment ?? string.Empty,
                Id3v1Field.Year => _DecimalDigitsOrEmpty(block.Year),
                Id3v1Field.Track => block.Track is null
                    ? string.Empty
                    : block.Track.Value.ToString(CultureInfo.InvariantCulture),
                Id3v1Field.Genre => block.Genre == 0
                    ? string.Empty
                    : Id3v1Genres.IndexToAudio(block.Genre) ?? string.Empty,
                _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
            };
        }

        /// <summary>
        /// Parses <paramref name="fieldString"/> into an ID3v1 scalar on <paramref name="overlay"/> (creates the block when needed).
        /// </summary>
        /// <param name="overlay">Overlay whose ID3v1 block is updated.</param>
        /// <param name="field">Which scalar to replace.</param>
        /// <param name="fieldString">Text as-is, or decimal digits for year/track; empty clears.</param>
        /// <exception cref="ArgumentException">Thrown when year/track text is not empty and not a valid integer in range.</exception>
        public static void SetId3v1FieldString(AudioTagOverlay overlay, Id3v1Field field, string fieldString)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            var trimmed = fieldString.Trim();
            overlay.EnsureEmptyBlock(AudioTagBlockKind.Id3v1);
            var existing = overlay.Id3v1!;

            var updated = field switch
            {
                Id3v1Field.Title => existing with { Title = trimmed.TrimmedOrNull() },
                Id3v1Field.Artist => existing with { Artist = trimmed.TrimmedOrNull() },
                Id3v1Field.Album => existing with { Album = trimmed.TrimmedOrNull() },
                Id3v1Field.Comment => existing with { Comment = trimmed.TrimmedOrNull() },
                Id3v1Field.Year => existing with { Year = _ParseNullableUInt(trimmed, max: 9999, nameof(fieldString)) },
                Id3v1Field.Track => existing with { Track = _ParseNullableByte(trimmed, nameof(fieldString)) },
                Id3v1Field.Genre => existing with { Genre = _ParseGenreByte(trimmed) },
                _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
            };

            overlay.Id3v1 = _IsId3v1Empty(updated) ? null : updated;
        }

        /// <summary>
        /// Returns the text of the modeled ID3v2 frame matching <paramref name="frameId"/> / language / description.
        /// </summary>
        /// <param name="overlay">Overlay whose <see cref="AudioTagOverlay.Id3v2"/> block is read.</param>
        /// <param name="frameId">Four-character frame id.</param>
        /// <param name="language">Optional language for multi-instance frames.</param>
        /// <param name="description">Optional description for multi-instance frames; omit for primary <c>COMM</c>/<c>USLT</c>.</param>
        /// <returns>Joined text values (<c>; </c>); empty when absent.</returns>
        public static string GetId3v2FrameString(
            AudioTagOverlay overlay,
            string frameId,
            string? language = null,
            string? description = null
        )
        {
            ArgumentNullException.ThrowIfNull(overlay);
            ArgumentException.ThrowIfNullOrWhiteSpace(frameId);

            var block = overlay.Id3v2;
            if (block is null)
                return string.Empty;

            var normalizedId = frameId.Trim().ToUpperInvariant();
            var frame = _FindFrame(block.Frames, normalizedId, language, description);
            return frame is null ? string.Empty : DelimitedText.Join(frame.TextValues);
        }

        /// <summary>
        /// Sets or clears one modeled ID3v2 frame instance (creates an ID3v2 v2.3 block when needed).
        /// </summary>
        /// <param name="overlay">Overlay whose ID3v2 block is updated.</param>
        /// <param name="frameId">Four-character frame id.</param>
        /// <param name="fieldString">Text, or <c>;</c>-separated list values; empty removes that instance only.</param>
        /// <param name="language">Optional language for multi-instance frames.</param>
        /// <param name="description">Optional description for multi-instance frames.</param>
        /// <exception cref="NotSupportedException">
        /// Thrown when writing a v2.4-only frame into a tag whose version is below 2.4
        /// (<see cref="Id3v2FrameVersionPolicy"/>).
        /// </exception>
        public static void SetId3v2FrameString(
            AudioTagOverlay overlay,
            string frameId,
            string fieldString,
            string? language = null,
            string? description = null
        )
        {
            ArgumentNullException.ThrowIfNull(overlay);
            ArgumentException.ThrowIfNullOrWhiteSpace(frameId);

            var normalizedId = frameId.Trim().ToUpperInvariant();
            var trimmed = fieldString.Trim();
            overlay.EnsureEmptyBlock(AudioTagBlockKind.Id3v2);
            var existing = overlay.Id3v2!;
            var frames = existing.Frames.ToList();

            _RemoveMatchingFrames(frames, normalizedId, language, description);

            if (trimmed.Length > 0)
            {
                Id3v2FrameVersionPolicy.EnsureCompatible(existing.Version, normalizedId);

                var values = DelimitedText.Split(trimmed);
                if (values.Length > 0)
                {
                    frames.Add(
                        new Id3v2ModeledFrame
                        {
                            FrameId = normalizedId,
                            Language = _ResolveLanguageForWrite(normalizedId, language, description),
                            Description = _NormalizeDescription(description),
                            TextValues = values,
                        }
                    );
                }
            }

            frames.Sort(_CompareFrames);
            overlay.Id3v2 =
                frames.Count == 0 ? null : new Id3v2TagData { Version = existing.Version, Frames = [.. frames] };
        }

        /// <summary>
        /// Returns the joined values for a Xiph comment key (case-insensitive).
        /// </summary>
        /// <param name="overlay">Overlay whose <see cref="AudioTagOverlay.Xiph"/> block is read.</param>
        /// <param name="key">Comment field key.</param>
        /// <returns>Joined values (<c>; </c>); empty when absent.</returns>
        public static string GetXiphFieldString(AudioTagOverlay overlay, string key)
        {
            ArgumentNullException.ThrowIfNull(overlay);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            var block = overlay.Xiph;
            if (block is null)
                return string.Empty;

            var normalizedKey = key.Trim().ToUpperInvariant();
            foreach (var row in block.Fields)
            {
                if (!string.Equals(row.Key, normalizedKey, StringComparison.Ordinal))
                    continue;

                return DelimitedText.Join(row.Values);
            }

            return string.Empty;
        }

        /// <summary>
        /// Sets or clears one Xiph comment key (creates an empty Xiph block when needed).
        /// </summary>
        /// <param name="overlay">Overlay whose Xiph block is updated.</param>
        /// <param name="key">Comment field key (stored uppercase).</param>
        /// <param name="fieldString">Text, or <c>;</c>-separated list values; empty removes the key.</param>
        public static void SetXiphFieldString(AudioTagOverlay overlay, string key, string fieldString)
        {
            ArgumentNullException.ThrowIfNull(overlay);
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            var normalizedKey = key.Trim().ToUpperInvariant();
            var trimmed = fieldString.Trim();
            overlay.EnsureEmptyBlock(AudioTagBlockKind.Xiph);
            var existing = overlay.Xiph!;
            var rows = existing
                .Fields.Where(r => !string.Equals(r.Key, normalizedKey, StringComparison.Ordinal))
                .ToList();

            if (trimmed.Length > 0)
            {
                var values = DelimitedText.Split(trimmed);
                if (values.Length > 0)
                    rows.Add(new TextFieldRow(normalizedKey, values));
            }

            rows.Sort(
                static (a, b) =>
                {
                    var byKey = string.CompareOrdinal(a.Key, b.Key);
                    return byKey != 0
                        ? byKey
                        : string.CompareOrdinal(string.Join('\0', a.Values), string.Join('\0', b.Values));
                }
            );

            overlay.Xiph = rows.Count == 0 ? null : new XiphTagData { Fields = [.. rows] };
        }

        private static Id3v2ModeledFrame? _FindFrame(
            ImmutableArray<Id3v2ModeledFrame> frames,
            string frameId,
            string? language,
            string? description
        )
        {
            foreach (var frame in frames)
            {
                if (_FrameMatches(frame, frameId, language, description))
                    return frame;
            }

            return null;
        }

        private static void _RemoveMatchingFrames(
            List<Id3v2ModeledFrame> frames,
            string frameId,
            string? language,
            string? description
        )
        {
            frames.RemoveAll(f => _FrameMatches(f, frameId, language, description));
        }

        private static bool _FrameMatches(
            Id3v2ModeledFrame frame,
            string frameId,
            string? language,
            string? description
        )
        {
            if (!string.Equals(frame.FrameId, frameId, StringComparison.Ordinal))
                return false;

            if (!Id3v2ModeledFrame.MultiInstanceFrameIds.Contains(frameId))
                return true;

            if (string.Equals(frameId, "TXXX", StringComparison.Ordinal))
                return _SameOptionalText(frame.Description, description);

            // COMM / USLT: primary = empty description when target description is omitted.
            if (!_SameOptionalText(frame.Description, description))
                return false;

            if (string.IsNullOrWhiteSpace(language))
                return true;

            return string.Equals(frame.Language, language.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool _SameOptionalText(string? left, string? right)
        {
            var a = string.IsNullOrWhiteSpace(left) ? null : left.Trim();
            var b = string.IsNullOrWhiteSpace(right) ? null : right.Trim();
            return string.Equals(a, b, StringComparison.Ordinal);
        }

        private static string? _ResolveLanguageForWrite(string frameId, string? language, string? description)
        {
            if (string.Equals(frameId, "TXXX", StringComparison.Ordinal))
                return null;

            if (!Id3v2ModeledFrame.MultiInstanceFrameIds.Contains(frameId))
                return null;

            if (!string.IsNullOrWhiteSpace(language))
                return language.Trim();

            // Primary COMM/USLT create uses eng when language omitted.
            if (string.IsNullOrWhiteSpace(description))
                return "eng";

            return null;
        }

        private static string? _NormalizeDescription(string? description)
        {
            return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        }

        private static int _CompareFrames(Id3v2ModeledFrame a, Id3v2ModeledFrame b)
        {
            var byId = string.CompareOrdinal(a.FrameId, b.FrameId);
            if (byId != 0)
                return byId;

            var byLang = string.CompareOrdinal(a.Language ?? string.Empty, b.Language ?? string.Empty);
            if (byLang != 0)
                return byLang;

            var byDesc = string.CompareOrdinal(a.Description ?? string.Empty, b.Description ?? string.Empty);
            if (byDesc != 0)
                return byDesc;

            return string.CompareOrdinal(string.Join('\0', a.TextValues), string.Join('\0', b.TextValues));
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

        private static byte _ParseGenreByte(string trimmed)
        {
            if (trimmed.Length == 0)
                return 0;

            if (byte.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
                return index;

            return Id3v1Genres.AudioToIndex(trimmed);
        }

        private static uint? _ParseNullableUInt(string trimmed, uint max, string valueParamName)
        {
            if (trimmed.Length == 0)
                return null;

            if (!uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new ArgumentException(
                    $"Value must be empty or a non-negative integer, got '{trimmed}'.",
                    valueParamName
                );
            }

            if (parsed > max)
            {
                throw new ArgumentException($"Value must be between 0 and {max}, got {parsed}.", valueParamName);
            }

            return parsed == 0 ? null : parsed;
        }

        private static byte? _ParseNullableByte(string trimmed, string valueParamName)
        {
            if (trimmed.Length == 0)
                return null;

            if (!byte.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new ArgumentException(
                    $"Value must be empty or an integer 0-255, got '{trimmed}'.",
                    valueParamName
                );
            }

            return parsed == 0 ? null : parsed;
        }

        private static string _DecimalDigitsOrEmpty(uint? value)
        {
            return value is null ? string.Empty : value.Value.ToString(CultureInfo.InvariantCulture);
        }
    }
}

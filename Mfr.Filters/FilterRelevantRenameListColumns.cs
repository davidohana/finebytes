using Mfr.Filters.Attributes;
using Mfr.Filters.Audio;
using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.FormatString;
using Mfr.Filters.Misc;
using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;
using Mfr.Models.RenameList.Fields.Id3v2;
using Mfr.Models.RenameList.Fields.Xiph;
using Mfr.Models.Tags;

namespace Mfr.Filters
{
    /// <summary>
    /// Infers Rename List columns relevant to a Filter Chain (write targets and format tokens).
    /// </summary>
    public static class FilterRelevantRenameListColumns
    {
        private static readonly Dictionary<FilterTarget, RenameListField> _writeTargetToField = _BuildWriteTargetMap();

        /// <summary>
        /// Collects ordered unique Rename List field keys that the filter chain writes or reads via tokens.
        /// </summary>
        /// <param name="filters">Filter Chain filters in order (disabled steps included).</param>
        /// <returns>
        /// Stable unique keys: chain order, write keys before token keys per filter, Original before Preview.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="filters"/> is null.</exception>
        public static IReadOnlyList<RenameListFieldKey> Collect(IEnumerable<BaseFilter> filters)
        {
            ArgumentNullException.ThrowIfNull(filters);

            var keys = new List<RenameListFieldKey>();
            var keyToIsSeen = new HashSet<RenameListFieldKey>();

            foreach (var filter in filters)
            {
                ArgumentNullException.ThrowIfNull(filter);

                _CollectWriteKeys(filter, keys, keyToIsSeen);
                _CollectTokenKeys(filter, keys, keyToIsSeen);
            }

            return keys;
        }

        /// <summary>
        /// Adds write-side catalog keys for <paramref name="filter"/>.
        /// </summary>
        private static void _CollectWriteKeys(
            BaseFilter filter,
            List<RenameListFieldKey> keys,
            HashSet<RenameListFieldKey> keyToIsSeen
        )
        {
            if (filter is StringTargetFilter stringTarget)
            {
                _AddFieldForWriteTarget(stringTarget.Target, keys, keyToIsSeen);
                return;
            }

            if (filter is AudioTagSetterFilter audioTagSetter)
            {
                _AddAudioTagSetterWriteKeys(audioTagSetter.Options, keys, keyToIsSeen);
                return;
            }

            if (filter is Id3v2FieldSetterFilter id3v2FieldSetter)
            {
                _AddId3v2FrameWriteKey(id3v2FieldSetter.Options.FrameId, keys, keyToIsSeen);
                return;
            }

            if (filter is DateTimeSetterFilter dateTimeSetter)
            {
                _AddTimestampWriteKey(dateTimeSetter.Options.TimestampField, keys, keyToIsSeen);
                return;
            }

            if (filter is TimeShifterFilter timeShifter)
            {
                _AddTimestampWriteKey(timeShifter.Options.TimestampField, keys, keyToIsSeen);
                return;
            }

            if (filter is AttributesSetterFilter)
            {
                _AddCatalogField(ExtendedRenameListFields.Group, ExtendedRenameListFields.Key.Attrs, keys, keyToIsSeen);
                return;
            }

            if (filter is PathMoverFilter)
            {
                _AddCatalogField(BasicRenameListField.Group, BasicRenameListFields.Key.Folder, keys, keyToIsSeen);
            }
        }

        /// <summary>
        /// Adds keys for non-null <see cref="AudioTagSetterOptions"/> semantic slots.
        /// </summary>
        private static void _AddAudioTagSetterWriteKeys(
            AudioTagSetterOptions options,
            List<RenameListFieldKey> keys,
            HashSet<RenameListFieldKey> keyToIsSeen
        )
        {
            foreach (var (propertyKey, _) in _EnumerateAudioTagSetterSlots(options))
            {
                _AddCatalogField(AudioTagRenameListFields.Group, propertyKey, keys, keyToIsSeen);
            }
        }

        /// <summary>
        /// Adds the Extended date column for a filesystem timestamp field.
        /// </summary>
        private static void _AddTimestampWriteKey(
            TimestampField timestampField,
            List<RenameListFieldKey> keys,
            HashSet<RenameListFieldKey> keyToIsSeen
        )
        {
            FormatTokenRenameListFieldMap.TryMapTimestampField(timestampField, out var groupId, out var propertyKey);
            _AddCatalogField(groupId, propertyKey, keys, keyToIsSeen);
        }

        /// <summary>
        /// Parses format templates on <paramref name="filter"/> and maps known tokens to catalog keys.
        /// </summary>
        private static void _CollectTokenKeys(
            BaseFilter filter,
            List<RenameListFieldKey> keys,
            HashSet<RenameListFieldKey> keyToIsSeen
        )
        {
            foreach (var template in _EnumerateFormatTemplates(filter))
            {
                _CollectTokensFromTemplate(template, keys, keyToIsSeen);
            }
        }

        /// <summary>
        /// Yields format-template option strings known for <paramref name="filter"/>.
        /// </summary>
        private static IEnumerable<string> _EnumerateFormatTemplates(BaseFilter filter)
        {
            if (filter is FormatterFilter formatter)
            {
                yield return formatter.Options.Template;
                yield break;
            }

            if (filter is InserterFilter inserter)
            {
                yield return inserter.Options.Text;
                yield break;
            }

            if (filter is NameListFilter nameList)
            {
                yield return nameList.Options.Prefix;
                yield return nameList.Options.Suffix;
                yield break;
            }

            if (filter is PathMoverFilter pathMover)
            {
                yield return pathMover.Options.SubFolder;
                yield break;
            }

            if (filter is Id3v2FieldSetterFilter id3v2FieldSetter)
            {
                yield return id3v2FieldSetter.Options.Text;
                yield break;
            }

            if (filter is AudioTagSetterFilter audioTagSetter)
            {
                foreach (var text in _EnumerateAudioTagSetterTexts(audioTagSetter.Options))
                {
                    yield return text;
                }
            }
        }

        /// <summary>
        /// Yields non-null Audio Tag Setter field texts.
        /// </summary>
        private static IEnumerable<string> _EnumerateAudioTagSetterTexts(AudioTagSetterOptions options)
        {
            foreach (var (_, field) in _EnumerateAudioTagSetterSlots(options))
            {
                yield return field.Text;
            }
        }

        /// <summary>
        /// Yields non-null Audio Tag Setter slots as catalog property key + field options.
        /// </summary>
        private static IEnumerable<(
            string PropertyKey,
            AudioTagStringFieldOptions Field
        )> _EnumerateAudioTagSetterSlots(AudioTagSetterOptions options)
        {
            foreach (var (semanticField, field) in _EnumerateAudioTagSetterSemanticSlots(options))
            {
                if (!AudioTagRenameListFields.TryGetSemanticPropertyKey(semanticField, out var propertyKey))
                {
                    continue;
                }

                yield return (propertyKey, field);
            }
        }

        /// <summary>
        /// Yields non-null Audio Tag Setter slots as semantic field + field options.
        /// </summary>
        private static IEnumerable<(
            SemanticAudioField SemanticField,
            AudioTagStringFieldOptions Field
        )> _EnumerateAudioTagSetterSemanticSlots(AudioTagSetterOptions options)
        {
            if (options.Performers is not null)
            {
                yield return (SemanticAudioField.Performers, options.Performers);
            }

            if (options.AlbumArtists is not null)
            {
                yield return (SemanticAudioField.AlbumArtists, options.AlbumArtists);
            }

            if (options.Title is not null)
            {
                yield return (SemanticAudioField.Title, options.Title);
            }

            if (options.Album is not null)
            {
                yield return (SemanticAudioField.Album, options.Album);
            }

            if (options.Genre is not null)
            {
                yield return (SemanticAudioField.Genre, options.Genre);
            }

            if (options.Comment is not null)
            {
                yield return (SemanticAudioField.Comment, options.Comment);
            }

            if (options.Composers is not null)
            {
                yield return (SemanticAudioField.Composers, options.Composers);
            }

            if (options.Lyrics is not null)
            {
                yield return (SemanticAudioField.Lyrics, options.Lyrics);
            }

            if (options.Grouping is not null)
            {
                yield return (SemanticAudioField.Grouping, options.Grouping);
            }

            if (options.Copyright is not null)
            {
                yield return (SemanticAudioField.Copyright, options.Copyright);
            }

            if (options.Conductor is not null)
            {
                yield return (SemanticAudioField.Conductor, options.Conductor);
            }

            if (options.Year is not null)
            {
                yield return (SemanticAudioField.Year, options.Year);
            }

            if (options.BeatsPerMinute is not null)
            {
                yield return (SemanticAudioField.BeatsPerMinute, options.BeatsPerMinute);
            }

            if (options.Track is not null)
            {
                yield return (SemanticAudioField.Track, options.Track);
            }

            if (options.TrackCount is not null)
            {
                yield return (SemanticAudioField.TrackCount, options.TrackCount);
            }

            if (options.Disc is not null)
            {
                yield return (SemanticAudioField.Disc, options.Disc);
            }

            if (options.DiscCount is not null)
            {
                yield return (SemanticAudioField.DiscCount, options.DiscCount);
            }
        }

        /// <summary>
        /// Validates <paramref name="template"/> and appends mapped token fields (including nested arg tokens).
        /// </summary>
        private static void _CollectTokensFromTemplate(
            string template,
            List<RenameListFieldKey> keys,
            HashSet<RenameListFieldKey> keyToIsSeen
        )
        {
            if (string.IsNullOrEmpty(template))
            {
                return;
            }

            var parse = FormatStringSyntax.TryValidate(template);
            foreach (var span in parse.Tokens)
            {
                if (
                    FormatTokenRenameListFieldMap.TryMap(
                        span.CanonicalName,
                        span.Args,
                        out var groupId,
                        out var propertyKey
                    )
                )
                {
                    _AddCatalogField(groupId, propertyKey, keys, keyToIsSeen);
                }

                // Nested tokens live in args (e.g. substr/token source=) and are not top-level spans.
                if (!string.IsNullOrEmpty(span.Args) && FormatStringCompiler.ContainsLikelyFormatTokens(span.Args))
                {
                    _CollectTokensFromTemplate(span.Args, keys, keyToIsSeen);
                }
            }
        }

        /// <summary>
        /// Reverse-looks up a catalog write target and appends Original (+ Preview when supported).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="Id3v2FrameTarget"/> maps by normalized frame id only (see
        /// <see cref="_AddId3v2FrameWriteKey"/>) so language/description instances still hit the
        /// primary COMM/USLT/TXXX catalog column — not MediaTag, and not a dictionary equality miss.
        /// <see cref="XiphFieldTarget"/> maps by uppercase key so mixed-case presets still hit the
        /// catalog column (apply I/O is case-insensitive; record equality is not).
        /// </para>
        /// </remarks>
        private static void _AddFieldForWriteTarget(
            FilterTarget target,
            List<RenameListFieldKey> keys,
            HashSet<RenameListFieldKey> keyToIsSeen
        )
        {
            if (target is Id3v2FrameTarget id3v2)
            {
                _AddId3v2FrameWriteKey(id3v2.FrameId, keys, keyToIsSeen);
                return;
            }

            if (target is XiphFieldTarget xiph)
            {
                _AddXiphFieldWriteKey(xiph.Key, keys, keyToIsSeen);
                return;
            }

            if (!_writeTargetToField.TryGetValue(target, out var field))
            {
                return;
            }

            _AddFieldKeys(field, keys, keyToIsSeen);
        }

        /// <summary>
        /// Appends the primary ID3v2 catalog column for <paramref name="frameId"/> (Original + Preview).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Used by string <see cref="Id3v2FrameTarget"/> Apply-To and by
        /// <see cref="Id3v2FieldSetterFilter"/> (not a <see cref="StringTargetFilter"/>). Multi-instance
        /// language/description frames share one COMM/USLT/TXXX column. Unmodeled ids are skipped via
        /// catalog lookup.
        /// </para>
        /// </remarks>
        private static void _AddId3v2FrameWriteKey(
            string frameId,
            List<RenameListFieldKey> keys,
            HashSet<RenameListFieldKey> keyToIsSeen
        )
        {
            if (string.IsNullOrWhiteSpace(frameId))
            {
                return;
            }

            _AddCatalogField(Id3v2RenameListFields.Group, frameId.Trim().ToUpperInvariant(), keys, keyToIsSeen);
        }

        /// <summary>
        /// Appends the Xiph catalog column for <paramref name="key"/> (Original + Preview).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Unknown / blank keys are skipped via catalog lookup. Keys are uppercased to match
        /// Xiph property keys and Apply-To I/O normalization.
        /// </para>
        /// </remarks>
        private static void _AddXiphFieldWriteKey(
            string key,
            List<RenameListFieldKey> keys,
            HashSet<RenameListFieldKey> keyToIsSeen
        )
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            _AddCatalogField(XiphRenameListFields.Group, key.Trim().ToUpperInvariant(), keys, keyToIsSeen);
        }

        /// <summary>
        /// Looks up a catalog field and appends Original (+ Preview when supported).
        /// </summary>
        private static void _AddCatalogField(
            string groupId,
            string propertyKey,
            List<RenameListFieldKey> keys,
            HashSet<RenameListFieldKey> keyToIsSeen
        )
        {
            if (!RenameListFieldCatalog.TryGetField(groupId, propertyKey, out var field))
            {
                return;
            }

            _AddFieldKeys(field, keys, keyToIsSeen);
        }

        /// <summary>
        /// Appends Original, then Preview when <see cref="RenameListField.SupportsPreview"/>.
        /// </summary>
        private static void _AddFieldKeys(
            RenameListField field,
            List<RenameListFieldKey> keys,
            HashSet<RenameListFieldKey> keyToIsSeen
        )
        {
            _TryAdd(field.OriginalKey, keys, keyToIsSeen);
            if (field.SupportsPreview)
            {
                _TryAdd(field.PreviewKey, keys, keyToIsSeen);
            }
        }

        /// <summary>
        /// Appends <paramref name="key"/> when not already present.
        /// </summary>
        private static void _TryAdd(
            RenameListFieldKey key,
            List<RenameListFieldKey> keys,
            HashSet<RenameListFieldKey> keyToIsSeen
        )
        {
            if (!keyToIsSeen.Add(key))
            {
                return;
            }

            keys.Add(key);
        }

        /// <summary>
        /// Builds the reverse map from catalog <see cref="RenameListField.WriteTarget"/> values.
        /// </summary>
        private static Dictionary<FilterTarget, RenameListField> _BuildWriteTargetMap()
        {
            var targetToField = new Dictionary<FilterTarget, RenameListField>();
            foreach (var field in RenameListFieldCatalog.All)
            {
                if (field.WriteTarget is not { } writeTarget)
                {
                    continue;
                }

                // First catalog hit wins when targets collide (none today).
                targetToField.TryAdd(writeTarget, field);
            }

            return targetToField;
        }
    }
}

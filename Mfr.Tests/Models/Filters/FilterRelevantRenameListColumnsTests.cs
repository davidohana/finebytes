using Mfr.Filters;
using Mfr.Filters.Attributes;
using Mfr.Filters.Audio;
using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.FormatString;
using Mfr.Filters.Formatting.Tokens;
using Mfr.Filters.Formatting.Tokens.Exif;
using Mfr.Filters.Formatting.Tokens.Image;
using Mfr.Filters.Formatting.Tokens.Media;
using Mfr.Filters.Formatting.Tokens.Mpeg;
using Mfr.Filters.Misc;
using Mfr.Filters.Space;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;
using Mfr.Models.RenameList.Fields.Image;
using Mfr.Models.RenameList.Fields.Jpeg;
using Mfr.Models.RenameList.Fields.Media;
using Mfr.Models.RenameList.Fields.Mpeg;

namespace Mfr.Tests.Models.Filters
{
    /// <summary>
    /// Tests for <see cref="FilterRelevantRenameListColumns"/>.
    /// </summary>
    public sealed class FilterRelevantRenameListColumnsTests
    {
        /// <summary>
        /// Verifies an empty chain yields no keys.
        /// </summary>
        [Fact]
        public void Collect_EmptyChain_ReturnsEmpty()
        {
            Assert.Empty(FilterRelevantRenameListColumns.Collect([]));
        }

        /// <summary>
        /// Verifies StringTarget write maps to Original + Preview Name columns.
        /// </summary>
        [Fact]
        public void Collect_StringTarget_FileName_AddsNameOriginalAndPreview()
        {
            var keys = FilterRelevantRenameListColumns.Collect([new RemoveSpacesFilter(new FileNameTarget())]);

            Assert.Equal(
                [
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                ],
                keys
            );
        }

        /// <summary>
        /// Verifies unmapped Apply-To targets (e.g. Id3v2) are skipped silently.
        /// </summary>
        [Fact]
        public void Collect_StringTarget_UnmappedId3v2_SkipsWrites()
        {
            var keys = FilterRelevantRenameListColumns.Collect([new RemoveSpacesFilter(new Id3v2FrameTarget("TIT2"))]);

            Assert.Empty(keys);
        }

        /// <summary>
        /// Verifies AncestorFolder targets with no catalog write field are skipped.
        /// </summary>
        [Fact]
        public void Collect_StringTarget_UnmappedAncestorFolder_SkipsWrites()
        {
            var keys = FilterRelevantRenameListColumns.Collect([new RemoveSpacesFilter(new AncestorFolderTarget(1))]);

            Assert.Empty(keys);
        }

        /// <summary>
        /// Verifies Formatter write target then template tokens (write before token; Original before Preview).
        /// </summary>
        [Fact]
        public void Collect_Formatter_WriteThenTokens_OrdersKeys()
        {
            var filter = new FormatterFilter(
                new FileNameTarget(),
                new FormatterOptions("<audio-title> - <file-extension>")
            );

            var keys = FilterRelevantRenameListColumns.Collect([filter]);

            Assert.Equal(
                [
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Original(AudioTagRenameListFields.Group, AudioTagRenameListFields.Key.Title),
                    RenameListFieldKey.Preview(AudioTagRenameListFields.Group, AudioTagRenameListFields.Key.Title),
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Extension),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Extension),
                ],
                keys
            );
        }

        /// <summary>
        /// Verifies meta/session tokens are omitted while nested source tokens still map.
        /// </summary>
        [Fact]
        public void Collect_Formatter_CounterOmitted_NestedSubstrSourceMapped()
        {
            var filter = new FormatterFilter(
                new FileNameTarget(),
                new FormatterOptions("<counter>_<substr:start=1,end=3,source=<full-name>>")
            );

            var keys = FilterRelevantRenameListColumns.Collect([filter]);

            Assert.Equal(
                [
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
                ],
                keys
            );
        }

        /// <summary>
        /// Verifies <c>file-date</c> args map to the matching Extended date column.
        /// </summary>
        [Fact]
        public void Collect_Formatter_FileDateToken_MapsExtendedDateColumn()
        {
            var filter = new FormatterFilter(
                new FileNameTarget(),
                new FormatterOptions("<file-date:yyyy-MM-dd,creation>")
            );

            var keys = FilterRelevantRenameListColumns.Collect([filter]);

            Assert.Equal(
                [
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Original(
                        ExtendedRenameListFields.Group,
                        ExtendedRenameListFields.Key.CreationDate
                    ),
                    RenameListFieldKey.Preview(
                        ExtendedRenameListFields.Group,
                        ExtendedRenameListFields.Key.CreationDate
                    ),
                ],
                keys
            );
        }

        /// <summary>
        /// Verifies AudioTagSetter non-null semantic options map to Audio Tag columns.
        /// </summary>
        [Fact]
        public void Collect_AudioTagSetter_NonNullFields_AddsSemanticColumns()
        {
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(
                    Title: new AudioTagStringFieldOptions(Text: "x"),
                    Genre: new AudioTagStringFieldOptions(Text: "rock"),
                    Track: new AudioTagStringFieldOptions(Text: "1")
                )
            );

            var keys = FilterRelevantRenameListColumns.Collect([filter]);

            Assert.Equal(
                [
                    RenameListFieldKey.Original(AudioTagRenameListFields.Group, AudioTagRenameListFields.Key.Title),
                    RenameListFieldKey.Preview(AudioTagRenameListFields.Group, AudioTagRenameListFields.Key.Title),
                    RenameListFieldKey.Original(AudioTagRenameListFields.Group, AudioTagRenameListFields.Key.Genres),
                    RenameListFieldKey.Preview(AudioTagRenameListFields.Group, AudioTagRenameListFields.Key.Genres),
                    RenameListFieldKey.Original(AudioTagRenameListFields.Group, AudioTagRenameListFields.Key.Track),
                    RenameListFieldKey.Preview(AudioTagRenameListFields.Group, AudioTagRenameListFields.Key.Track),
                ],
                keys
            );
        }

        /// <summary>
        /// Verifies AudioTagSetter field texts contribute token-mapped keys after write keys.
        /// </summary>
        [Fact]
        public void Collect_AudioTagSetter_FieldTextTokens_AppendAfterWrites()
        {
            var filter = new AudioTagSetterFilter(
                new AudioTagSetterOptions(Title: new AudioTagStringFieldOptions(Text: "<file-name>"))
            );

            var keys = FilterRelevantRenameListColumns.Collect([filter]);

            Assert.Equal(
                [
                    RenameListFieldKey.Original(AudioTagRenameListFields.Group, AudioTagRenameListFields.Key.Title),
                    RenameListFieldKey.Preview(AudioTagRenameListFields.Group, AudioTagRenameListFields.Key.Title),
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                ],
                keys
            );
        }

        /// <summary>
        /// Verifies omitted AudioTagSetter fields and empty options add nothing.
        /// </summary>
        [Fact]
        public void Collect_AudioTagSetter_AllOmitted_ReturnsEmpty()
        {
            Assert.Empty(FilterRelevantRenameListColumns.Collect([new AudioTagSetterFilter()]));
        }

        /// <summary>
        /// Verifies DateTimeSetter maps the chosen timestamp to Extended Original + Preview.
        /// </summary>
        [Fact]
        public void Collect_DateTimeSetter_AddsTimestampColumns()
        {
            var filter = new DateTimeSetterFilter(
                new DateTimeSetterOptions(
                    TimestampField: TimestampField.LastAccess,
                    SetDate: true,
                    Date: new DateOnly(2024, 1, 1),
                    SetTime: false,
                    Time: default
                )
            );

            var keys = FilterRelevantRenameListColumns.Collect([filter]);

            Assert.Equal(
                [
                    RenameListFieldKey.Original(
                        ExtendedRenameListFields.Group,
                        ExtendedRenameListFields.Key.LastAccessDate
                    ),
                    RenameListFieldKey.Preview(
                        ExtendedRenameListFields.Group,
                        ExtendedRenameListFields.Key.LastAccessDate
                    ),
                ],
                keys
            );
        }

        /// <summary>
        /// Verifies AttributesSetter maps to the Extended attributes column.
        /// </summary>
        [Fact]
        public void Collect_AttributesSetter_AddsAttrsColumns()
        {
            var keys = FilterRelevantRenameListColumns.Collect([new AttributesSetterFilter()]);

            Assert.Equal(
                [
                    RenameListFieldKey.Original(ExtendedRenameListFields.Group, ExtendedRenameListFields.Key.Attrs),
                    RenameListFieldKey.Preview(ExtendedRenameListFields.Group, ExtendedRenameListFields.Key.Attrs),
                ],
                keys
            );
        }

        /// <summary>
        /// Verifies PathMover write Folder then SubFolder template tokens.
        /// </summary>
        [Fact]
        public void Collect_PathMover_FolderWriteThenSubFolderTokens()
        {
            var filter = new PathMoverFilter(new PathMoverOptions(RootFolder: @"C:\Out", SubFolder: "<file-name>"));

            var keys = FilterRelevantRenameListColumns.Collect([filter]);

            Assert.Equal(
                [
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Folder),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Folder),
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                ],
                keys
            );
        }

        /// <summary>
        /// Verifies chain order keeps earlier filter keys ahead of later ones; duplicates are dropped.
        /// </summary>
        [Fact]
        public void Collect_ChainOrder_DedupesAcrossFilters()
        {
            var first = new RemoveSpacesFilter(new FileNameTarget());
            var second = new FormatterFilter(new FileExtensionTarget(), new FormatterOptions("<file-name>"));

            var keys = FilterRelevantRenameListColumns.Collect([first, second]);

            Assert.Equal(
                [
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Extension),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Extension),
                ],
                keys
            );
        }

        /// <summary>
        /// Verifies ItemType has no preview side when mapped from a token.
        /// </summary>
        [Fact]
        public void Collect_Formatter_FileOrFolderToken_OriginalOnly()
        {
            var filter = new FormatterFilter(new FileNameTarget(), new FormatterOptions("<file-or-folder>"));

            var keys = FilterRelevantRenameListColumns.Collect([filter]);

            Assert.Equal(
                [
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.ItemType),
                ],
                keys
            );
        }

        /// <summary>
        /// Verifies Media / MPEG / Image / named-EXIF tokens map to Original-only catalog columns.
        /// </summary>
        /// <param name="template">Formatter template containing one or more tokens under test.</param>
        /// <param name="expectedGroupId">Catalog group for the first mapped token field.</param>
        /// <param name="expectedPropertyKey">Catalog property key for the first mapped token field.</param>
        [Theory]
        [InlineData("<media-duration>", MediaRenameListFields.Group, MediaRenameListFields.Key.Duration)]
        [InlineData("<mpeg-bitrate>", MpegRenameListFields.Group, MpegRenameListFields.Key.Bitrate)]
        [InlineData("<mpeg-encoding>", MpegRenameListFields.Group, MpegRenameListFields.Key.VBR)]
        [InlineData("<image-width>", ImageRenameListFields.Group, ImageRenameListFields.Key.Width)]
        [InlineData("<exif-make>", JpegRenameListFields.Group, JpegRenameListFields.Key.Make)]
        [InlineData("<exif-date:yyyy-MM-dd>", JpegRenameListFields.Group, JpegRenameListFields.Key.DateTaken)]
        public void Collect_Formatter_MediaMpegImageExifTokens_MapOriginalCatalogColumns(
            string template,
            string expectedGroupId,
            string expectedPropertyKey
        )
        {
            var filter = new FormatterFilter(new FileNameTarget(), new FormatterOptions(template));

            var keys = FilterRelevantRenameListColumns.Collect([filter]);
            var expectedTokenKey = RenameListFieldKey.Original(expectedGroupId, expectedPropertyKey);

            Assert.Equal(
                [
                    RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name),
                    expectedTokenKey,
                ],
                keys
            );
            Assert.True(RenameListFieldCatalog.TryGetField(expectedTokenKey, out _));
        }

        /// <summary>
        /// Verifies every Media / MPEG / Image / named-EXIF mapped token resolves to a catalog field.
        /// </summary>
        [Fact]
        public void TryMap_MappedMediaMpegImageExifTokens_ResolveCatalogFields()
        {
            var mappedTokens = FormatTokenRegistry
                .NameToToken.Values.Distinct()
                .OfType<IRenameListMappedFormatToken>()
                .Where(static token =>
                    token
                        is MediaPropertyTokenBase
                            or MpegAudioPropertyTokenBase
                            or ImagePropertyTokenBase
                            or ExifPropertyTokenBase
                            or ExifDateToken
                )
                .ToList();

            Assert.NotEmpty(mappedTokens);

            foreach (var mapped in mappedTokens)
            {
                Assert.True(mapped.TryGetFixedField(out var expectedGroupId, out var expectedPropertyKey));
                var token = (IFormatToken)mapped;
                var canonicalName = token.Names[0];

                Assert.True(
                    FormatTokenRenameListFieldMap.TryMap(canonicalName, args: "", out var groupId, out var propertyKey),
                    $"unmapped token '{canonicalName}'"
                );
                Assert.Equal(expectedGroupId, groupId);
                Assert.Equal(expectedPropertyKey, propertyKey);
                Assert.True(
                    RenameListFieldCatalog.TryGetField(groupId, propertyKey, out _),
                    $"missing catalog field {groupId}/{propertyKey} for '{canonicalName}'"
                );
            }

            Assert.False(FormatTokenRenameListFieldMap.TryMap("exif", "ExifSub,36867", out _, out _));
        }
    }
}

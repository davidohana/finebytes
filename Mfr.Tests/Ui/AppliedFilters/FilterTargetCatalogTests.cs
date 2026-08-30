using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Models.Tags.Id3v1;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Unit tests for <see cref="FilterTargetCatalog"/> lookup and Apply-To labels.
    /// </summary>
    public sealed class FilterTargetCatalogTests
    {
        /// <summary>
        /// Verifies unknown <see cref="FilterTarget"/> types fall back to File Prefix.
        /// </summary>
        [Fact]
        public void Resolve_falls_back_to_file_prefix_for_unknown_target_type()
        {
            var (group, option, ancestorFolderLevel) = FilterTargetCatalog.Resolve(new UnknownFilterTarget());

            Assert.Equal("File Name", group.Label);
            Assert.IsType<FilePrefixTarget>(option.Prototype);
            Assert.Equal(1, ancestorFolderLevel);
        }

        /// <summary>
        /// Verifies ID3v2 frame ids match catalog options regardless of case.
        /// </summary>
        [Fact]
        public void Resolve_matches_id3v2_frame_id_case_insensitively()
        {
            var (group, option, _) = FilterTargetCatalog.Resolve(new Id3v2FrameTarget("tit2"));

            Assert.Equal("ID3v2", group.Label);
            var prototype = Assert.IsType<Id3v2FrameTarget>(option.Prototype);
            Assert.Equal("TIT2", prototype.FrameId);
        }

        /// <summary>
        /// Verifies Xiph keys match catalog options regardless of case.
        /// </summary>
        [Fact]
        public void Resolve_matches_xiph_key_case_insensitively()
        {
            var (group, option, _) = FilterTargetCatalog.Resolve(new XiphFieldTarget("title"));

            Assert.Equal("Xiph", group.Label);
            var prototype = Assert.IsType<XiphFieldTarget>(option.Prototype);
            Assert.Equal("TITLE", prototype.Key);
        }

        /// <summary>
        /// Verifies ID3v1 catalog lookup uses the enum payload.
        /// </summary>
        [Fact]
        public void Resolve_selects_id3v1_field()
        {
            var (group, option, _) = FilterTargetCatalog.Resolve(new Id3v1FieldTarget(Id3v1Field.Album));

            Assert.Equal("ID3v1", group.Label);
            var prototype = Assert.IsType<Id3v1FieldTarget>(option.Prototype);
            Assert.Equal(Id3v1Field.Album, prototype.Field);
        }

        /// <summary>
        /// Verifies ID3v2 list subtitles use the description when set, otherwise the friendly frame label.
        /// </summary>
        [Fact]
        public void GetLabel_id3v2_uses_description_when_set()
        {
            Assert.Equal("COMM (Comment)", FilterTargetCatalog.GetLabel(new Id3v2FrameTarget("COMM")));
            Assert.Equal("COMM (Short)", FilterTargetCatalog.GetLabel(new Id3v2FrameTarget("COMM", "eng", "Short")));
            Assert.Equal(
                "TXXX (MusicBrainz Artist Id)",
                FilterTargetCatalog.GetLabel(new Id3v2FrameTarget("TXXX", Description: "MusicBrainz Artist Id"))
            );
        }

        private sealed record UnknownFilterTarget : FilterTarget;
    }
}

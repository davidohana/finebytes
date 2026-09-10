using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v1;
using Mfr.Models.Tags.Id3v2;

namespace Mfr.Tests.Ui.AppliedFilters
{
    /// <summary>
    /// Unit tests for <see cref="FilterTargetCatalog"/> lookup and Apply-To labels.
    /// </summary>
    public sealed class FilterTargetCatalogTests
    {
        /// <summary>
        /// Verifies unknown <see cref="FilterTarget"/> types fall back to File Name.
        /// </summary>
        [Fact]
        public void Resolve_falls_back_to_file_name_for_unknown_target_type()
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

        /// <summary>
        /// Verifies every modeled ID3v2 frame has a friendly Apply-To label (not a bare frame id).
        /// </summary>
        [Fact]
        public void Id3v2_options_cover_all_modeled_frames_with_friendly_labels()
        {
            var id3v2Group = FilterTargetCatalog.Groups.First(group => group.Label == "ID3v2");
            Assert.Equal(Id3v2ModeledFrame.AllModeledFrameIds.Count, id3v2Group.Targets.Count);

            foreach (var frameId in Id3v2ModeledFrame.AllModeledFrameIds)
            {
                var option = id3v2Group.Targets.Single(o =>
                    o.Prototype is Id3v2FrameTarget frame && frame.FrameId == frameId
                );
                Assert.Equal(Id3v2FrameLabels.For(frameId), option.Label);
                Assert.NotEqual(frameId, option.Label);
            }
        }

        /// <summary>
        /// Verifies Xiph keys that share a semantic field reuse <see cref="SemanticAudioFieldLabels"/>.
        /// </summary>
        [Fact]
        public void GetLabel_xiph_overlapping_keys_use_semantic_audio_labels()
        {
            Assert.Equal(
                SemanticAudioFieldLabels.For(SemanticAudioField.Title),
                FilterTargetCatalog.GetLabel(new XiphFieldTarget("TITLE"))
            );
            Assert.Equal(
                SemanticAudioFieldLabels.For(SemanticAudioField.Performers),
                FilterTargetCatalog.GetLabel(new XiphFieldTarget("ARTIST"))
            );
            Assert.Equal(
                SemanticAudioFieldLabels.For(SemanticAudioField.BeatsPerMinute),
                FilterTargetCatalog.GetLabel(new XiphFieldTarget("BPM"))
            );

            foreach (var row in AudioCatalogFieldMaps.All)
            {
                Assert.Equal(
                    SemanticAudioFieldLabels.For(row.Field),
                    FilterTargetCatalog.GetLabel(new XiphFieldTarget(row.XiphKey))
                );
            }
        }

        /// <summary>
        /// Verifies Xiph-only keys keep distinct wording (not the semantic Track/Disc/Comment labels).
        /// </summary>
        [Fact]
        public void GetLabel_xiph_only_keys_keep_distinct_wording()
        {
            Assert.Equal("Track Number", FilterTargetCatalog.GetLabel(new XiphFieldTarget("TRACKNUMBER")));
            Assert.NotEqual(
                SemanticAudioFieldLabels.For(SemanticAudioField.Track),
                FilterTargetCatalog.GetLabel(new XiphFieldTarget("TRACKNUMBER"))
            );
            Assert.Equal("Description", FilterTargetCatalog.GetLabel(new XiphFieldTarget("DESCRIPTION")));
            Assert.Equal("Tempo", FilterTargetCatalog.GetLabel(new XiphFieldTarget("TEMPO")));
        }

        /// <summary>
        /// Verifies Apply-To File Name / Path options use <see cref="PathFieldLabels"/>.
        /// </summary>
        [Fact]
        public void File_name_and_path_options_use_path_field_labels()
        {
            var fileNameGroup = FilterTargetCatalog.Groups.First(g => g.Label == PathFieldLabels.FileName);
            Assert.Equal(
                [PathFieldLabels.FileName, PathFieldLabels.FileExtension, PathFieldLabels.FullFileName],
                fileNameGroup.Targets.Select(t => t.Label)
            );

            var pathGroup = FilterTargetCatalog.Groups.First(g => g.Label == "Path");
            Assert.Equal(
                [PathFieldLabels.FullPath, PathFieldLabels.ParentDirectory, PathFieldLabels.ParentFolder],
                pathGroup.Targets.Select(t => t.Label)
            );
            Assert.Equal(PathFieldLabels.ParentFolder, FilterTargetCatalog.GetLabel(new AncestorFolderTarget(1)));
        }

        private sealed record UnknownFilterTarget : FilterTarget;
    }
}

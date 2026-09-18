using Mfr.Engine.RenameScript;
using Mfr.Filters;
using Mfr.Filters.Attributes;
using Mfr.Filters.Audio;
using Mfr.Filters.Case;
using Mfr.Filters.Misc;
using Mfr.Models.Filters;
using Mfr.Models.Media;
using Mfr.Models.Rename;
using Mfr.Models.Tags;
using Mfr.Models.Tags.Id3v1;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Unit tests for <see cref="RenameScriptFilterSupport"/>.
    /// </summary>
    public sealed class RenameScriptFilterSupportTests
    {
        /// <summary>
        /// Verifies name and path targets are supported.
        /// </summary>
        [Theory]
        [InlineData(typeof(FileNameTarget))]
        [InlineData(typeof(FileExtensionTarget))]
        [InlineData(typeof(FileFullNameTarget))]
        [InlineData(typeof(FullPathTarget))]
        [InlineData(typeof(ParentDirectoryTarget))]
        public void IsSupported_target_true_for_path_domains(Type targetType)
        {
            var target = (FilterTarget)Activator.CreateInstance(targetType)!;
            Assert.True(RenameScriptFilterSupport.IsSupported(target));
        }

        /// <summary>
        /// Verifies ancestor folder and attributes targets are supported.
        /// </summary>
        [Fact]
        public void IsSupported_target_true_for_ancestor_and_attrs()
        {
            Assert.True(RenameScriptFilterSupport.IsSupported(new AncestorFolderTarget(1)));
            Assert.True(RenameScriptFilterSupport.IsSupported(new FileAttributesTarget()));
        }

        /// <summary>
        /// Verifies timestamp and tag targets are not supported.
        /// </summary>
        [Fact]
        public void IsSupported_target_false_for_dates_and_tags()
        {
            Assert.False(RenameScriptFilterSupport.IsSupported(new FileTimestampTarget(TimestampField.LastWrite)));
            Assert.False(RenameScriptFilterSupport.IsSupported(new SemanticAudioFieldTarget(SemanticAudioField.Title)));
            Assert.False(RenameScriptFilterSupport.IsSupported(new Id3v1FieldTarget(Id3v1Field.Title)));
            Assert.False(RenameScriptFilterSupport.IsSupported(new Id3v2FrameTarget("TIT2")));
            Assert.False(RenameScriptFilterSupport.IsSupported(new XiphFieldTarget("TITLE")));
        }

        /// <summary>
        /// Verifies string filters follow their Apply-To target.
        /// </summary>
        [Fact]
        public void IsSupported_filter_follows_string_target()
        {
            var options = new LettersCaseOptions(LettersCaseMode.LowerCase, []);
            Assert.True(RenameScriptFilterSupport.IsSupported(new LettersCaseFilter(new FileNameTarget(), options)));
            Assert.False(
                RenameScriptFilterSupport.IsSupported(
                    new LettersCaseFilter(new SemanticAudioFieldTarget(SemanticAudioField.Title), options)
                )
            );
        }

        /// <summary>
        /// Verifies fixed path/attrs filters are supported and date/tag filters are not.
        /// </summary>
        [Fact]
        public void IsSupported_filter_fixed_domains()
        {
            Assert.True(RenameScriptFilterSupport.IsSupported(new AttributesSetterFilter()));
            Assert.True(RenameScriptFilterSupport.IsSupported(new PathMoverFilter()));
            Assert.False(RenameScriptFilterSupport.IsSupported(new DateTimeSetterFilter()));
            Assert.False(RenameScriptFilterSupport.IsSupported(new TimeShifterFilter()));
            Assert.False(RenameScriptFilterSupport.IsSupported(new AudioTagSetterFilter()));
            Assert.False(RenameScriptFilterSupport.IsSupported(new TagRemoverFilter()));
            Assert.False(RenameScriptFilterSupport.IsSupported(new Id3v2FieldSetterFilter()));
        }

        /// <summary>
        /// Verifies only enabled unsupported steps are listed, in order, without duplicates.
        /// </summary>
        [Fact]
        public void GetUnsupportedEnabledStepNames_skips_disabled_and_supported()
        {
            var names = RenameScriptFilterSupport.GetUnsupportedEnabledStepNames([
                (true, new LettersCaseFilter(), "Case"),
                (true, new DateTimeSetterFilter(), "Date/Time Setter"),
                (false, new TagRemoverFilter(), "Audio Tag Remover"),
                (true, new DateTimeSetterFilter(), "Date/Time Setter"),
                (true, new AudioTagSetterFilter(), "Audio Tag Setter"),
            ]);

            Assert.Equal(["Date/Time Setter", "Audio Tag Setter"], names);
        }
    }
}

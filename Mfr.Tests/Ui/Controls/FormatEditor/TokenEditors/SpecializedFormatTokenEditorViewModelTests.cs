using Mfr.App.Ui.ViewModels.Controls.FormatEditor.TokenEditors;
using Mfr.App.Ui.ViewModels.FilterEditors.Audio;
using Mfr.Filters.Formatting;

namespace Mfr.Tests.Ui.Controls.FormatEditor.TokenEditors
{
    /// <summary>
    /// Unit tests for specialized format-token parameter editors.
    /// </summary>
    public sealed class SpecializedFormatTokenEditorViewModelTests
    {
        /// <summary>
        /// Verifies id3v2 defaults to TIT2 and round-trips a content descriptor.
        /// </summary>
        [Fact]
        public void Id3v2_DefaultsAndDescriptor()
        {
            var defaults = new Id3v2FormatTokenEditorViewModel(null);
            Assert.Equal(Id3v2FrameChoice.Tit2, defaults.SelectedFrame);
            Assert.Equal("id3v2:TIT2", defaults.BuildInnerText());
            Assert.Equal(
                FormatTokenCatalog.Entries.First(e => e.CanonicalName == "id3v2").InsertText,
                defaults.ResultingFormatString
            );

            var withDesc = new Id3v2FormatTokenEditorViewModel("TXXX:catalog");
            Assert.Equal("TXXX", withDesc.SelectedFrame.FrameId);
            Assert.Equal("catalog", withDesc.Description);
            Assert.True(withDesc.ShowsDescription);
            Assert.Equal("id3v2:TXXX:catalog", withDesc.BuildInnerText());
            Assert.True(FormatStringSyntax.TryValidate(withDesc.ResultingFormatString).Success);
        }

        /// <summary>
        /// Verifies exif defaults and source,name round-trip.
        /// </summary>
        [Fact]
        public void Exif_DefaultsAndRoundTrip()
        {
            var defaults = new ExifFormatTokenEditorViewModel(null);
            Assert.Equal("ExifSub", defaults.Source);
            Assert.Equal("36867", defaults.Name);
            Assert.Equal(
                FormatTokenCatalog.Entries.First(e => e.CanonicalName == "exif").InsertText,
                defaults.ResultingFormatString
            );

            var vm = new ExifFormatTokenEditorViewModel("Exif,Make");
            Assert.Equal("Exif", vm.Source);
            Assert.Equal("Make", vm.Name);
            Assert.Equal("exif:Exif,Make", vm.BuildInnerText());
            Assert.True(FormatStringSyntax.TryValidate(vm.ResultingFormatString).Success);
        }
    }
}

using Mfr.App.Ui.ViewModels.Controls.FormatEditor.TokenEditors;
using Mfr.Filters.Formatting;

namespace Mfr.Tests.Ui.Controls.FormatEditor.TokenEditors
{
    /// <summary>
    /// Unit tests for simple format-token parameter editors.
    /// </summary>
    public sealed class SimpleFormatTokenEditorViewModelTests
    {
        /// <summary>
        /// Verifies parent-folder level 1 stays bare and higher levels append <c>:N</c>.
        /// </summary>
        [Fact]
        public void ParentFolder_DefaultsAndLevel()
        {
            var defaults = new ParentFolderFormatTokenEditorViewModel(null);
            Assert.Equal(1, defaults.Level);
            Assert.Equal("parent-folder", defaults.BuildInnerText());
            Assert.Equal(
                FormatTokenCatalog.Entries.First(e => e.CanonicalName == "parent-folder").InsertText,
                defaults.ResultingFormatString
            );

            var level2 = new ParentFolderFormatTokenEditorViewModel("2");
            Assert.Equal(2, level2.Level);
            Assert.Equal("parent-folder:2", level2.BuildInnerText());
            Assert.True(FormatStringSyntax.TryValidate(level2.ResultingFormatString).Success);
        }

        /// <summary>
        /// Verifies now omits empty format and keeps a custom format string.
        /// </summary>
        [Fact]
        public void Now_OptionalFormat()
        {
            var bare = new NowFormatTokenEditorViewModel(null);
            Assert.Equal("now", bare.BuildInnerText());

            var custom = new NowFormatTokenEditorViewModel("yyyy-MM-dd");
            Assert.Equal("now:yyyy-MM-dd", custom.BuildInnerText());
            Assert.True(FormatStringSyntax.TryValidate(custom.ResultingFormatString).Success);
        }

        /// <summary>
        /// Verifies exif-date requires a format and defaults to <c>dd-MM-yyyy</c>.
        /// </summary>
        [Fact]
        public void ExifDate_DefaultFormat()
        {
            var vm = new ExifDateFormatTokenEditorViewModel(null);
            Assert.Equal("exif-date:dd-MM-yyyy", vm.BuildInnerText());
            Assert.Equal(
                FormatTokenCatalog.Entries.First(e => e.CanonicalName == "exif-date").InsertText,
                vm.ResultingFormatString
            );
            Assert.True(FormatStringSyntax.TryValidate(vm.ResultingFormatString).Success);
        }

        /// <summary>
        /// Verifies random-char defaults and round-trips <c>low,high</c>.
        /// </summary>
        [Fact]
        public void RandomChar_DefaultsAndRoundTrip()
        {
            var defaults = new RandomCharFormatTokenEditorViewModel(null);
            Assert.Equal("A", defaults.Low);
            Assert.Equal("Z", defaults.High);
            Assert.Equal("random-char:A,Z", defaults.BuildInnerText());

            var parsed = new RandomCharFormatTokenEditorViewModel("0,9");
            Assert.Equal("0", parsed.Low);
            Assert.Equal("9", parsed.High);
            Assert.Equal("random-char:0,9", parsed.BuildInnerText());
            Assert.True(FormatStringSyntax.TryValidate(parsed.ResultingFormatString).Success);
        }
    }
}

using Mfr.App.Ui.ViewModels.Controls.FormatEditor.TokenEditors;
using Mfr.Filters.Formatting;

namespace Mfr.Tests.Ui.Controls.FormatEditor.TokenEditors
{
    /// <summary>
    /// Unit tests for named / positional format-token parameter editors.
    /// </summary>
    public sealed class NamedFormatTokenEditorViewModelTests
    {
        /// <summary>
        /// Verifies substr defaults and named-arg round-trip.
        /// </summary>
        [Fact]
        public void Substr_DefaultsAndRoundTrip()
        {
            var defaults = new SubstrFormatTokenEditorViewModel(null);
            Assert.Equal(
                FormatTokenCatalog.Entries.First(e => e.CanonicalName == "substr").InsertText,
                defaults.ResultingFormatString
            );

            const string args = "start=2,end=5,source=<full-name>";
            var vm = new SubstrFormatTokenEditorViewModel(args);
            Assert.Equal(2, vm.Start);
            Assert.Equal(5, vm.End);
            Assert.Equal("<full-name>", vm.Source);
            Assert.Equal("substr:" + args, vm.BuildInnerText());
            Assert.True(FormatStringSyntax.TryValidate(vm.ResultingFormatString).Success);
        }

        /// <summary>
        /// Verifies token extract defaults and named-arg round-trip.
        /// </summary>
        [Fact]
        public void Token_DefaultsAndRoundTrip()
        {
            var defaults = new TokenFormatTokenEditorViewModel(null);
            Assert.Equal(
                FormatTokenCatalog.Entries.First(e => e.CanonicalName == "token").InsertText,
                defaults.ResultingFormatString
            );

            const string args = "tokenNumber=2,separator=_,includeNext=true,includePrev=false,source=<full-name>";
            var vm = new TokenFormatTokenEditorViewModel(args);
            Assert.Equal(2, vm.TokenNumber);
            Assert.Equal("_", vm.Separator);
            Assert.True(vm.IncludeNext);
            Assert.False(vm.IncludePrev);
            Assert.Equal("<full-name>", vm.Source);
            Assert.Equal("token:" + args, vm.BuildInnerText());
            Assert.True(FormatStringSyntax.TryValidate(vm.ResultingFormatString).Success);
        }

        /// <summary>
        /// Verifies file-date positional format,kind round-trip.
        /// </summary>
        [Fact]
        public void FileDate_DefaultsAndRoundTrip()
        {
            var defaults = new FileDateFormatTokenEditorViewModel(null);
            Assert.Equal(
                FormatTokenCatalog.Entries.First(e => e.CanonicalName == "file-date").InsertText,
                defaults.ResultingFormatString
            );

            var vm = new FileDateFormatTokenEditorViewModel("yyyy-MM-dd HH:mm,lastWrite");
            Assert.Equal("yyyy-MM-dd HH:mm", vm.Format);
            Assert.Equal("lastWrite", vm.Kind.Value);
            Assert.Equal("file-date:yyyy-MM-dd HH:mm,lastWrite", vm.BuildInnerText());
            Assert.True(FormatStringSyntax.TryValidate(vm.ResultingFormatString).Success);
        }

        /// <summary>
        /// Verifies file-size bare default and unit/decimals emission.
        /// </summary>
        [Fact]
        public void FileSize_DefaultsAndArgs()
        {
            var defaults = new FileSizeFormatTokenEditorViewModel(null);
            Assert.Equal("file-size", defaults.BuildInnerText());

            var vm = new FileSizeFormatTokenEditorViewModel("mb,2");
            Assert.Equal("mb", vm.Unit.Value);
            Assert.Equal(2, vm.Decimals);
            Assert.Equal("file-size:mb,2", vm.BuildInnerText());
            Assert.True(FormatStringSyntax.TryValidate(vm.ResultingFormatString).Success);
        }
    }
}

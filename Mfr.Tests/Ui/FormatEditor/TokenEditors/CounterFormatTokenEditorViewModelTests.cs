using Mfr.App.Ui.ViewModels.FormatEditor;
using Mfr.App.Ui.ViewModels.FormatEditor.TokenEditors;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.Tests.Ui.FormatEditor.TokenEditors
{
    /// <summary>
    /// Unit tests for <see cref="CounterFormatTokenEditorViewModel"/>.
    /// </summary>
    public sealed class CounterFormatTokenEditorViewModelTests
    {
        /// <summary>
        /// Verifies empty args use catalog defaults and emit the catalog insert inner text.
        /// </summary>
        [Fact]
        public void Defaults_MatchCatalogInsertText()
        {
            var entry = FormatTokenCatalog.Entries.First(e => e.CanonicalName == "counter");
            var vm = new CounterFormatTokenEditorViewModel(args: null);

            Assert.Equal(1, vm.Initial);
            Assert.Equal(1, vm.Step);
            Assert.Equal("none", vm.Padding.Value);
            Assert.Equal(2, vm.Length);
            Assert.Equal("global", vm.ResetScope.Value);
            Assert.Equal(entry.InsertText, vm.ResultingFormatString);
            Assert.True(FormatStringSyntax.TryValidate(vm.ResultingFormatString).Success);
        }

        /// <summary>
        /// Verifies named args round-trip through parse and BuildInnerText.
        /// </summary>
        [Fact]
        public void RoundTrip_ParsesAndRebuildsArgs()
        {
            const string args = "initial=10,step=2,padding=fixed,length=4,resetScope=perFolder";
            var vm = new CounterFormatTokenEditorViewModel(args);

            Assert.Equal(10, vm.Initial);
            Assert.Equal(2, vm.Step);
            Assert.Equal("fixed", vm.Padding.Value);
            Assert.Equal(4, vm.Length);
            Assert.Equal("perFolder", vm.ResetScope.Value);
            Assert.Equal("counter:" + args, vm.BuildInnerText());
            Assert.Equal("<counter:" + args + ">", vm.ResultingFormatString);
        }

        /// <summary>
        /// Verifies registry creates the counter editor.
        /// </summary>
        [Fact]
        public void Registry_CreatesCounterEditor()
        {
            Assert.True(FormatTokenEditorRegistry.HasEditor("counter"));
            Assert.True(FormatTokenEditorRegistry.TryCreate("counter", string.Empty, out var editor));
            Assert.IsType<CounterFormatTokenEditorViewModel>(editor);
        }
    }
}

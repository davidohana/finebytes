using Mfr.App.Ui.ViewModels.Controls.FormatEditor;
using Mfr.Filters.Formatting;

namespace Mfr.Tests.Ui.Controls.FormatEditor
{
    /// <summary>
    /// Unit tests for <see cref="FormatEditorViewModel"/>.
    /// </summary>
    public sealed class FormatEditorViewModelTests
    {
        /// <summary>
        /// Verifies search filters catalog rows by name and description.
        /// </summary>
        [Fact]
        public void SearchText_FiltersVisibleEntries()
        {
            var inserted = new List<string>();
            var vm = new FormatEditorViewModel(
                insertText: inserted.Add,
                jumpToError: static () => { },
                editUnderCaret: static () => { }
            );

            Assert.Equal(FormatTokenCatalog.Entries.Count, vm.VisibleEntries.Count);

            vm.SearchText = "counter";
            Assert.Contains(vm.VisibleEntries, e => e.CanonicalName == "counter");
            Assert.All(
                vm.VisibleEntries,
                e =>
                    Assert.True(
                        e.CanonicalName.Contains("counter", StringComparison.OrdinalIgnoreCase)
                            || e.DisplayName.Contains("counter", StringComparison.OrdinalIgnoreCase)
                            || e.ShortDescription.Contains("counter", StringComparison.OrdinalIgnoreCase)
                            || e.GroupPath.Contains("counter", StringComparison.OrdinalIgnoreCase)
                    )
            );
        }

        /// <summary>
        /// Verifies InsertEntry invokes the insert callback with catalog InsertText.
        /// </summary>
        [Fact]
        public void InsertEntryCommand_InsertsCatalogText()
        {
            var inserted = new List<string>();
            var vm = new FormatEditorViewModel(
                insertText: inserted.Add,
                jumpToError: static () => { },
                editUnderCaret: static () => { }
            );

            var entry = FormatTokenCatalog.Entries.First(e => e.CanonicalName == "file-name");
            vm.SearchText = "file";
            Assert.True(vm.InsertEntryCommand.CanExecute(entry));
            vm.InsertEntryCommand.Execute(entry);

            Assert.Equal([entry.InsertText], inserted);
            Assert.Equal(string.Empty, vm.SearchText);
        }

        /// <summary>
        /// Verifies Validate sets error state for unknown tokens.
        /// </summary>
        [Fact]
        public void Validate_UnknownToken_SetsError()
        {
            var vm = new FormatEditorViewModel(
                insertText: static _ => { },
                jumpToError: static () => { },
                editUnderCaret: static () => { }
            );

            vm.Validate("<nope>");
            Assert.True(vm.HasError);
            Assert.False(string.IsNullOrEmpty(vm.ErrorMessage));
            Assert.NotNull(vm.LastParseResult);
            Assert.False(vm.LastParseResult.Success);
        }

        /// <summary>
        /// Verifies long parse errors are truncated inline while FullErrorMessage stays complete.
        /// </summary>
        [Fact]
        public void Validate_LongError_TruncatesInlineMessage()
        {
            var vm = new FormatEditorViewModel(
                insertText: static _ => { },
                jumpToError: static () => { },
                editUnderCaret: static () => { }
            );

            // Unknown token with a very long name forces a long ErrorMessage from TryValidate.
            var longName = new string('a', 200);
            vm.Validate($"<{longName}>");
            Assert.True(vm.HasError);
            Assert.NotNull(vm.FullErrorMessage);
            Assert.True(vm.FullErrorMessage.Length > vm.ErrorMessage.Length);
            Assert.EndsWith("…", vm.ErrorMessage);
        }
    }
}

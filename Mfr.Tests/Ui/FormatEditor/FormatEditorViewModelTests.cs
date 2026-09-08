using Mfr.App.Ui.ViewModels.FormatEditor;
using Mfr.Filters.Formatting.FormatString;

namespace Mfr.Tests.Ui.FormatEditor
{
    /// <summary>
    /// Unit tests for <see cref="FormatEditorViewModel"/> validation / error chrome.
    /// </summary>
    public sealed class FormatEditorViewModelTests
    {
        /// <summary>
        /// Verifies Validate sets error state for unknown tokens.
        /// </summary>
        [Fact]
        public void Validate_UnknownToken_SetsError()
        {
            var vm = _CreateVm();

            vm.Validate("<nope>");
            Assert.True(vm.HasError);
            Assert.False(string.IsNullOrEmpty(vm.ErrorMessage));
            Assert.NotNull(vm.LastParseResult);
            Assert.False(vm.LastParseResult.Success);
        }

        /// <summary>
        /// Verifies WhenLikelyTokens skips errors for non-token angle brackets.
        /// </summary>
        [Fact]
        public void Validate_WhenLikelyTokens_NonLikelyLiteral_ClearsError()
        {
            var vm = _CreateVm();
            vm.ValidationMode = FormatStringValidationMode.WhenLikelyTokens;

            vm.Validate("<3>");
            Assert.False(vm.HasError);
            Assert.NotNull(vm.LastParseResult);
            Assert.True(vm.LastParseResult.Success);
        }

        /// <summary>
        /// Verifies long parse errors are truncated inline while FullErrorMessage stays complete.
        /// </summary>
        [Fact]
        public void Validate_LongError_TruncatesInlineMessage()
        {
            var vm = _CreateVm();

            // Unknown token with a very long name forces a long ErrorMessage from TryValidate.
            var longName = new string('a', 200);
            vm.Validate($"<{longName}>");
            Assert.True(vm.HasError);
            Assert.NotNull(vm.FullErrorMessage);
            Assert.True(vm.FullErrorMessage.Length > vm.ErrorMessage.Length);
            Assert.EndsWith("…", vm.ErrorMessage);
        }

        /// <summary>
        /// Verifies JumpToError and Edit invoke their callbacks.
        /// </summary>
        [Fact]
        public void JumpToError_And_Edit_InvokeCallbacks()
        {
            var jumped = 0;
            var edited = 0;
            var vm = new FormatEditorViewModel(jumpToError: () => jumped++, editUnderCaret: () => edited++);

            vm.JumpToErrorCommand.Execute(null);
            vm.EditCommand.Execute(null);

            Assert.Equal(1, jumped);
            Assert.Equal(1, edited);
        }

        private static FormatEditorViewModel _CreateVm()
        {
            return new(jumpToError: static () => { }, editUnderCaret: static () => { });
        }
    }
}

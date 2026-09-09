using Mfr.App.Ui.ViewModels.FormatEditor;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Ui.FormatEditor
{
    /// <summary>
    /// Unit tests for <see cref="FormatTokenPreviewViewModel"/>.
    /// </summary>
    public sealed class FormatTokenPreviewViewModelTests
    {
        /// <summary>
        /// Verifies empty Rename List shows MFR7 empty-list messaging.
        /// </summary>
        [Fact]
        public void EmptyList_ShowsUnavailableMessages()
        {
            var preview = new FormatTokenPreviewViewModel([]);
            preview.Refresh("<file-name>");

            Assert.Equal(FormatTokenPreviewViewModel.EmptyListSampleText, preview.SampleText);
            Assert.Equal(FormatTokenPreviewViewModel.PreviewUnavailableText, preview.PreviewResult);
            Assert.Equal(string.Empty, preview.ItemIndexLabel);
            Assert.False(preview.CanGoPrevious);
            Assert.False(preview.CanGoNext);
            Assert.False(preview.GoPreviousCommand.CanExecute(null));
            Assert.False(preview.GoNextCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies sample / result update when cycling items.
        /// </summary>
        [Fact]
        public void Cycle_UpdatesSampleAndResult()
        {
            var items = new[]
            {
                FilterTestHelpers.CreateRenameItem(prefix: "alpha", extension: "mp3"),
                FilterTestHelpers.CreateRenameItem(prefix: "beta", extension: "wav", renameListIndex: 1),
            };
            var preview = new FormatTokenPreviewViewModel(items);
            preview.Refresh("<file-name>");

            Assert.Equal("alpha.mp3", preview.SampleText);
            Assert.Equal("alpha", preview.PreviewResult);
            Assert.Equal("1", preview.ItemIndexLabel);
            Assert.False(preview.CanGoPrevious);
            Assert.True(preview.CanGoNext);

            preview.GoNext();
            Assert.Equal("beta.wav", preview.SampleText);
            Assert.Equal("beta", preview.PreviewResult);
            Assert.Equal("2", preview.ItemIndexLabel);
            Assert.True(preview.CanGoPrevious);
            Assert.False(preview.CanGoNext);

            preview.GoPrevious();
            Assert.Equal("alpha.mp3", preview.SampleText);
            Assert.Equal("1", preview.ItemIndexLabel);
        }

        /// <summary>
        /// Verifies Refresh re-evaluates when the resulting format string changes.
        /// </summary>
        [Fact]
        public void Refresh_ReevaluatesResult()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "track", extension: "mp3");
            var preview = new FormatTokenPreviewViewModel([item]);
            preview.Refresh("<file-name>");
            Assert.Equal("track", preview.PreviewResult);

            preview.Refresh("<file-extension>");
            Assert.Equal("mp3", preview.PreviewResult);
        }

        /// <summary>
        /// Verifies evaluation failures are prefixed like MFR7.
        /// </summary>
        [Fact]
        public void Refresh_UnknownToken_PrefixesError()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            var preview = new FormatTokenPreviewViewModel([item]);
            preview.Refresh("<does-not-exist>");

            Assert.StartsWith(FormatTokenPreviewViewModel.ErrorPrefix, preview.PreviewResult, StringComparison.Ordinal);
            Assert.Contains("Unknown formatter token", preview.PreviewResult);
        }
    }
}

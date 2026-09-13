namespace Mfr.Tests.Models
{
    /// <summary>
    /// Unit tests for <see cref="ConfirmationPolicy"/>.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class ConfirmationPolicyTests
    {
        public ConfirmationPolicyTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        [Theory]
        [InlineData(ConfirmationKind.GoWithPreviewErrors)]
        [InlineData(ConfirmationKind.UndoRename)]
        [InlineData(ConfirmationKind.ReplaceAppliedFiltersOnLoad)]
        [InlineData(ConfirmationKind.ClearRenameList)]
        [InlineData(ConfirmationKind.ClearAppliedFilters)]
        [InlineData(ConfirmationKind.OverwritePreset)]
        [InlineData(ConfirmationKind.DeletePreset)]
        public void ShouldConfirm_true_by_default(ConfirmationKind kind)
        {
            Assert.True(ConfirmationPolicy.ShouldConfirm(kind));
        }

        [Fact]
        public void Suppress_skips_only_that_kind()
        {
            ConfirmationPolicy.Suppress(ConfirmationKind.ClearRenameList);

            Assert.False(ConfirmationPolicy.ShouldConfirm(ConfirmationKind.ClearRenameList));
            Assert.True(ConfirmationPolicy.ShouldConfirm(ConfirmationKind.GoWithPreviewErrors));
            Assert.Contains(ConfirmationKind.ClearRenameList, ConfigStore.Ui.SuppressedConfirmations);
        }

        [Fact]
        public void Suppress_is_idempotent()
        {
            ConfirmationPolicy.Suppress(ConfirmationKind.UndoRename);
            ConfirmationPolicy.Suppress(ConfirmationKind.UndoRename);

            Assert.Equal([ConfirmationKind.UndoRename], ConfigStore.Ui.SuppressedConfirmations);
        }

        [Fact]
        public void ClearSuppressions_restores_show_by_default()
        {
            ConfirmationPolicy.Suppress(ConfirmationKind.GoWithPreviewErrors);
            ConfirmationPolicy.Suppress(ConfirmationKind.DeletePreset);
            ConfirmationPolicy.ClearSuppressions();

            Assert.Empty(ConfigStore.Ui.SuppressedConfirmations);
            Assert.True(ConfirmationPolicy.ShouldConfirm(ConfirmationKind.GoWithPreviewErrors));
            Assert.True(ConfirmationPolicy.ShouldConfirm(ConfirmationKind.DeletePreset));
        }

        [Fact]
        public void Load_ignores_obsolete_confirmationPrompts()
        {
            using var temp = ConfigStoreTempFile.CreateWithContent(
                /*lang=json,strict*/
                """
                {
                  "ui": {
                    "confirmationPrompts": "fewer",
                    "suppressedConfirmations": ["clearRenameList"]
                  }
                }
                """
            );
            ConfigStore.Load(temp.Path);

            Assert.Equal([ConfirmationKind.ClearRenameList], ConfigStore.Ui.SuppressedConfirmations);
            Assert.False(ConfirmationPolicy.ShouldConfirm(ConfirmationKind.ClearRenameList));
            Assert.True(ConfirmationPolicy.ShouldConfirm(ConfirmationKind.GoWithPreviewErrors));
        }
    }
}

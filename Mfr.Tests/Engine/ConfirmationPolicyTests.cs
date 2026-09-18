namespace Mfr.Tests.Engine
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
        [InlineData(ConfirmationKind.ReplaceFilterChainOnLoad)]
        [InlineData(ConfirmationKind.ClearRenameList)]
        [InlineData(ConfirmationKind.ClearFilterChain)]
        [InlineData(ConfirmationKind.OverwritePreset)]
        [InlineData(ConfirmationKind.DeletePreset)]
        [InlineData(ConfirmationKind.GenerateRenameScriptUnsupportedFilters)]
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
            Assert.Contains(ConfirmationKind.ClearRenameList, ConfigStore.Options.SuppressedConfirmations);
        }

        [Fact]
        public void Suppress_is_idempotent()
        {
            ConfirmationPolicy.Suppress(ConfirmationKind.UndoRename);
            ConfirmationPolicy.Suppress(ConfirmationKind.UndoRename);

            Assert.Equal([ConfirmationKind.UndoRename], ConfigStore.Options.SuppressedConfirmations);
        }

        [Fact]
        public void ClearSuppressions_restores_show_by_default()
        {
            ConfirmationPolicy.Suppress(ConfirmationKind.GoWithPreviewErrors);
            ConfirmationPolicy.Suppress(ConfirmationKind.DeletePreset);
            ConfirmationPolicy.ClearSuppressions();

            Assert.Empty(ConfigStore.Options.SuppressedConfirmations);
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
                  "options": {
                    "confirmationPrompts": "fewer",
                    "suppressedConfirmations": ["clearRenameList"]
                  }
                }
                """
            );
            ConfigStore.Load(temp.Path);

            Assert.Equal([ConfirmationKind.ClearRenameList], ConfigStore.Options.SuppressedConfirmations);
            Assert.False(ConfirmationPolicy.ShouldConfirm(ConfirmationKind.ClearRenameList));
            Assert.True(ConfirmationPolicy.ShouldConfirm(ConfirmationKind.GoWithPreviewErrors));
        }
    }
}

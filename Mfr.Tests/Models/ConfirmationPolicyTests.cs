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
        [InlineData(ConfirmationPrompts.Fewer, ConfirmationKind.GoWithPreviewErrors, false)]
        [InlineData(ConfirmationPrompts.Fewer, ConfirmationKind.ReplaceAppliedFiltersOnLoad, false)]
        [InlineData(ConfirmationPrompts.Fewer, ConfirmationKind.ClearRenameList, false)]
        [InlineData(ConfirmationPrompts.Fewer, ConfirmationKind.ClearAppliedFilters, false)]
        [InlineData(ConfirmationPrompts.Normal, ConfirmationKind.GoWithPreviewErrors, true)]
        [InlineData(ConfirmationPrompts.Normal, ConfirmationKind.ReplaceAppliedFiltersOnLoad, false)]
        [InlineData(ConfirmationPrompts.Normal, ConfirmationKind.ClearRenameList, false)]
        [InlineData(ConfirmationPrompts.Normal, ConfirmationKind.ClearAppliedFilters, false)]
        [InlineData(ConfirmationPrompts.More, ConfirmationKind.GoWithPreviewErrors, true)]
        [InlineData(ConfirmationPrompts.More, ConfirmationKind.ReplaceAppliedFiltersOnLoad, true)]
        [InlineData(ConfirmationPrompts.More, ConfirmationKind.ClearRenameList, true)]
        [InlineData(ConfirmationPrompts.More, ConfirmationKind.ClearAppliedFilters, true)]
        public void ShouldConfirm_matches_level_and_kind_table(
            ConfirmationPrompts level,
            ConfirmationKind kind,
            bool expected
        )
        {
            ConfigStore.Config.Ui.ConfirmationPrompts = level;
            Assert.Equal(expected, ConfirmationPolicy.ShouldConfirm(kind));
        }
    }
}

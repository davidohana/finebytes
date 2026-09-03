using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Filters.Case;
using Mfr.Models.Filters;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Tests.Ui.AppliedFilters;
using Mfr.Tests.Ui.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Phase 10a: Applied Filters chain changes drive Rename List preview.
    /// </summary>
    public sealed class RenameListViewModelPreviewTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies appending Letters Case Upper updates the preview column and change count.
        /// </summary>
        [Fact]
        public async Task Chain_edit_updates_preview_column_and_change_count()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "hello.txt");
            File.WriteAllText(path, "x");

            var (renameList, applied) = _CreateWiredPanes(dir);
            await renameList.AddPathsAsync([path]).ConfigureAwait(true);
            Assert.Equal("hello.txt", renameList.Entries[0].FullFileNamePreview);
            Assert.Equal(0, renameList.ChangeCount);

            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            applied
                .Steps[0]
                .SetFilter(
                    new LettersCaseFilter(
                        new FilePrefixTarget(),
                        new LettersCaseOptions(LettersCaseMode.UpperCase, CapitalizeSkipWords: [])
                    )
                );

            Assert.Equal("HELLO.txt", renameList.Entries[0].FullFileNamePreview);
            Assert.Equal(1, renameList.ChangeCount);
            Assert.Equal(0, renameList.PreviewErrorCount);
        }

        /// <summary>
        /// Verifies disabling a step restores identity preview.
        /// </summary>
        [Fact]
        public async Task Disabling_step_restores_identity_preview()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "hello.txt");
            File.WriteAllText(path, "x");

            var (renameList, applied) = _CreateWiredPanes(dir);
            await renameList.AddPathsAsync([path]).ConfigureAwait(true);
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            applied
                .Steps[0]
                .SetFilter(
                    new LettersCaseFilter(
                        new FilePrefixTarget(),
                        new LettersCaseOptions(LettersCaseMode.UpperCase, CapitalizeSkipWords: [])
                    )
                );
            Assert.Equal("HELLO.txt", renameList.Entries[0].FullFileNamePreview);

            applied.Steps[0].Enabled = false;

            Assert.Equal("hello.txt", renameList.Entries[0].FullFileNamePreview);
            Assert.Equal(0, renameList.ChangeCount);
        }

        /// <summary>
        /// Verifies Filter Options target change re-previews via SetFilter.
        /// </summary>
        [Fact]
        public async Task Filter_options_target_change_repreviews()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "hello.txt");
            File.WriteAllText(path, "x");

            var (renameList, applied) = _CreateWiredPanes(dir);
            await renameList.AddPathsAsync([path]).ConfigureAwait(true);
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            applied
                .Steps[0]
                .SetFilter(
                    new LettersCaseFilter(
                        new FilePrefixTarget(),
                        new LettersCaseOptions(LettersCaseMode.UpperCase, CapitalizeSkipWords: [])
                    )
                );

            var dialog = new FilterOptionsDialogViewModel(applied.Steps[0])
            {
                SelectedTargetGroup = FilterTargetCatalog.Groups[0],
                SelectedTargetOption = FilterTargetCatalog
                    .Groups[0]
                    .Targets.Single(option => option.Prototype is FileFullNameTarget),
            };
            applied.ApplyFilterOptions(dialog);

            Assert.Equal("HELLO.TXT", renameList.Entries[0].FullFileNamePreview);
        }

        /// <summary>
        /// Verifies clearing the filter stack resets preview to identity.
        /// </summary>
        [Fact]
        public async Task Clearing_filters_resets_preview()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "hello.txt");
            File.WriteAllText(path, "x");

            var (renameList, applied) = _CreateWiredPanes(dir);
            await renameList.AddPathsAsync([path]).ConfigureAwait(true);
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            applied
                .Steps[0]
                .SetFilter(
                    new LettersCaseFilter(
                        new FilePrefixTarget(),
                        new LettersCaseOptions(LettersCaseMode.UpperCase, CapitalizeSkipWords: [])
                    )
                );

            applied.ClearCommand.Execute(null);

            Assert.Equal("hello.txt", renameList.Entries[0].FullFileNamePreview);
            Assert.Equal(
                "hello.txt",
                renameList
                    .Entries[0]
                    .GetFieldText(
                        RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.FullName)
                    )
            );
        }

        private (RenameListViewModel RenameList, AppliedFiltersViewModel Applied) _CreateWiredPanes(string dir)
        {
            var renameList = _context.CreateRenameListViewModel(dir);
            var applied = new AppliedFiltersViewModel();
            applied.ChainChanged += (_, _) => renameList.Preview(applied.ToChain());
            return (renameList, applied);
        }
    }
}

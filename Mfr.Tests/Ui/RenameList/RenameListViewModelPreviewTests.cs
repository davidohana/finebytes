using Avalonia.Headless.XUnit;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.Filters.Case;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Phase 10a–10c: filter-chain and membership preview, Auto-Preview toggle.
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
            applied.Steps[0].SetFilter(_LettersCase(LettersCaseMode.UpperCase));

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
            applied.Steps[0].SetFilter(_LettersCase(LettersCaseMode.UpperCase));
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
            applied.Steps[0].SetFilter(_LettersCase(LettersCaseMode.UpperCase));

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
            applied.Steps[0].SetFilter(_LettersCase(LettersCaseMode.UpperCase));

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

        /// <summary>
        /// Verifies reordering two casing steps re-runs preview in the new stack order.
        /// </summary>
        [Fact]
        public async Task Reordering_steps_applies_filters_in_new_order()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "hello.txt");
            File.WriteAllText(path, "x");

            var (renameList, applied) = _CreateWiredPanes(dir);
            await renameList.AddPathsAsync([path]).ConfigureAwait(true);
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            applied.Steps[0].SetFilter(_LettersCase(LettersCaseMode.UpperCase));
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            applied.Steps[1].SetFilter(_LettersCase(LettersCaseMode.LowerCase));
            Assert.Equal("hello.txt", renameList.Entries[0].FullFileNamePreview);

            applied.MoveStepsTo([1], targetIndex: 0);

            Assert.Equal("HELLO.txt", renameList.Entries[0].FullFileNamePreview);
        }

        /// <summary>
        /// Verifies adding files after filters are already on the stack applies the current chain (Phase 10b).
        /// </summary>
        [Fact]
        public async Task Add_after_filters_applies_current_chain()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "hello.txt");
            File.WriteAllText(path, "x");

            var (renameList, applied) = _CreateWiredPanes(dir);
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            applied.Steps[0].SetFilter(_LettersCase(LettersCaseMode.UpperCase));

            await renameList.AddPathsAsync([path]).ConfigureAwait(true);

            Assert.Equal("HELLO.txt", renameList.Entries[0].FullFileNamePreview);
            Assert.Equal(1, renameList.ChangeCount);
        }

        /// <summary>
        /// Verifies removing a changed row updates change count from the remaining membership.
        /// </summary>
        [Fact]
        public async Task Remove_updates_change_count()
        {
            var dir = _context.CreateTempDir();
            var upperPath = Path.Combine(dir, "hello.txt");
            var lowerPath = Path.Combine(dir, "WORLD.txt");
            File.WriteAllText(upperPath, "x");
            File.WriteAllText(lowerPath, "x");

            var (renameList, applied) = _CreateWiredPanes(dir);
            await renameList.AddPathsAsync([upperPath, lowerPath]).ConfigureAwait(true);
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            applied.Steps[0].SetFilter(_LettersCase(LettersCaseMode.UpperCase));
            Assert.Equal(1, renameList.ChangeCount);

            renameList.SetSelectedEntries([renameList.Entries[0]]);
            renameList.RemoveSelectedCommand.Execute(null);

            Assert.Single(renameList.Entries);
            Assert.Equal("WORLD.txt", renameList.Entries[0].FullFileNamePreview);
            Assert.Equal(0, renameList.ChangeCount);
        }

        /// <summary>
        /// Verifies clearing the Rename List zeros preview status counts.
        /// </summary>
        [Fact]
        public async Task Clear_zeros_preview_counts()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "hello.txt");
            File.WriteAllText(path, "x");

            var (renameList, applied) = _CreateWiredPanes(dir);
            await renameList.AddPathsAsync([path]).ConfigureAwait(true);
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            applied.Steps[0].SetFilter(_LettersCase(LettersCaseMode.UpperCase));
            Assert.Equal(1, renameList.ChangeCount);

            renameList.ClearCommand.Execute(null);

            Assert.Empty(renameList.Entries);
            Assert.Equal(0, renameList.ChangeCount);
            Assert.Equal(0, renameList.PreviewErrorCount);
        }

        /// <summary>
        /// Verifies row move does not change preview values or raise membership.
        /// </summary>
        [Fact]
        public async Task Row_reorder_does_not_change_preview_or_raise_membership()
        {
            var dir = _context.CreateTempDir();
            var firstPath = Path.Combine(dir, "aaa.txt");
            var secondPath = Path.Combine(dir, "bbb.txt");
            File.WriteAllText(firstPath, "x");
            File.WriteAllText(secondPath, "x");

            var (renameList, applied) = _CreateWiredPanes(dir);
            await renameList.AddPathsAsync([firstPath, secondPath]).ConfigureAwait(true);
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            applied.Steps[0].SetFilter(_LettersCase(LettersCaseMode.UpperCase));
            Assert.Equal("AAA.txt", renameList.Entries[0].FullFileNamePreview);
            Assert.Equal("BBB.txt", renameList.Entries[1].FullFileNamePreview);

            var membershipRaises = 0;
            renameList.MembershipChanged += (_, _) => membershipRaises++;
            renameList.SetSelectedEntries([renameList.Entries[1]]);
            renameList.MoveSelectedUpCommand.Execute(null);

            Assert.Equal(0, membershipRaises);
            Assert.Equal("BBB.txt", renameList.Entries[0].FullFileNamePreview);
            Assert.Equal("AAA.txt", renameList.Entries[1].FullFileNamePreview);
        }

        /// <summary>
        /// Verifies adding a path already on the list does not raise membership or re-preview.
        /// </summary>
        [Fact]
        public async Task Duplicate_add_does_not_raise_membership()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "hello.txt");
            File.WriteAllText(path, "x");

            var (renameList, applied) = _CreateWiredPanes(dir);
            await renameList.AddPathsAsync([path]).ConfigureAwait(true);
            applied.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            applied.Steps[0].SetFilter(_LettersCase(LettersCaseMode.UpperCase));
            Assert.Equal("HELLO.txt", renameList.Entries[0].FullFileNamePreview);

            var membershipRaises = 0;
            renameList.MembershipChanged += (_, _) => membershipRaises++;
            await renameList.AddPathsAsync([path]).ConfigureAwait(true);

            Assert.Equal(0, membershipRaises);
            Assert.Single(renameList.Entries);
            Assert.Equal("HELLO.txt", renameList.Entries[0].FullFileNamePreview);
        }

        /// <summary>
        /// Verifies MainWindow production wiring re-previews after add when filters are already on the stack.
        /// </summary>
        [AvaloniaFact]
        public async Task MainWindow_add_after_filters_applies_current_chain()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "hello.txt");
            File.WriteAllText(path, "x");

            var main = new MainWindowViewModel(dir);
            main.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            main.AppliedFiltersViewModel.Steps[0].SetFilter(_LettersCase(LettersCaseMode.UpperCase));
            await main.WaitForPendingPreviewAsync().ConfigureAwait(true);

            await main.RenameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);
            await main.WaitForPendingPreviewAsync().ConfigureAwait(true);

            Assert.Equal("HELLO.txt", main.RenameListViewModel.Entries[0].FullFileNamePreview);
            Assert.Equal(1, main.RenameListViewModel.ChangeCount);
        }

        /// <summary>
        /// Verifies turning Auto-Preview off skips chain-driven preview updates.
        /// </summary>
        [AvaloniaFact]
        public async Task Auto_preview_off_skips_chain_preview()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "hello.txt");
            File.WriteAllText(path, "x");

            var main = new MainWindowViewModel(dir);
            await main.RenameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);
            await main.WaitForPendingPreviewAsync().ConfigureAwait(true);

            main.RenameListViewModel.IsAutoPreview = false;
            main.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            main.AppliedFiltersViewModel.Steps[0].SetFilter(_LettersCase(LettersCaseMode.UpperCase));
            await main.WaitForPendingPreviewAsync().ConfigureAwait(true);

            Assert.Equal("hello.txt", main.RenameListViewModel.Entries[0].FullFileNamePreview);
            Assert.Equal(0, main.RenameListViewModel.ChangeCount);
        }

        /// <summary>
        /// Verifies turning Auto-Preview back on re-runs preview with the current chain.
        /// </summary>
        [AvaloniaFact]
        public async Task Auto_preview_on_repreviews_current_chain()
        {
            var dir = _context.CreateTempDir();
            var path = Path.Combine(dir, "hello.txt");
            File.WriteAllText(path, "x");

            var main = new MainWindowViewModel(dir);
            await main.RenameListViewModel.AddPathsAsync([path]).ConfigureAwait(true);
            await main.WaitForPendingPreviewAsync().ConfigureAwait(true);

            main.RenameListViewModel.IsAutoPreview = false;
            main.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            main.AppliedFiltersViewModel.Steps[0].SetFilter(_LettersCase(LettersCaseMode.UpperCase));
            Assert.Equal("hello.txt", main.RenameListViewModel.Entries[0].FullFileNamePreview);

            main.RenameListViewModel.ToggleAutoPreviewCommand.Execute(null);
            await main.WaitForPendingPreviewAsync().ConfigureAwait(true);

            Assert.True(main.RenameListViewModel.IsAutoPreview);
            Assert.Equal("HELLO.txt", main.RenameListViewModel.Entries[0].FullFileNamePreview);
            Assert.Equal(1, main.RenameListViewModel.ChangeCount);
        }

        /// <summary>
        /// Verifies canceling an in-progress preview disables Auto-Preview (MFR7).
        /// </summary>
        [Fact]
        public async Task PreviewAsync_cancel_disables_auto_preview()
        {
            var dir = _context.CreateTempDir();
            var paths = new List<string>();
            for (var i = 0; i < 40; i++)
            {
                var path = Path.Combine(dir, $"f{i:D2}.txt");
                File.WriteAllText(path, "x");
                paths.Add(path);
            }

            var renameList = _context.CreateRenameListViewModel(dir);
            await renameList.AddPathsAsync(paths).ConfigureAwait(true);
            Assert.True(renameList.IsAutoPreview);

            void OnProgressChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName is nameof(RenameListProgressViewModel.IsBusy) && renameList.IsBusy)
                {
                    renameList.Progress.CancelCommand.Execute(null);
                }
            }

            renameList.Progress.PropertyChanged += OnProgressChanged;
            try
            {
                var chain = new FilterChain
                {
                    Steps = [new FilterChainStep(Enabled: true, _LettersCase(LettersCaseMode.UpperCase))],
                };
                var completed = await renameList.PreviewAsync(chain).ConfigureAwait(true);

                Assert.False(completed);
                Assert.False(renameList.IsAutoPreview);
            }
            finally
            {
                renameList.Progress.PropertyChanged -= OnProgressChanged;
            }
        }

        private (RenameListViewModel RenameList, AppliedFiltersViewModel Applied) _CreateWiredPanes(string dir)
        {
            var renameList = _context.CreateRenameListViewModel(dir);
            var applied = new AppliedFiltersViewModel();
            applied.ChainChanged += (_, _) => renameList.Preview(applied.ToChain());
            renameList.MembershipChanged += (_, _) => renameList.Preview(applied.ToChain());
            return (renameList, applied);
        }

        private static LettersCaseFilter _LettersCase(LettersCaseMode mode)
        {
            return new LettersCaseFilter(new FilePrefixTarget(), new LettersCaseOptions(mode, CapitalizeSkipWords: []));
        }
    }
}

using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Clear-command confirmation gates for the Rename List.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class RenameListClearConfirmTests : IDisposable
    {
        private readonly RenameListUiTestContext _context = new();

        public RenameListClearConfirmTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Verifies clear confirms by default and clears when accepted.
        /// </summary>
        [Fact]
        public async Task Clear_confirm_accept_clears_list()
        {
            var dir = _context.CreateTempDir();
            await File.WriteAllTextAsync(Path.Combine(dir, "a.txt"), "a");
            var renameList = new RenameListViewModel(_context.CreateFileListViewModel(dir));
            await renameList.AddPathsAsync([Path.Combine(dir, "a.txt")]);

            var asked = 0;
            renameList.UiHooks = new RenameListUiHooks
            {
                ConfirmClearAsync = () =>
                {
                    asked++;
                    return Task.FromResult(true);
                },
            };

            await renameList.ClearCommand.ExecuteAsync(null);

            Assert.Equal(1, asked);
            Assert.Empty(renameList.Entries);
        }

        /// <summary>
        /// Verifies decline leaves the Rename List unchanged.
        /// </summary>
        [Fact]
        public async Task Clear_confirm_decline_keeps_list()
        {
            var dir = _context.CreateTempDir();
            await File.WriteAllTextAsync(Path.Combine(dir, "a.txt"), "a");
            var renameList = new RenameListViewModel(_context.CreateFileListViewModel(dir));
            await renameList.AddPathsAsync([Path.Combine(dir, "a.txt")]);
            renameList.UiHooks = new RenameListUiHooks { ConfirmClearAsync = () => Task.FromResult(false) };

            await renameList.ClearCommand.ExecuteAsync(null);

            Assert.Single(renameList.Entries);
        }

        /// <summary>
        /// Verifies a suppressed ClearRenameList skips the confirm hook.
        /// </summary>
        [Fact]
        public async Task Clear_suppressed_skips_confirm()
        {
            ConfirmationPolicy.Suppress(ConfirmationKind.ClearRenameList);
            var dir = _context.CreateTempDir();
            await File.WriteAllTextAsync(Path.Combine(dir, "a.txt"), "a");
            var renameList = new RenameListViewModel(_context.CreateFileListViewModel(dir));
            await renameList.AddPathsAsync([Path.Combine(dir, "a.txt")]);

            var asked = 0;
            renameList.UiHooks = new RenameListUiHooks
            {
                ConfirmClearAsync = () =>
                {
                    asked++;
                    return Task.FromResult(false);
                },
            };

            await renameList.ClearCommand.ExecuteAsync(null);

            Assert.Equal(0, asked);
            Assert.Empty(renameList.Entries);
        }

        /// <summary>
        /// Verifies a missing confirm hook aborts and leaves the list unchanged.
        /// </summary>
        [Fact]
        public async Task Clear_null_hook_aborts()
        {
            var dir = _context.CreateTempDir();
            await File.WriteAllTextAsync(Path.Combine(dir, "a.txt"), "a");
            var renameList = new RenameListViewModel(_context.CreateFileListViewModel(dir));
            await renameList.AddPathsAsync([Path.Combine(dir, "a.txt")]);

            await renameList.ClearCommand.ExecuteAsync(null);

            Assert.Single(renameList.Entries);
        }

        /// <summary>
        /// Verifies Alt+drag clear bypasses the confirmation gate.
        /// </summary>
        [Fact]
        public async Task ClearWithoutConfirm_skips_hook()
        {
            var dir = _context.CreateTempDir();
            await File.WriteAllTextAsync(Path.Combine(dir, "a.txt"), "a");
            var renameList = new RenameListViewModel(_context.CreateFileListViewModel(dir));
            await renameList.AddPathsAsync([Path.Combine(dir, "a.txt")]);

            var asked = 0;
            renameList.UiHooks = new RenameListUiHooks
            {
                ConfirmClearAsync = () =>
                {
                    asked++;
                    return Task.FromResult(false);
                },
            };

            renameList.ClearWithoutConfirm();

            Assert.Equal(0, asked);
            Assert.Empty(renameList.Entries);
        }
    }
}

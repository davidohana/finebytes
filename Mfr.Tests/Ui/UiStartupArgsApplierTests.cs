using Avalonia.Headless.XUnit;
using Mfr.App.Ui;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.Services.Shell;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.Models.Config;
using Mfr.Utils;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests desktop startup argv apply after main window / pane VMs exist.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class UiStartupArgsApplierTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Resets Options-owned add policy to defaults for each test.
        /// </summary>
        public UiStartupArgsApplierTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies sources-only seeding adds Rename List rows and locates the first item.
        /// </summary>
        [AvaloniaFact]
        public async Task Apply_Sources_Only_Seeds_Rename_List_And_Locates_First()
        {
            var dir = _CreateSampleFolder();
            var other = _tempDirectoryFixture.CreateTempDir();
            var viewModel = _CreateMain(other);
            var alphaPath = Path.Combine(dir, "alpha.txt");

            await UiStartupArgsApplier.ApplyAsync(viewModel, [alphaPath, Path.Combine(dir, "beta.md")]);

            Assert.Equal(2, viewModel.RenameListViewModel.Entries.Count);
            Assert.Equal(["alpha.txt", "beta.md"], _PreviewNames(viewModel));
            Assert.True(PathComparers.Os.Equals(dir, viewModel.FileListViewModel.CurrentPath));
            Assert.Contains(
                viewModel.FileListViewModel.SelectedEntries,
                entry => PathComparers.Os.Equals(entry.FullPath, alphaPath)
            );
        }

        /// <summary>
        /// Verifies sources may include wildcards expanded by the engine (console-shaped).
        /// </summary>
        [AvaloniaFact]
        public async Task Apply_Sources_Wildcard_Expands_Like_Console()
        {
            var dir = _CreateSampleFolder();
            var viewModel = _CreateMain(dir);

            await UiStartupArgsApplier.ApplyAsync(viewModel, [Path.Combine(dir, "*.txt")]);

            Assert.Single(viewModel.RenameListViewModel.Entries);
            Assert.Equal("alpha.txt", viewModel.RenameListViewModel.Entries[0].FullFileName);
        }

        /// <summary>
        /// Verifies <c>--initial-folder</c> alone navigates the File List without seeding.
        /// </summary>
        [AvaloniaFact]
        public async Task Apply_InitialFolder_Only_Navigates_File_List()
        {
            var start = _tempDirectoryFixture.CreateTempDir();
            var browse = _CreateSampleFolder();
            var viewModel = _CreateMain(start);

            await UiStartupArgsApplier.ApplyAsync(viewModel, ["--initial-folder", browse]);

            Assert.Empty(viewModel.RenameListViewModel.Entries);
            Assert.True(PathComparers.Os.Equals(browse, viewModel.FileListViewModel.CurrentPath));
        }

        /// <summary>
        /// Verifies combined sources + <c>--initial-folder</c> seeds and prefers the browse path.
        /// </summary>
        [AvaloniaFact]
        public async Task Apply_Sources_And_InitialFolder_Seeds_And_Uses_Browse_Path()
        {
            var sourceDir = _CreateSampleFolder();
            var browseDir = _tempDirectoryFixture.CreateTempDir();
            var start = _tempDirectoryFixture.CreateTempDir();
            var viewModel = _CreateMain(start);
            var alphaPath = Path.Combine(sourceDir, "alpha.txt");

            await UiStartupArgsApplier.ApplyAsync(viewModel, [alphaPath, "--initial-folder", browseDir]);

            Assert.Single(viewModel.RenameListViewModel.Entries);
            Assert.True(PathComparers.Os.Equals(browseDir, viewModel.FileListViewModel.CurrentPath));
            Assert.DoesNotContain(
                viewModel.FileListViewModel.SelectedEntries,
                entry => PathComparers.Os.Equals(entry.FullPath, alphaPath)
            );
        }

        /// <summary>
        /// Verifies omitted add modifiers fall back to Options prefs.
        /// </summary>
        [AvaloniaFact]
        public async Task Apply_Sources_Uses_Options_Prefs_When_Modifiers_Omitted()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            ConfigStore.Options.AddMode = RenameListAddMode.Folders;
            ConfigStore.Options.AddFolderContents = false;
            var viewModel = _CreateMain(parent);

            await UiStartupArgsApplier.ApplyAsync(viewModel, [albumPath]);

            Assert.Single(viewModel.RenameListViewModel.Entries);
            Assert.Equal("album", viewModel.RenameListViewModel.Entries[0].FullFileName);
            Assert.Equal("Folder", viewModel.RenameListViewModel.Entries[0].FileFolder);
        }

        /// <summary>
        /// Verifies argv add modifiers override Options prefs when sources are present.
        /// </summary>
        [AvaloniaFact]
        public async Task Apply_Sources_Modifier_Overrides_Beat_Prefs()
        {
            var (parent, albumPath) = _CreateAlbumTree();
            ConfigStore.Options.AddMode = RenameListAddMode.Files;
            ConfigStore.Options.AddFolderContents = true;
            var viewModel = _CreateMain(parent);

            await UiStartupArgsApplier.ApplyAsync(
                viewModel,
                [albumPath, "--files", "no", "--folders", "yes", "--recursive"]
            );

            var names = _PreviewNames(viewModel).OrderBy(n => n, StringComparer.Ordinal).ToList();
            Assert.Equal(["album", "disc1"], names);
        }

        /// <summary>
        /// Verifies orphan add modifiers are ignored when no sources were supplied.
        /// </summary>
        [AvaloniaFact]
        public async Task Apply_Orphan_Modifiers_Without_Sources_Are_Ignored()
        {
            var dir = _CreateSampleFolder();
            var viewModel = _CreateMain(dir);
            var beforePath = viewModel.FileListViewModel.CurrentPath;

            await UiStartupArgsApplier.ApplyAsync(viewModel, ["--files", "no", "--include-hidden"]);

            Assert.Empty(viewModel.RenameListViewModel.Entries);
            Assert.True(PathComparers.Os.Equals(beforePath, viewModel.FileListViewModel.CurrentPath));
        }

        /// <summary>
        /// Verifies startup seeding ignores File List exclude masks (console-shaped sources).
        /// </summary>
        [AvaloniaFact]
        public async Task Apply_Sources_Ignores_File_List_Exclude_Masks()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            File.WriteAllText(Path.Combine(dir, "keep.txt"), "k");
            File.WriteAllText(Path.Combine(dir, "skip.exe"), "e");
            var viewModel = _CreateMain(dir);
            viewModel.FileListViewModel.ExcludeMasksEnabled = true;
            viewModel.FileListViewModel.ExcludeMasks = ["*.exe"];

            await UiStartupArgsApplier.ApplyAsync(viewModel, [Path.Combine(dir, "*")]);

            var names = _PreviewNames(viewModel).OrderBy(n => n, StringComparer.Ordinal).ToList();
            Assert.Equal(["keep.txt", "skip.exe"], names);
        }

        /// <summary>
        /// Verifies invalid source paths soft-fail without crashing (UI still usable).
        /// </summary>
        [AvaloniaFact]
        public async Task Apply_Invalid_Source_Soft_Fails()
        {
            var dir = _CreateSampleFolder();
            var missing = Path.Combine(dir, "missing-" + Guid.NewGuid().ToString("N") + ".txt");
            var viewModel = _CreateMain(dir);

            await UiStartupArgsApplier.ApplyAsync(viewModel, [missing]);

            Assert.Empty(viewModel.RenameListViewModel.Entries);
            Assert.Equal("No items were added.", viewModel.RenameListViewModel.LastStatusMessage.ToPlainText());
        }

        /// <summary>
        /// Verifies invalid <c>--initial-folder</c> soft-fails with a File List status message.
        /// </summary>
        [AvaloniaFact]
        public async Task Apply_Invalid_InitialFolder_Soft_Fails()
        {
            var dir = _CreateSampleFolder();
            var missing = Path.Combine(dir, "missing-" + Guid.NewGuid().ToString("N"));
            var viewModel = _CreateMain(dir);

            await UiStartupArgsApplier.ApplyAsync(viewModel, ["--initial-folder", missing]);

            Assert.Empty(viewModel.RenameListViewModel.Entries);
            Assert.Equal(
                FileListCatalog.FormatListingError(FileListListingFailure.NotFound),
                viewModel.FileListViewModel.LastStatusMessage.ToPlainText()
            );
        }

        /// <summary>
        /// Verifies parse failures set status and do not throw.
        /// </summary>
        [AvaloniaFact]
        public async Task Apply_Parse_Failure_Sets_Status_Without_Throwing()
        {
            var dir = _CreateSampleFolder();
            var viewModel = _CreateMain(dir);

            await UiStartupArgsApplier.ApplyAsync(viewModel, ["--not-a-real-option"]);

            Assert.Empty(viewModel.RenameListViewModel.Entries);
            Assert.Contains("Unknown option", viewModel.StatusHint.ToPlainText(), StringComparison.Ordinal);
        }

        private MainWindowViewModel _CreateMain(string initialPath)
        {
            var viewModel = new MainWindowViewModel(initialPath, shellOpener: NullFileShellOpener.Instance);
            FileListListingWait.WaitUntilIdle(viewModel.FileListViewModel);
            return viewModel;
        }

        private string _CreateSampleFolder()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            File.WriteAllText(Path.Combine(dir, "alpha.txt"), "a");
            File.WriteAllText(Path.Combine(dir, "beta.md"), "b");
            return dir;
        }

        private (string Parent, string AlbumPath) _CreateAlbumTree()
        {
            var parent = _tempDirectoryFixture.CreateTempDir();
            var albumPath = Directory.CreateDirectory(Path.Combine(parent, "album")).FullName;
            File.WriteAllText(Path.Combine(albumPath, "track.mp3"), "t");
            File.WriteAllText(Path.Combine(albumPath, "readme.txt"), "r");
            var disc1 = Directory.CreateDirectory(Path.Combine(albumPath, "disc1")).FullName;
            File.WriteAllText(Path.Combine(disc1, "nested.mp3"), "n");
            return (parent, albumPath);
        }

        private static IReadOnlyList<string> _PreviewNames(MainWindowViewModel viewModel)
        {
            return [.. viewModel.RenameListViewModel.Entries.Select(entry => entry.FullFileName)];
        }
    }
}

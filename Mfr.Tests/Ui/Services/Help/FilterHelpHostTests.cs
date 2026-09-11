using Mfr.App.Ui.Services.Help;
using Mfr.App.Ui.Services.Shell;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.Services.Help
{
    /// <summary>
    /// Unit tests for <see cref="FilterHelpHost"/> path resolution and open.
    /// </summary>
    public sealed class FilterHelpHostTests
    {
        /// <summary>
        /// Verifies the host finds a help file under a configured root and opens it.
        /// </summary>
        [Fact]
        public void TryOpen_resolves_and_opens_existing_file()
        {
            var helpDir = Path.Combine(Path.GetTempPath(), $"mfr-help-{Guid.NewGuid():N}");
            Directory.CreateDirectory(helpDir);
            try
            {
                var helpFile = Path.Combine(helpDir, "SpaceCharacter.html");
                File.WriteAllText(helpFile, "<html></html>");
                var opener = new RecordingShellOpener();
                var host = new FilterHelpHost(opener, [helpDir]);

                Assert.True(host.TryOpen("SpaceCharacter.html", out var fullPath));
                Assert.Equal(helpFile, fullPath);
                Assert.Equal([helpFile], opener.OpenedPaths);
            }
            finally
            {
                Directory.Delete(helpDir, recursive: true);
            }
        }

        /// <summary>
        /// Verifies missing help files do not open and return false.
        /// </summary>
        [Fact]
        public void TryOpen_returns_false_when_missing()
        {
            var helpDir = Path.Combine(Path.GetTempPath(), $"mfr-help-missing-{Guid.NewGuid():N}");
            Directory.CreateDirectory(helpDir);
            try
            {
                var opener = new RecordingShellOpener();
                var host = new FilterHelpHost(opener, [helpDir]);

                Assert.False(host.TryOpen("SpaceCharacter.html", out var fullPath));
                Assert.Null(fullPath);
                Assert.Empty(opener.OpenedPaths);
            }
            finally
            {
                Directory.Delete(helpDir, recursive: true);
            }
        }

        /// <summary>
        /// Verifies path segments in the help file name are rejected.
        /// </summary>
        [Fact]
        public void TryResolve_rejects_path_segments()
        {
            var host = new FilterHelpHost(NullFileShellOpener.Instance, [@"C:\nowhere"]);
            Assert.False(host.TryResolve(@"..\SpaceCharacter.html", out _));
            Assert.False(host.TryResolve(@"sub\SpaceCharacter.html", out _));
        }

        /// <summary>
        /// Verifies the missing-help message points at the app help folder.
        /// </summary>
        [Fact]
        public void FormatMissingHelpMessage_lists_default_roots()
        {
            var message = FilterHelpHost.FormatMissingHelpMessage("SpaceCharacter.html");
            Assert.Contains("SpaceCharacter.html", message, StringComparison.Ordinal);
            Assert.Contains("application folder", message, StringComparison.OrdinalIgnoreCase);
            foreach (var root in FilterHelpHost.DefaultHelpRoots)
            {
                Assert.Contains(root, message, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Verifies default roots are only the app-local <c>help/</c> folder.
        /// </summary>
        [Fact]
        public void DefaultHelpRoots_is_app_base_help_only()
        {
            Assert.Equal([Path.Combine(AppContext.BaseDirectory, "help")], FilterHelpHost.DefaultHelpRoots);
        }
    }

    /// <summary>
    /// Applied Filters help command enablement and open wiring.
    /// </summary>
    public sealed class AppliedFiltersFilterHelpTests
    {
        /// <summary>
        /// Verifies help is enabled for a single mapped selection and opens the file.
        /// </summary>
        [Fact]
        public void OpenSelectedFilterHelp_opens_mapped_file()
        {
            var helpDir = Path.Combine(Path.GetTempPath(), $"mfr-help-cmd-{Guid.NewGuid():N}");
            Directory.CreateDirectory(helpDir);
            try
            {
                var helpFile = Path.Combine(helpDir, "LettersCase.html");
                File.WriteAllText(helpFile, "<html></html>");
                var opener = new RecordingShellOpener();
                var viewModel = new AppliedFiltersViewModel(filterHelp: new FilterHelpHost(opener, [helpDir]));
                viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));

                Assert.True(viewModel.OpenSelectedFilterHelpCommand.CanExecute(null));
                string? missing = null;
                viewModel.FilterHelpMissing += (_, fileName) => missing = fileName;

                viewModel.OpenSelectedFilterHelpCommand.Execute(null);

                Assert.Null(missing);
                Assert.Equal([helpFile], opener.OpenedPaths);
            }
            finally
            {
                Directory.Delete(helpDir, recursive: true);
            }
        }

        /// <summary>
        /// Verifies help raises <see cref="AppliedFiltersViewModel.FilterHelpMissing"/> when the file is absent.
        /// </summary>
        [Fact]
        public void OpenSelectedFilterHelp_raises_missing_when_file_absent()
        {
            var helpDir = Path.Combine(Path.GetTempPath(), $"mfr-help-absent-{Guid.NewGuid():N}");
            Directory.CreateDirectory(helpDir);
            try
            {
                var opener = new RecordingShellOpener();
                var viewModel = new AppliedFiltersViewModel(filterHelp: new FilterHelpHost(opener, [helpDir]));
                viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("SpaceCharacter"));
                string? missing = null;
                viewModel.FilterHelpMissing += (_, fileName) => missing = fileName;

                viewModel.OpenSelectedFilterHelpCommand.Execute(null);

                Assert.Equal("SpaceCharacter.html", missing);
                Assert.Empty(opener.OpenedPaths);
            }
            finally
            {
                Directory.Delete(helpDir, recursive: true);
            }
        }

        /// <summary>
        /// Verifies help is disabled for multi-select (same as Filter Options / pin / reset).
        /// </summary>
        [Fact]
        public void OpenSelectedFilterHelp_disabled_for_multi_select()
        {
            var viewModel = new AppliedFiltersViewModel(
                filterHelp: new FilterHelpHost(NullFileShellOpener.Instance, [])
            );
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("LettersCase"));
            viewModel.SetSelectedSteps([]);
            viewModel.AddCommand.Execute(AppliedFiltersTestUi.Entry("ShrinkSpaces"));
            viewModel.SetSelectedSteps([viewModel.Steps[0], viewModel.Steps[1]]);

            Assert.False(viewModel.OpenSelectedFilterHelpCommand.CanExecute(null));
        }

        /// <summary>
        /// Verifies help is disabled with an empty Applied list.
        /// </summary>
        [Fact]
        public void OpenSelectedFilterHelp_disabled_when_empty()
        {
            var viewModel = new AppliedFiltersViewModel(
                filterHelp: new FilterHelpHost(NullFileShellOpener.Instance, [])
            );
            Assert.False(viewModel.OpenSelectedFilterHelpCommand.CanExecute(null));
        }
    }

    /// <summary>
    /// Records <see cref="IFileShellOpener.OpenWithDefaultApp"/> calls for tests.
    /// </summary>
    file sealed class RecordingShellOpener : IFileShellOpener
    {
        public List<string> OpenedPaths { get; } = [];

        public void OpenWithDefaultApp(string path)
        {
            OpenedPaths.Add(path);
        }

        public void RevealInFileManager(string path) { }

        public void OpenFolderInFileManager(string folderPath) { }

        public void ShowProperties(string path) { }
    }
}

using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.Filters.Replace;
using Mfr.Models.Config;
using Mfr.Tests.Ui.FilterChainPane;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Shared GO→last-op setup for Undo prepare / Log Undo UI tests.
    /// </summary>
    internal static class UndoPrepareTestUi
    {
        /// <summary>
        /// Creates <c>alpha.txt</c>, runs GO with a literal prefix replacer to <c>renamed.txt</c>, and
        /// suppresses GO/Undo confirms.
        /// </summary>
        /// <param name="tempDirectoryFixture">Temp directory owner.</param>
        /// <param name="disableAutoPreview">When <see langword="true"/>, turns Auto-Preview off before add.</param>
        /// <returns>Window VM plus source/destination paths after GO.</returns>
        public static async Task<(
            MainWindowViewModel ViewModel,
            string Source,
            string Destination
        )> GoPrefixRenameAsync(TempDirectoryFixture tempDirectoryFixture, bool disableAutoPreview = true)
        {
            ArgumentNullException.ThrowIfNull(tempDirectoryFixture);

            var dir = tempDirectoryFixture.CreateTempDir();
            var source = Path.Combine(dir, "alpha.txt");
            var destination = Path.Combine(dir, "renamed.txt");
            await File.WriteAllTextAsync(source, "alpha").ConfigureAwait(true);

            var viewModel = new MainWindowViewModel(dir);
            if (disableAutoPreview)
            {
                viewModel.RenameListViewModel.DisableAutoPreview();
            }

            await viewModel.RenameListViewModel.AddPathsAsync([source]).ConfigureAwait(true);
            viewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("Replacer"));
            viewModel.FilterChainViewModel.Steps[0].SetFilter(PrefixReplacer("alpha", "renamed"));
            ConfigStore.Options.SuppressedConfirmations =
            [
                ConfirmationKind.GoWithPreviewErrors,
                ConfirmationKind.UndoRename,
            ];

            await viewModel.GoCommand.ExecuteAsync(null).ConfigureAwait(true);
            return (viewModel, source, destination);
        }

        /// <summary>
        /// Builds a literal whole-prefix <see cref="ReplacerFilter"/> on the file name.
        /// </summary>
        /// <param name="find">Find text.</param>
        /// <param name="replacement">Replacement text.</param>
        /// <returns>Configured filter.</returns>
        public static ReplacerFilter PrefixReplacer(string find, string replacement)
        {
            return new ReplacerFilter(
                Target: new FilePrefixTarget(),
                Options: new ReplacerOptions(
                    Find: find,
                    Replacement: replacement,
                    Match: new ReplacerMatchOptions(
                        Mode: ReplacerMode.Literal,
                        CaseSensitive: true,
                        ReplaceAll: false,
                        WholeWord: false
                    )
                )
            );
        }
    }
}

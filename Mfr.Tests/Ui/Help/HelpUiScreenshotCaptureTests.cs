using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui;
using Mfr.App.Ui.Services.Shell;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.App.Ui.Views.FileList;
using Mfr.App.Ui.Views.FilterChainPane;
using Mfr.App.Ui.Views.FilterEditors;
using Mfr.App.Ui.Views.FilterPalette;
using Mfr.App.Ui.Views.RenameList;
using Mfr.Tests.Ui.FilterChainPane;
using AppMainWindow = Mfr.App.Ui.Views.MainWindow.MainWindow;

namespace Mfr.Tests.Ui.Help
{
    /// <summary>
    /// One-shot capture of main-window / pane screenshots for non-filter help pages.
    /// <para>
    /// Run with <c>MFR_CAPTURE_HELP_SCREENSHOTS=1</c>. No-ops otherwise.
    /// </para>
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class HelpUiScreenshotCaptureTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Resets prefs so captures do not pick up leftover session chrome.
        /// </summary>
        public HelpUiScreenshotCaptureTests()
        {
            ConfigStoreTestReset.LoadEmpty();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Renders P0 shell shots under <c>help/images/ui/</c>.
        /// </summary>
        [AvaloniaFact]
        public async Task Capture_p0_shell_panes_to_help_images()
        {
            if (Environment.GetEnvironmentVariable("MFR_CAPTURE_HELP_SCREENSHOTS") != "1")
            {
                return;
            }

            var sampleDir = _CreateSampleFolder();
            var outputDir = Path.Combine(_ResolveHelpImagesDirectory(), "ui");
            Directory.CreateDirectory(outputDir);

            var viewModel = new MainWindowViewModel(
                initialFileListPath: sampleDir,
                persistSession: false,
                shellOpener: NullFileShellOpener.Instance
            );
            FileListListingWait.WaitUntilIdle(viewModel.FileListViewModel);
            Assert.False(viewModel.FileListViewModel.HasListingError, viewModel.FileListViewModel.ListingError);
            Assert.NotEmpty(viewModel.FileListViewModel.Entries);

            var window = new AppMainWindow
            {
                DataContext = viewModel,
                Width = 1280,
                Height = 860,
                Background = Brushes.White,
            };

            try
            {
                window.Show();
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
                FileListListingWait.WaitUntilIdle(viewModel.FileListViewModel);

                var sources = Directory
                    .GetFiles(sampleDir)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                await UiStartupArgsApplier.ApplyAsync(viewModel, sources);
                FileListListingWait.WaitUntilIdle(viewModel.FileListViewModel);
                Assert.False(viewModel.FileListViewModel.HasListingError, viewModel.FileListViewModel.ListingError);
                Assert.Equal(5, viewModel.RenameListViewModel.Entries.Count);

                viewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("SpaceCharacter"));
                viewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("LettersCase"));
                viewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("ShrinkSpaces"));
                // Prefer an options-bearing step so Filter Configuration is not an empty body.
                viewModel.FilterChainViewModel.SetSelectedSteps([viewModel.FilterChainViewModel.Steps[1]]);
                viewModel.FilterEditorViewModel.FormatTokenPickerExpanded = false;
                if (viewModel.FilterEditorViewModel.OptionsEditor is { } optionsEditor)
                {
                    optionsEditor.FormatTokenPickerExpanded = false;
                }

                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
                FileListListingWait.WaitUntilIdle(viewModel.FileListViewModel);
                await Task.Delay(300);
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                Assert.False(viewModel.FileListViewModel.ShowListingBusy);
                Assert.False(viewModel.FileListViewModel.HasListingError, viewModel.FileListViewModel.ListingError);

                _CaptureControl(window, Path.Combine(outputDir, "main-window.png"));

                var fileList = window.GetVisualDescendants().OfType<FileListView>().Single();
                var renameList = window.GetVisualDescendants().OfType<RenameListView>().Single();
                var palette = window.GetVisualDescendants().OfType<FilterPaletteView>().Single();
                var chain = window.GetVisualDescendants().OfType<FilterChainView>().Single();
                var filterEditor = window.GetVisualDescendants().OfType<FilterEditorView>().Single();

                _CaptureControl(fileList, Path.Combine(outputDir, "file-list.png"));
                _CaptureControl(renameList, Path.Combine(outputDir, "rename-list.png"));
                _CaptureControl(palette, Path.Combine(outputDir, "available-filters.png"));
                _CaptureControl(chain, Path.Combine(outputDir, "filter-chain.png"));
                _CaptureControl(filterEditor, Path.Combine(outputDir, "filter-configuration.png"));

                foreach (
                    var name in new[]
                    {
                        "main-window.png",
                        "file-list.png",
                        "rename-list.png",
                        "available-filters.png",
                        "filter-chain.png",
                        "filter-configuration.png",
                    }
                )
                {
                    Assert.True(File.Exists(Path.Combine(outputDir, name)), "Missing " + name);
                }
            }
            finally
            {
                window.Close();
            }
        }

        private static void _CaptureControl(Control control, string path)
        {
            ArgumentNullException.ThrowIfNull(control);

            control.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var bounds = control.Bounds;
            var pixelWidth = Math.Max(1, (int)Math.Ceiling(bounds.Width));
            var pixelHeight = Math.Max(1, (int)Math.Ceiling(bounds.Height));
            Assert.True(pixelWidth > 40 && pixelHeight > 40, $"{path} too small: {pixelWidth}x{pixelHeight}");

            using var bitmap = new RenderTargetBitmap(new PixelSize(pixelWidth, pixelHeight), new Vector(96, 96));
            bitmap.Render(control);
            bitmap.Save(path, PngBitmapEncoderOptions.Default);
        }

        private string _CreateSampleFolder()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            File.WriteAllText(Path.Combine(dir, "Blue Train.mp3"), "x");
            File.WriteAllText(Path.Combine(dir, "report_draft.txt"), "x");
            File.WriteAllText(Path.Combine(dir, "IMG_0001.jpg"), "x");
            File.WriteAllText(Path.Combine(dir, "Chapter 01.pdf"), "x");
            File.WriteAllText(Path.Combine(dir, "a b c.doc"), "x");
            return dir;
        }

        private static string _ResolveHelpImagesDirectory([CallerFilePath] string sourceFile = "")
        {
            var testsDir = Path.GetDirectoryName(sourceFile)!;
            var root = Path.GetFullPath(Path.Combine(testsDir, "..", "..", ".."));
            return Path.Combine(root, "help", "images");
        }
    }
}

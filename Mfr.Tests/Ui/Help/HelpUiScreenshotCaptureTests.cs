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
using Mfr.App.Ui.ViewModels.FilterChainPane;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.App.Ui.ViewModels.LogDialog;
using Mfr.App.Ui.ViewModels.MainWindow;
using Mfr.App.Ui.ViewModels.Options;
using Mfr.App.Ui.ViewModels.Presets;
using Mfr.App.Ui.ViewModels.RenameList;
using Mfr.App.Ui.Views;
using Mfr.App.Ui.Views.FileList;
using Mfr.App.Ui.Views.FilterChainPane;
using Mfr.App.Ui.Views.FilterEditors;
using Mfr.App.Ui.Views.FilterEditors.Trimming;
using Mfr.App.Ui.Views.FilterPalette;
using Mfr.App.Ui.Views.FormatEditor;
using Mfr.App.Ui.Views.LogDialog;
using Mfr.App.Ui.Views.Options;
using Mfr.App.Ui.Views.Presets;
using Mfr.App.Ui.Views.RenameList;
using Mfr.Filters.Case;
using Mfr.Tests.Ui.FilterChainPane;
using Mfr.Tests.Ui.FilterEditors;
using Mfr.Tests.Ui.Presets;
using Mfr.Tests.Ui.RenameList;
using AppMainWindow = Mfr.App.Ui.Views.MainWindow.MainWindow;
using FormatEditorControl = Mfr.App.Ui.Views.FormatEditor.FormatEditor;

namespace Mfr.Tests.Ui.Help
{
    /// <summary>
    /// One-shot capture of main-window / pane / dialog screenshots for non-filter help pages.
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
            RenameLogStore.ClearLastOperation();
            ConfigStore.RenameLog.Limit = 0;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            RenameLogStore.ClearLastOperation();
            ConfigStoreTestReset.LoadEmpty();
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

        /// <summary>
        /// Renders P1 dialog / tool shots under <c>help/images/ui/</c>.
        /// </summary>
        [AvaloniaFact]
        public async Task Capture_p1_dialogs_and_tools_to_help_images()
        {
            if (Environment.GetEnvironmentVariable("MFR_CAPTURE_HELP_SCREENSHOTS") != "1")
            {
                return;
            }

            var outputDir = Path.Combine(_ResolveHelpImagesDirectory(), "ui");
            Directory.CreateDirectory(outputDir);

            _CaptureFilterOptions(outputDir);
            _CaptureOptions(outputDir);
            _CapturePresetManager(outputDir);
            _CaptureRenameLog(outputDir);
            _CaptureFormatEditor(outputDir);
            _CaptureFieldShuttle(outputDir);
            _CaptureAutoSort(outputDir);
            _CaptureVisualTrim(outputDir);
            await _CaptureStatusBarAsync(outputDir);

            foreach (
                var name in new[]
                {
                    "filter-options.png",
                    "options.png",
                    "preset-manager.png",
                    "rename-log.png",
                    "format-editor.png",
                    "field-shuttle.png",
                    "auto-sort.png",
                    "visual-trim.png",
                    "status-bar.png",
                }
            )
            {
                Assert.True(File.Exists(Path.Combine(outputDir, name)), "Missing " + name);
            }
        }

        /// <summary>
        /// Renders P2 howto / tutorial shots under <c>help/images/guide/</c>.
        /// </summary>
        [AvaloniaFact]
        public async Task Capture_p2_guide_shots_to_help_images()
        {
            if (Environment.GetEnvironmentVariable("MFR_CAPTURE_HELP_SCREENSHOTS") != "1")
            {
                return;
            }

            var outputDir = Path.Combine(_ResolveHelpImagesDirectory(), "guide");
            Directory.CreateDirectory(outputDir);

            await _CaptureTutorialOverviewAsync(outputDir);
            await _CaptureApplyGoAsync(outputDir);
            await _CaptureUndoLastAsync(outputDir);
            _CaptureSavePreset(outputDir);
            _CaptureResetConfig(outputDir);

            foreach (
                var name in new[]
                {
                    "tutorial-overview.png",
                    "apply-go.png",
                    "undo-last.png",
                    "save-preset.png",
                    "reset-config.png",
                }
            )
            {
                Assert.True(File.Exists(Path.Combine(outputDir, name)), "Missing " + name);
            }
        }

        private async Task _CaptureTutorialOverviewAsync(string outputDir)
        {
            // Prefer reuse of the P0 main-window shot when present.
            var mainWindowPath = Path.Combine(_ResolveHelpImagesDirectory(), "ui", "main-window.png");
            var dest = Path.Combine(outputDir, "tutorial-overview.png");
            if (File.Exists(mainWindowPath))
            {
                File.Copy(mainWindowPath, dest, overwrite: true);
                return;
            }

            var (window, _) = await _ShowSeededMainWindowAsync();
            try
            {
                _CaptureControl(window, dest);
            }
            finally
            {
                window.Close();
            }
        }

        private async Task _CaptureApplyGoAsync(string outputDir)
        {
            var (window, _) = await _ShowSeededMainWindowAsync();
            try
            {
                var toolbar = window
                    .GetVisualDescendants()
                    .OfType<Border>()
                    .First(border => border.Classes.Contains("pane-action-strip"));
                toolbar.Background = Brushes.White;
                _CaptureControl(toolbar, Path.Combine(outputDir, "apply-go.png"), minWidth: 80, minHeight: 20);
            }
            finally
            {
                window.Close();
            }
        }

        private async Task _CaptureUndoLastAsync(string outputDir)
        {
            var (viewModel, _, _) = await UndoPrepareTestUi
                .GoPrefixRenameAsync(_tempDirectoryFixture, disableAutoPreview: true)
                .ConfigureAwait(true);

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

                await viewModel.UndoLastCommand.ExecuteAsync(null).ConfigureAwait(true);
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
                await Task.Delay(200);
                Dispatcher.UIThread.RunJobs();

                Assert.Contains("Prepared undo", viewModel.StatusHint.ToPlainText());
                _CaptureControl(window, Path.Combine(outputDir, "undo-last.png"));
            }
            finally
            {
                window.Close();
            }
        }

        private static void _CaptureSavePreset(string outputDir)
        {
            var viewModel = new SavePresetDialogViewModel
            {
                Name = "Music tags cleanup",
                Description = "Normalize tags and file names for a music folder.",
                SaveRenameListColumns = true,
            };
            var dialog = new SavePresetDialog(viewModel);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                _CaptureControl(dialog, Path.Combine(outputDir, "save-preset.png"));
            }
            finally
            {
                dialog.Close();
            }
        }

        private static void _CaptureResetConfig(string outputDir)
        {
            var dialog = new ConfirmMessageDialog(
                title: "Confirmation",
                message: "Reset configuration to default values? Magic File Renamer will close and restart."
            );
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                _CaptureControl(dialog, Path.Combine(outputDir, "reset-config.png"));
            }
            finally
            {
                dialog.Close();
            }
        }

        private async Task<(AppMainWindow Window, MainWindowViewModel ViewModel)> _ShowSeededMainWindowAsync()
        {
            var sampleDir = _CreateSampleFolder();
            var viewModel = new MainWindowViewModel(
                initialFileListPath: sampleDir,
                persistSession: false,
                shellOpener: NullFileShellOpener.Instance
            );
            FileListListingWait.WaitUntilIdle(viewModel.FileListViewModel);
            Assert.False(viewModel.FileListViewModel.HasListingError, viewModel.FileListViewModel.ListingError);

            var window = new AppMainWindow
            {
                DataContext = viewModel,
                Width = 1280,
                Height = 860,
                Background = Brushes.White,
            };
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

            viewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("SpaceCharacter"));
            viewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("LettersCase"));
            viewModel.FilterChainViewModel.SetSelectedSteps([viewModel.FilterChainViewModel.Steps[1]]);
            viewModel.FilterEditorViewModel.FormatTokenPickerExpanded = false;
            if (viewModel.FilterEditorViewModel.OptionsEditor is { } optionsEditor)
            {
                optionsEditor.FormatTokenPickerExpanded = false;
            }

            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(200);
            Dispatcher.UIThread.RunJobs();
            return (window, viewModel);
        }

        private static void _CaptureFilterOptions(string outputDir)
        {
            var step = new FilterChainStepViewModel("Letters Case", new LettersCaseFilter());
            var dialogVm = new FilterOptionsDialogViewModel(step);
            var dialog = new FilterOptionsDialog(dialogVm);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                _CaptureControl(dialog, Path.Combine(outputDir, "filter-options.png"));
            }
            finally
            {
                dialog.Close();
            }
        }

        private static void _CaptureOptions(string outputDir)
        {
            var dialog = new OptionsDialog(new OptionsDialogViewModel());
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                _CaptureControl(dialog, Path.Combine(outputDir, "options.png"));
            }
            finally
            {
                dialog.Close();
            }
        }

        private static void _CapturePresetManager(string outputDir)
        {
            var (dialog, _, _) = PresetManagerDialogTestUi.ShowWithPresets(
                "Music tags cleanup",
                "Photo batch",
                "Strip drafts"
            );

            try
            {
                _CaptureControl(dialog, Path.Combine(outputDir, "preset-manager.png"));
            }
            finally
            {
                dialog.Close();
            }
        }

        private void _CaptureRenameLog(string outputDir)
        {
            var logDir = _tempDirectoryFixture.CreateTempDir();
            RenameLogStore.CaptureFromCommit(
                [
                    new RenameResultItem(
                        OriginalPath: TestPaths.Absolute("Blue Train.mp3"),
                        Status: RenameStatus.CommitOk,
                        Error: null,
                        Changes: [new RenamePropertyChange("FileName", "Blue Train", "Blue_Train")],
                        DestinationPath: TestPaths.Absolute("Blue_Train.mp3"),
                        IsFolder: false
                    ),
                    new RenameResultItem(
                        OriginalPath: TestPaths.Absolute("report_draft.txt"),
                        Status: RenameStatus.CommitOk,
                        Error: null,
                        Changes: [new RenamePropertyChange("FileName", "report_draft", "report")],
                        DestinationPath: TestPaths.Absolute("report.txt"),
                        IsFolder: false
                    ),
                ],
                directoryPath: logDir,
                limit: 10
            );
            RenameLogStore.CaptureFromCommit(
                [
                    new RenameResultItem(
                        OriginalPath: TestPaths.Absolute("IMG_0001.jpg"),
                        Status: RenameStatus.CommitOk,
                        Error: null,
                        Changes: [new RenamePropertyChange("FileName", "IMG_0001", "photo-0001")],
                        DestinationPath: TestPaths.Absolute("photo-0001.jpg"),
                        IsFolder: false
                    ),
                ],
                directoryPath: logDir,
                limit: 10
            );

            var dialog = new RenameLogDialog(new RenameLogDialogViewModel(logDir));
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                _CaptureControl(dialog, Path.Combine(outputDir, "rename-log.png"));
            }
            finally
            {
                dialog.Close();
            }
        }

        private static void _CaptureFormatEditor(string outputDir)
        {
            var editor = new FormatEditorControl
            {
                Text = "Track <counter:initial=1,step=1> - <file-name>",
                ShowRightClickHint = true,
            };
            var pane = new FormatTokenPickerPane { Content = editor, IsExpanded = true };
            var window = new Window
            {
                Title = "Format Editor",
                Width = 720,
                Height = 320,
                Background = Brushes.White,
                Content = new Border
                {
                    Padding = new Thickness(12),
                    Background = Brushes.White,
                    Child = pane,
                },
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                _CaptureControl(pane, Path.Combine(outputDir, "format-editor.png"));
            }
            finally
            {
                window.Close();
            }
        }

        private static void _CaptureFieldShuttle(string outputDir)
        {
            var dialogVm = new RenameListFieldShuttleDialogViewModel(
                RenameListVisibleColumn.CreateDefaults(),
                RenameListSortKey.DefaultKeys
            );
            var dialog = new RenameListFieldShuttleDialog(dialogVm) { Width = 900, Height = 700 };
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                _CaptureControl(dialog, Path.Combine(outputDir, "field-shuttle.png"));
            }
            finally
            {
                dialog.Close();
            }
        }

        private static void _CaptureAutoSort(string outputDir)
        {
            var dialogVm = new RenameListFieldShuttleDialogViewModel(
                RenameListVisibleColumn.CreateDefaults(),
                RenameListSortKey.DefaultKeys,
                RenameListFieldShuttleTab.Sort
            );
            var dialog = new RenameListFieldShuttleDialog(dialogVm) { Width = 900, Height = 700 };
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                _CaptureControl(dialog, Path.Combine(outputDir, "auto-sort.png"));
            }
            finally
            {
                dialog.Close();
            }
        }

        private static void _CaptureVisualTrim(string outputDir)
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("TrimLeft"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var optionsEditor = Assert.IsType<CountFilterEditorViewModel>(
                mainViewModel.FilterEditorViewModel.OptionsEditor
            );
            optionsEditor.TrimHelper.SetSampleText("Blue Train.mp3");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var helperView = editorView.GetVisualDescendants().OfType<VisualTrimHelperView>().Single();
            var textBox = helperView.FindControl<TextBox>("TrimHelperText");
            Assert.NotNull(textBox);
            textBox.SelectionStart = 0;
            textBox.SelectionEnd = 5;
            FilterEditorTestUi.RaisePointerReleased(textBox);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                // RenderTargetBitmap treats unset Background as transparent → black; force a light plate.
                helperView.Background = Brushes.White;
                _CaptureControl(helperView, Path.Combine(outputDir, "visual-trim.png"));
            }
            finally
            {
                window.Close();
            }
        }

        private async Task _CaptureStatusBarAsync(string outputDir)
        {
            var sampleDir = _CreateSampleFolder();
            var viewModel = new MainWindowViewModel(
                initialFileListPath: sampleDir,
                persistSession: false,
                shellOpener: NullFileShellOpener.Instance
            );
            FileListListingWait.WaitUntilIdle(viewModel.FileListViewModel);

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

                viewModel.FilterChainViewModel.AppendCommand.Execute(FilterChainTestUi.Entry("LettersCase"));
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();
                await Task.Delay(200);
                Dispatcher.UIThread.RunJobs();

                var itemsLabel = window
                    .GetVisualDescendants()
                    .OfType<TextBlock>()
                    .First(block =>
                        block.Text is not null && block.Text.StartsWith("Items:", StringComparison.Ordinal)
                    );
                var statusBar = itemsLabel.GetVisualAncestors().OfType<Border>().First();
                // Status bar Border has no Background; alone it renders transparent → black.
                statusBar.Background = Brushes.White;
                _CaptureControl(statusBar, Path.Combine(outputDir, "status-bar.png"), minWidth: 200, minHeight: 16);
            }
            finally
            {
                window.Close();
            }
        }

        private static void _CaptureControl(Control control, string path, int minWidth = 40, int minHeight = 40)
        {
            ArgumentNullException.ThrowIfNull(control);

            control.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var bounds = control.Bounds;
            var pixelWidth = Math.Max(1, (int)Math.Ceiling(bounds.Width));
            var pixelHeight = Math.Max(1, (int)Math.Ceiling(bounds.Height));
            Assert.True(
                pixelWidth > minWidth && pixelHeight > minHeight,
                $"{path} too small: {pixelWidth}x{pixelHeight}"
            );

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

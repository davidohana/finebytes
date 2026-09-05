using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Misc;
using Mfr.App.Ui.Views.FilterEditors.Misc;
using Mfr.Filters.Misc;
using Mfr.Tests.Ui.AppliedFilters;
using Mfr.Tests.Ui.RenameList;

namespace Mfr.Tests.Ui.FilterEditors.Misc
{
    /// <summary>
    /// Headless tests for <see cref="MoverFilterEditorView"/>.
    /// </summary>
    public sealed class MoverFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Mover option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Mover"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<MoverFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<MoverFilterEditorView>().Single();
            var rootFolder = editor.FindControl<TextBox>("RootFolderBox");
            var subFolder = editor.FindControl<TextBox>("SubFolderBox");
            var browse = editor.FindControl<HyperlinkButton>("BrowseRootButton");
            Assert.NotNull(rootFolder);
            Assert.NotNull(subFolder);
            Assert.NotNull(browse);
            Assert.Equal(@"C:\", rootFolder.Text);
            Assert.Equal("MFR", subFolder.Text);
            Assert.Same(
                ((MoverFilterEditorViewModel)mainViewModel.FilterEditorViewModel.OptionsEditor).BrowseRootFolderCommand,
                browse.Command
            );

            rootFolder.Text = @"E:\Archive";
            subFolder.Text = "<now:yyyy>";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (MoverFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(@"E:\Archive", filter.Options.RootFolder);
            Assert.Equal("<now:yyyy>", filter.Options.SubFolder);

            window.Close();
        }

        /// <summary>
        /// Verifies dropping a folder onto Mover Root folder sets the destination path.
        /// </summary>
        [AvaloniaFact]
        public async Task Root_folder_drop_folder_sets_path()
        {
            var dir = Path.Combine(Path.GetTempPath(), "mfr-mover-drop-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var dest = Path.Combine(dir, "Dest");
                Directory.CreateDirectory(dest);

                var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
                mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Mover"));
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                var editor = editorView.GetVisualDescendants().OfType<MoverFilterEditorView>().Single();
                var dropTarget = editor.FindControl<StackPanel>("RootFolderDropTarget");
                Assert.NotNull(dropTarget);

                var dataTransfer = await RenameListTestHelpers.CreateFileDataTransferAsync(window, [dest]);
                var dragOver = new DragEventArgs(
                    DragDrop.DragOverEvent,
                    dataTransfer,
                    dropTarget,
                    default,
                    KeyModifiers.None
                )
                {
                    DragEffects = DragDropEffects.None,
                };
                dropTarget.RaiseEvent(dragOver);
                Assert.Equal(DragDropEffects.Copy, dragOver.DragEffects);

                dropTarget.RaiseEvent(
                    new DragEventArgs(DragDrop.DropEvent, dataTransfer, dropTarget, default, KeyModifiers.None)
                );
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                var moverFilter = (MoverFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
                Assert.Equal(dest, moverFilter.Options.RootFolder);
                Assert.Equal(
                    dest,
                    ((MoverFilterEditorViewModel)mainViewModel.FilterEditorViewModel.OptionsEditor!).RootFolder
                );

                window.Close();
            }
            finally
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (IOException) { }
            }
        }

        /// <summary>
        /// Verifies dropping a file onto Mover Root folder uses the file's parent directory.
        /// </summary>
        [AvaloniaFact]
        public async Task Root_folder_drop_file_uses_parent()
        {
            var dir = Path.Combine(Path.GetTempPath(), "mfr-mover-drop-file-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var filePath = Path.Combine(dir, "track.mp3");
                File.WriteAllText(filePath, "x");

                var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
                mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Mover"));
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                var editor = editorView.GetVisualDescendants().OfType<MoverFilterEditorView>().Single();
                var dropTarget = editor.FindControl<StackPanel>("RootFolderDropTarget");
                Assert.NotNull(dropTarget);

                var dataTransfer = await RenameListTestHelpers.CreateFileDataTransferAsync(window, [filePath]);
                dropTarget.RaiseEvent(
                    new DragEventArgs(DragDrop.DropEvent, dataTransfer, dropTarget, default, KeyModifiers.None)
                );
                window.UpdateLayout();
                Dispatcher.UIThread.RunJobs();

                var moverFilter = (MoverFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
                Assert.Equal(dir, moverFilter.Options.RootFolder);

                window.Close();
            }
            finally
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (IOException) { }
            }
        }

        /// <summary>
        /// Verifies Mover Root folder DragOver rejects non-file payloads.
        /// </summary>
        [AvaloniaFact]
        public void Root_folder_drag_over_rejects_non_file()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("Mover"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var editor = editorView.GetVisualDescendants().OfType<MoverFilterEditorView>().Single();
            var dropTarget = editor.FindControl<StackPanel>("RootFolderDropTarget");
            Assert.NotNull(dropTarget);

            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.CreateText("not-a-file"));
            var dragOver = new DragEventArgs(
                DragDrop.DragOverEvent,
                dataTransfer,
                dropTarget,
                default,
                KeyModifiers.None
            )
            {
                DragEffects = DragDropEffects.Copy,
            };
            dropTarget.RaiseEvent(dragOver);

            Assert.Equal(DragDropEffects.None, dragOver.DragEffects);
            Assert.Equal(
                @"C:\",
                ((MoverFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter).Options.RootFolder
            );

            window.Close();
        }
    }
}

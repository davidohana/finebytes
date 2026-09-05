using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Audio;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Audio;
using Mfr.Filters.Audio;
using Mfr.Models.Tags;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Audio
{
    /// <summary>
    /// Headless tests for <see cref="TagRemoverFilterEditorView"/>.
    /// </summary>
    public sealed class TagRemoverFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Audio Tag Remover option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Tag_remover_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("TagRemover"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<TagRemoverFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);
            var editorVm = (TagRemoverFilterEditorViewModel)mainViewModel.FilterEditorViewModel.OptionsEditor;

            var editor = editorView.GetVisualDescendants().OfType<TagRemoverFilterEditorView>().Single();
            var removeAll = editor.FindControl<CompactCheckBox>("RemoveAllCheckBox");
            var blockKinds = editor.FindControl<ItemsControl>("BlockKindsItemsControl");
            Assert.NotNull(removeAll);
            Assert.NotNull(blockKinds);
            Assert.True(removeAll.IsChecked);
            Assert.False(editorVm.AreBlockTypesEnabled);
            Assert.False(blockKinds.IsEnabled);

            removeAll.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(editorVm.AreBlockTypesEnabled);
            Assert.True(blockKinds.IsEnabled);

            var id3v1 = _FindBlockCheckBox(editor, AudioTagBlockKind.Id3v1);
            var id3v2 = _FindBlockCheckBox(editor, AudioTagBlockKind.Id3v2);
            Assert.False(id3v1.IsChecked);
            Assert.False(id3v2.IsChecked);

            var filter = (TagRemoverFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.False(filter.Options.All);
            Assert.Empty(filter.Options.Blocks ?? []);

            id3v1.IsChecked = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            filter = (TagRemoverFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.False(filter.Options.All);
            Assert.Equal([AudioTagBlockKind.Id3v1], filter.Options.Blocks);

            id3v1.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            filter = (TagRemoverFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.False(filter.Options.All);
            Assert.Empty(filter.Options.Blocks ?? []);
            Assert.False(removeAll.IsChecked);
            Assert.True(editorVm.AreBlockTypesEnabled);

            window.Close();
        }

        private static CompactCheckBox _FindBlockCheckBox(TagRemoverFilterEditorView editor, AudioTagBlockKind kind)
        {
            return editor
                .GetVisualDescendants()
                .OfType<CompactCheckBox>()
                .Single(box => box.Tag is AudioTagBlockKind tagged && tagged == kind);
        }
    }
}

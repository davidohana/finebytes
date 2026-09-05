using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Attributes;
using Mfr.App.Ui.Views.FilterEditors.Attributes;
using Mfr.Filters.Attributes;
using Mfr.Tests.Ui.AppliedFilters;

namespace Mfr.Tests.Ui.FilterEditors.Attributes
{
    /// <summary>
    /// Headless tests for <see cref="AttributesSetterFilterEditorView"/>.
    /// </summary>
    public sealed class AttributesSetterFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Attributes Setter On/Off/Keep radio edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Attributes_setter_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("AttributesSetter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<AttributesSetterFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);

            var editor = editorView.GetVisualDescendants().OfType<AttributesSetterFilterEditorView>().Single();
            var readOnlyKeep = editor.FindControl<RadioButton>("ReadOnlyKeepRadio");
            var hiddenKeep = editor.FindControl<RadioButton>("HiddenKeepRadio");
            var archiveKeep = editor.FindControl<RadioButton>("ArchiveKeepRadio");
            var systemKeep = editor.FindControl<RadioButton>("SystemKeepRadio");
            Assert.NotNull(readOnlyKeep);
            Assert.NotNull(hiddenKeep);
            Assert.NotNull(archiveKeep);
            Assert.NotNull(systemKeep);
            Assert.True(readOnlyKeep.IsChecked);
            Assert.True(hiddenKeep.IsChecked);
            Assert.True(archiveKeep.IsChecked);
            Assert.True(systemKeep.IsChecked);

            var hiddenOn = editor.FindControl<RadioButton>("HiddenOnRadio");
            var archiveOff = editor.FindControl<RadioButton>("ArchiveOffRadio");
            var readOnlyOn = editor.FindControl<RadioButton>("ReadOnlyOnRadio");
            Assert.NotNull(hiddenOn);
            Assert.NotNull(archiveOff);
            Assert.NotNull(readOnlyOn);

            hiddenOn.IsChecked = true;
            archiveOff.IsChecked = true;
            readOnlyOn.IsChecked = true;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (AttributesSetterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(AttributeTriState.Set, filter.Options.ReadOnly);
            Assert.Equal(AttributeTriState.Set, filter.Options.Hidden);
            Assert.Equal(AttributeTriState.Clear, filter.Options.Archive);
            Assert.Equal(AttributeTriState.Keep, filter.Options.System);

            window.Close();
        }
    }
}

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FilterEditors.Attributes;
using Mfr.App.Ui.Views.Controls;
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
            var readOnlyRow = editor.FindControl<AttributeTriStateRow>("ReadOnlyRow");
            var hiddenRow = editor.FindControl<AttributeTriStateRow>("HiddenRow");
            var archiveRow = editor.FindControl<AttributeTriStateRow>("ArchiveRow");
            var systemRow = editor.FindControl<AttributeTriStateRow>("SystemRow");
            Assert.NotNull(readOnlyRow);
            Assert.NotNull(hiddenRow);
            Assert.NotNull(archiveRow);
            Assert.NotNull(systemRow);

            var readOnlyKeep = readOnlyRow.FindControl<RadioButton>("KeepRadio");
            var hiddenKeep = hiddenRow.FindControl<RadioButton>("KeepRadio");
            var archiveKeep = archiveRow.FindControl<RadioButton>("KeepRadio");
            var systemKeep = systemRow.FindControl<RadioButton>("KeepRadio");
            Assert.NotNull(readOnlyKeep);
            Assert.NotNull(hiddenKeep);
            Assert.NotNull(archiveKeep);
            Assert.NotNull(systemKeep);
            Assert.True(readOnlyKeep.IsChecked);
            Assert.True(hiddenKeep.IsChecked);
            Assert.True(archiveKeep.IsChecked);
            Assert.True(systemKeep.IsChecked);

            var hiddenOn = hiddenRow.FindControl<RadioButton>("OnRadio");
            var archiveOff = archiveRow.FindControl<RadioButton>("OffRadio");
            var readOnlyOn = readOnlyRow.FindControl<RadioButton>("OnRadio");
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

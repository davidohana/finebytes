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
    /// Headless tests for <see cref="DateTimeSetterFilterEditorView"/>.
    /// </summary>
    public sealed class DateTimeSetterFilterEditorViewTests
    {
        /// <summary>
        /// Verifies Date/Time Setter option edits persist on the applied step.
        /// </summary>
        [AvaloniaFact]
        public void Date_time_setter_controls_update_chain_options()
        {
            var (window, mainViewModel, editorView) = FilterEditorTestUi.ShowFilterEditorPanes();
            mainViewModel.AppliedFiltersViewModel.AppendCommand.Execute(AppliedFiltersTestUi.Entry("DateTimeSetter"));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.IsType<DateTimeSetterFilterEditorViewModel>(mainViewModel.FilterEditorViewModel.OptionsEditor);
            var editorVm = (DateTimeSetterFilterEditorViewModel)mainViewModel.FilterEditorViewModel.OptionsEditor!;

            var editor = editorView.GetVisualDescendants().OfType<DateTimeSetterFilterEditorView>().Single();
            var fieldCombo = editor.FindControl<ComboBox>("TimestampFieldCombo");
            var setDateCheck = editor.FindControl<CheckBox>("SetDateCheckBox");
            var setTimeCheck = editor.FindControl<CheckBox>("SetTimeCheckBox");
            var dateBox = editor.FindControl<TextBox>("DateBox");
            var timeBox = editor.FindControl<TextBox>("TimeBox");
            var currentButton = editor.FindControl<Button>("CurrentButton");
            Assert.NotNull(fieldCombo);
            Assert.NotNull(setDateCheck);
            Assert.NotNull(setTimeCheck);
            Assert.NotNull(dateBox);
            Assert.NotNull(timeBox);
            Assert.NotNull(currentButton);
            Assert.True(setDateCheck.IsChecked);
            Assert.True(setTimeCheck.IsChecked);
            Assert.True(dateBox.IsEnabled);
            Assert.True(timeBox.IsEnabled);
            Assert.Equal(
                DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                dateBox.Text
            );
            Assert.False(string.IsNullOrWhiteSpace(timeBox.Text));

            fieldCombo.SelectedItem = editorVm.TimestampFields.Single(c => c.Field == TimestampField.Creation);
            dateBox.Text = "2020-12-25";
            timeBox.Text = "09:00:15";
            setTimeCheck.IsChecked = false;
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            var filter = (DateTimeSetterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(TimestampField.Creation, filter.Options.TimestampField);
            Assert.True(filter.Options.SetDate);
            Assert.Equal(new DateOnly(2020, 12, 25), filter.Options.Date);
            Assert.False(filter.Options.SetTime);
            Assert.Equal(new TimeOnly(9, 0, 15), filter.Options.Time);
            Assert.False(timeBox.IsEnabled);

            setTimeCheck.IsChecked = true;
            currentButton.Command!.Execute(null);
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            filter = (DateTimeSetterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(DateOnly.FromDateTime(DateTime.Today), filter.Options.Date);
            Assert.True(filter.Options.SetTime);

            timeBox.Text = "25:19:01";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            filter = (DateTimeSetterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(
                filter.Options.Time.ToString("HH':'mm':'ss", System.Globalization.CultureInfo.InvariantCulture),
                timeBox.Text
            );
            Assert.NotEqual("25:19:01", timeBox.Text);

            dateBox.Text = "2024-02-30";
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            filter = (DateTimeSetterFilter)mainViewModel.AppliedFiltersViewModel.ToChain().Steps[0].Filter;
            Assert.Equal(
                filter.Options.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                dateBox.Text
            );
            Assert.NotEqual("2024-02-30", dateBox.Text);

            window.Close();
        }
    }
}

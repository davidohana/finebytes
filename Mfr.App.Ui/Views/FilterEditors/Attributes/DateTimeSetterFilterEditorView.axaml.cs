using Avalonia.Controls;
using Avalonia.Input;
using Mfr.App.Ui.ViewModels.FilterEditors.Attributes;
using Mfr.Filters.Attributes;

namespace Mfr.App.Ui.Views.FilterEditors.Attributes
{
    /// <summary>
    /// Option editor for <see cref="DateTimeSetterFilter"/>.
    /// </summary>
    public partial class DateTimeSetterFilterEditorView : UserControl
    {
        /// <summary>
        /// Initializes the date/time setter option editor.
        /// </summary>
        public DateTimeSetterFilterEditorView()
        {
            InitializeComponent();
        }

        private void _OnDateBoxLostFocus(object? sender, FocusChangedEventArgs e)
        {
            if (DataContext is DateTimeSetterFilterEditorViewModel editor)
            {
                editor.CommitDateText();
            }
        }

        private void _OnTimeBoxLostFocus(object? sender, FocusChangedEventArgs e)
        {
            if (DataContext is DateTimeSetterFilterEditorViewModel editor)
            {
                editor.CommitTimeText();
            }
        }
    }
}

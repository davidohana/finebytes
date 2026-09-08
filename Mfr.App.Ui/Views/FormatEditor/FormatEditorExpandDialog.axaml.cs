using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Larger surface for editing a format string; keeps <see cref="FormatEditor.Text"/> in sync with the host.
    /// </summary>
    public partial class FormatEditorExpandDialog : Window
    {
        /// <summary>
        /// Fixed editor height inside the expand dialog (~5–8 comfortable lines).
        /// </summary>
        public const double DialogEditorHeight = 280;

        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public FormatEditorExpandDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the dialog bound to a host <see cref="FormatEditor"/>.
        /// </summary>
        /// <param name="host">In-place FormatEditor whose text is edited.</param>
        public FormatEditorExpandDialog(FormatEditor host)
            : this()
        {
            ArgumentNullException.ThrowIfNull(host);

            Host = host;
            Editor.AcceptsReturn = host.AcceptsReturn;
            Editor.ValidationMode = host.ValidationMode;
            Editor.MaxLength = host.MaxLength;
            Editor.Watermark = host.Watermark;
            Editor.ShowExpandButton = false;
            Editor.ShowRightClickHint = false;
            Editor.UseFixedEditorHeight(DialogEditorHeight);

            // Live two-way sync while the dialog is open; closing keeps the last value.
            Editor.Bind(
                FormatEditor.TextProperty,
                new Binding
                {
                    Source = host,
                    Path = nameof(FormatEditor.Text),
                    Mode = BindingMode.TwoWay,
                }
            );
        }

        /// <summary>
        /// Gets the in-place FormatEditor this dialog edits, when constructed with a host.
        /// </summary>
        public FormatEditor? Host { get; }

        /// <summary>
        /// Gets the dialog's FormatEditor (Insert/Edit available).
        /// </summary>
        public FormatEditor DialogEditor => Editor;

        private void _OnOkClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

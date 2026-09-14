using Avalonia.Controls;
using Avalonia.Interactivity;
using Mfr.Models.Config;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Shared OK/Cancel confirmation dialog (title + body).
    /// <para>
    /// When a <see cref="ConfirmationKind"/> is supplied, shows a “Keep showing…” checkbox (default checked).
    /// Unchecking it and confirming OK suppresses that kind and persists prefs.
    /// </para>
    /// <para>Closes with <see langword="true"/> for OK and <see langword="false"/> for Cancel or Escape.</para>
    /// </summary>
    public partial class ConfirmMessageDialog : Window
    {
        private readonly ConfirmationKind? _kind;

        /// <summary>
        /// Initializes an empty dialog (designer / XAML loader).
        /// </summary>
        public ConfirmMessageDialog()
        {
            InitializeComponent();
            ModalDialogKeyboard.Attach(this);
            // After footer's Close(true): prefs update still runs on the same click before ShowDialog completes.
            Footer.AcceptButton.Click += _OnAcceptClick;
        }

        /// <summary>
        /// Initializes the dialog with a title and message body (no keep-showing checkbox).
        /// </summary>
        /// <param name="title">Window title.</param>
        /// <param name="message">Message body.</param>
        public ConfirmMessageDialog(string title, string message)
            : this(title, message, kind: null) { }

        /// <summary>
        /// Initializes the dialog with a title, message body, and optional suppressible confirmation kind.
        /// </summary>
        /// <param name="title">Window title.</param>
        /// <param name="message">Message body.</param>
        /// <param name="kind">
        /// When set, shows the keep-showing checkbox for this kind; when <see langword="null"/>, no checkbox
        /// (e.g. Reset Configuration).
        /// </param>
        public ConfirmMessageDialog(string title, string message, ConfirmationKind? kind)
            : this()
        {
            Title = title;
            MessageText.Text = message;
            _kind = kind;
            if (kind is null)
            {
                return;
            }

            KeepShowingCheckBox.IsVisible = true;
        }

        /// <summary>
        /// When set, replaces <see cref="ConfigStore.Save"/> after suppress on OK (via <see cref="ConfigStoreSave"/>; headless tests).
        /// </summary>
        internal Action? SaveConfig { get; set; }

        /// <summary>
        /// On OK: when a kind is set and keep-showing is unchecked, suppresses and persists.
        /// </summary>
        private void _OnAcceptClick(object? sender, RoutedEventArgs e)
        {
            if (_kind is not { } kind)
            {
                return;
            }

            if (KeepShowingCheckBox.IsChecked != false)
            {
                return;
            }

            ConfirmationPolicy.Suppress(kind);
            ConfigStoreSave.Invoke(SaveConfig);
        }
    }
}

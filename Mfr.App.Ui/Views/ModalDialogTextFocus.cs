using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Focuses a modal dialog text field and selects its contents on open.
    /// </summary>
    internal static class ModalDialogTextFocus
    {
        /// <summary>
        /// Focuses the editable <see cref="TextBox"/> inside <paramref name="host"/> when present;
        /// otherwise focuses <paramref name="host"/>.
        /// </summary>
        /// <param name="host">
        /// A <see cref="TextBox"/>, or a host such as an editable <see cref="ComboBox"/> that contains one.
        /// </param>
        public static void FocusAndSelectAll(Control host)
        {
            ArgumentNullException.ThrowIfNull(host);
            if (host is TextBox textBox)
            {
                textBox.Focus();
                textBox.SelectAll();
                return;
            }

            var editable = host.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
            if (editable is not null)
            {
                editable.Focus();
                editable.SelectAll();
                return;
            }

            host.Focus();
        }
    }
}

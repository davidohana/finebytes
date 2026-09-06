using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Mfr.App.Ui.Views;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless smoke tests for the shared OK-only message dialog.
    /// </summary>
    public sealed class OkMessageDialogTests
    {
        [AvaloniaFact]
        /// <summary>
        /// Verifies the dialog constructs with title and message body.
        /// </summary>
        public void OkMessageDialog_Constructs_With_Title_And_Message()
        {
            var dialog = new OkMessageDialog("Warning", "Something happened.");
            dialog.Show();

            Assert.True(dialog.IsVisible);
            Assert.Equal("Warning", dialog.Title);
            var message = dialog.FindControl<TextBlock>("MessageText");
            Assert.NotNull(message);
            Assert.Equal("Something happened.", message.Text);
        }
    }
}

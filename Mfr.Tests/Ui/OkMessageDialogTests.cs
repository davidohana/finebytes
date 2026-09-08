using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Mfr.App.Ui.Views;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless smoke tests for the shared OK-only message dialog.
    /// </summary>
    public sealed class OkMessageDialogTests
    {
        /// <summary>
        /// Verifies the dialog constructs with title, message, and app-chrome OK footer.
        /// </summary>
        [AvaloniaFact]
        public void OkMessageDialog_Constructs_With_Title_And_Message()
        {
            var dialog = new OkMessageDialog("Warning", "Something happened.");
            dialog.Show();
            dialog.UpdateLayout();

            Assert.True(dialog.IsVisible);
            Assert.Equal("Warning", dialog.Title);
            var message = dialog.FindControl<TextBlock>("MessageText");
            Assert.NotNull(message);
            Assert.Equal("Something happened.", message.Text);

            var footer = dialog.FindControl<StackPanel>("Footer");
            Assert.NotNull(footer);
            Assert.Equal(HorizontalAlignment.Center, footer.HorizontalAlignment);

            var ok = dialog.FindControl<Button>("OkButton");
            Assert.NotNull(ok);
            Assert.Contains("ok-message-footer", ok.Classes);
            var brush = Assert.IsAssignableFrom<ISolidColorBrush>(ok.Background);
            Assert.Equal(0, brush.Color.A);

            dialog.Close();
        }

        /// <summary>
        /// Verifies Escape closes the dialog via the OK button's <c>IsCancel</c> wiring.
        /// </summary>
        [AvaloniaFact]
        public void Escape_Closes_Dialog()
        {
            var dialog = new OkMessageDialog("Warning", "Something happened.");
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(dialog.Focusable);
            Assert.True(dialog.IsVisible);

            dialog.RaiseEvent(
                new KeyEventArgs
                {
                    RoutedEvent = InputElement.KeyDownEvent,
                    Key = Key.Escape,
                    Source = dialog,
                }
            );
            Dispatcher.UIThread.RunJobs();

            Assert.False(dialog.IsVisible);
        }
    }
}

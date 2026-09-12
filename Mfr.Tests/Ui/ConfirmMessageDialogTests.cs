using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Mfr.App.Ui.Views;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless smoke tests for the shared OK/Cancel confirmation dialog.
    /// </summary>
    public sealed class ConfirmMessageDialogTests
    {
        /// <summary>
        /// Verifies the dialog constructs with title, message, and OK/Cancel footer.
        /// </summary>
        [AvaloniaFact]
        public void ConfirmMessageDialog_Constructs_With_Title_And_Message()
        {
            var dialog = new ConfirmMessageDialog("Confirmation", "Will restart.");
            dialog.Show();
            dialog.UpdateLayout();

            Assert.True(dialog.IsVisible);
            Assert.Equal("Confirmation", dialog.Title);
            var message = dialog.FindControl<TextBlock>("MessageText");
            Assert.NotNull(message);
            Assert.Equal("Will restart.", message.Text);

            var footer = dialog.FindControl<StackPanel>("Footer");
            Assert.NotNull(footer);
            Assert.Equal(HorizontalAlignment.Center, footer.HorizontalAlignment);

            var ok = dialog.FindControl<Button>("OkButton");
            Assert.NotNull(ok);
            Assert.Contains("message-dialog-footer", ok.Classes);
            Assert.True(ok.IsDefault);
            var app = Assert.IsAssignableFrom<Application>(Application.Current);
            Assert.True(
                app.TryGetResource("AppChromeSelectionBrush", app.ActualThemeVariant, out var selection)
            );
            Assert.Equal(selection, ok.Background);
            Assert.True(
                app.TryGetResource(
                    "AppChromeSelectionBorderBrush",
                    app.ActualThemeVariant,
                    out var selectionBorder
                )
            );
            Assert.Equal(selectionBorder, ok.BorderBrush);

            var cancel = dialog.FindControl<Button>("CancelButton");
            Assert.NotNull(cancel);
            Assert.Contains("message-dialog-footer", cancel.Classes);
            Assert.True(cancel.IsCancel);
            var cancelBrush = Assert.IsAssignableFrom<ISolidColorBrush>(cancel.Background);
            Assert.Equal(0, cancelBrush.Color.A);

            dialog.Close(false);
        }

        /// <summary>
        /// Verifies Escape closes the dialog via the Cancel button's <c>IsCancel</c> wiring.
        /// </summary>
        [AvaloniaFact]
        public void Escape_Closes_Dialog()
        {
            var dialog = new ConfirmMessageDialog("Confirmation", "Will restart.");
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

        /// <summary>
        /// Verifies OK closes with <see langword="true"/> when shown as a dialog.
        /// </summary>
        [AvaloniaFact]
        public async Task Ok_Returns_True()
        {
            var owner = new Window();
            owner.Show();
            Dispatcher.UIThread.RunJobs();

            var dialog = new ConfirmMessageDialog("Confirmation", "Will restart.");
            var resultTask = dialog.ShowDialog<bool>(owner);
            Dispatcher.UIThread.RunJobs();

            var ok = dialog.FindControl<Button>("OkButton");
            Assert.NotNull(ok);
            ok.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.True(await resultTask);
            owner.Close();
        }

        /// <summary>
        /// Verifies Cancel closes with <see langword="false"/> when shown as a dialog.
        /// </summary>
        [AvaloniaFact]
        public async Task Cancel_Returns_False()
        {
            var owner = new Window();
            owner.Show();
            Dispatcher.UIThread.RunJobs();

            var dialog = new ConfirmMessageDialog("Confirmation", "Will restart.");
            var resultTask = dialog.ShowDialog<bool>(owner);
            Dispatcher.UIThread.RunJobs();

            var cancel = dialog.FindControl<Button>("CancelButton");
            Assert.NotNull(cancel);
            cancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            Assert.False(await resultTask);
            owner.Close();
        }
    }
}

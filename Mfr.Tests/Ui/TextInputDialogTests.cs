using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels;
using Mfr.App.Ui.Views;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Headless smoke tests for the shared text-input dialog.
    /// </summary>
    public sealed class TextInputDialogTests
    {
        /// <summary>
        /// Verifies styled prompt/note runs render on open, including bold and override-blue.
        /// </summary>
        [AvaloniaFact]
        public void TextInputDialog_Applies_Styled_Prompt_And_Note_On_Open()
        {
            var prompt = new TextInputPrompt
            {
                Title = "Manual Set Value",
                DefaultValue = "alpha.txt",
                Prompt = StatusHintDisplay.FromRuns(
                    new StatusHintRun("Set the "),
                    new StatusHintRun("initial value (before filters)") { FontWeight = FontWeight.Bold },
                    new StatusHintRun(" to:")
                ),
                Note = StatusHintDisplay.FromRuns(
                    new StatusHintRun("Takes effect now ("),
                    new StatusHintRun("blue")
                    {
                        FontWeight = FontWeight.Bold,
                        ForegroundResourceKey = "RenameListManualOverrideForegroundBrush",
                    },
                    new StatusHintRun(").")
                ),
            };

            var dialog = new TextInputDialog(prompt);
            dialog.Show();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Manual Set Value", dialog.Title);

            var promptText = dialog.FindControl<TextBlock>("PromptText");
            Assert.NotNull(promptText);
            Assert.NotNull(promptText.Inlines);
            Assert.Equal(3, promptText.Inlines.Count);
            var boldSide = Assert.IsType<Run>(promptText.Inlines[1]);
            Assert.Equal("initial value (before filters)", boldSide.Text);
            Assert.Equal(FontWeight.Bold, boldSide.FontWeight);

            var noteText = dialog.FindControl<TextBlock>("NoteText");
            Assert.NotNull(noteText);
            Assert.True(noteText.IsVisible);
            Assert.NotNull(noteText.Inlines);
            Assert.Equal(3, noteText.Inlines.Count);
            var blueRun = Assert.IsType<Run>(noteText.Inlines[1]);
            Assert.Equal("blue", blueRun.Text);
            Assert.Equal(FontWeight.Bold, blueRun.FontWeight);
            var app = Assert.IsAssignableFrom<Application>(Application.Current);
            Assert.True(
                app.TryGetResource(
                    "RenameListManualOverrideForegroundBrush",
                    dialog.ActualThemeVariant,
                    out var overrideResource
                )
            );
            var expectedBrush = Assert.IsAssignableFrom<ISolidColorBrush>(overrideResource);
            var actualBrush = Assert.IsAssignableFrom<ISolidColorBrush>(blueRun.Foreground);
            Assert.Equal(expectedBrush.Color, actualBrush.Color);

            var valueBox = dialog.FindControl<TextBox>("ValueBox");
            Assert.NotNull(valueBox);
            Assert.Equal("alpha.txt", valueBox.Text);

            dialog.Close(null);
        }

        /// <summary>
        /// Verifies an empty note stays hidden so the dialog does not reserve note space.
        /// </summary>
        [AvaloniaFact]
        public void TextInputDialog_Hides_Empty_Note()
        {
            var prompt = new TextInputPrompt
            {
                Title = "Prompt",
                DefaultValue = string.Empty,
                Prompt = StatusHintDisplay.FromPlain("Enter a value:"),
            };

            var dialog = new TextInputDialog(prompt);
            dialog.Show();
            dialog.UpdateLayout();

            var noteText = dialog.FindControl<TextBlock>("NoteText");
            Assert.NotNull(noteText);
            Assert.False(noteText.IsVisible);

            dialog.Close(null);
        }
    }
}

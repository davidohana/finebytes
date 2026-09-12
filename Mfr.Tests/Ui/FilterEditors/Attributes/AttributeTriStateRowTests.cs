using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mfr.App.Ui.Views.Controls;
using Mfr.App.Ui.Views.FilterEditors.Attributes;

namespace Mfr.Tests.Ui.FilterEditors.Attributes
{
    /// <summary>
    /// Headless layout tests for <see cref="AttributeTriStateRow"/>.
    /// </summary>
    public sealed class AttributeTriStateRowTests
    {
        /// <summary>
        /// Verifies nested labeled rows still share label width under a parent shared-size scope.
        /// </summary>
        [AvaloniaFact]
        public void Labels_align_width_within_shared_size_scope()
        {
            var shortRow = new AttributeTriStateRow { Label = "A:", GroupName = "Short" };
            var longRow = new AttributeTriStateRow { Label = "Much longer label:", GroupName = "Long" };
            var panel = new StackPanel { Spacing = 6 };
            Grid.SetIsSharedSizeScope(panel, true);
            panel.Children.Add(shortRow);
            panel.Children.Add(longRow);

            var window = new Window
            {
                Width = 420,
                Height = 120,
                Content = panel,
            };
            window.Show();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            try
            {
                var shortLabel = _LabelText(shortRow);
                var longLabel = _LabelText(longRow);
                Assert.Equal("A:", shortLabel.Text);
                Assert.Equal("Much longer label:", longLabel.Text);
                Assert.True(
                    shortLabel.Bounds.Width > 1 && longLabel.Bounds.Width > 1,
                    "Label text blocks should be measured."
                );
                Assert.Equal(longLabel.Bounds.Width, shortLabel.Bounds.Width, precision: 0);
            }
            finally
            {
                window.Close();
            }
        }

        private static TextBlock _LabelText(AttributeTriStateRow row)
        {
            var labeledRow = row.GetVisualDescendants().OfType<FilterEditorLabeledRow>().Single();
            var label = labeledRow
                .GetVisualDescendants()
                .OfType<TextBlock>()
                .FirstOrDefault(block => block.Classes.Contains("filter-editor-label"));
            Assert.NotNull(label);
            return label;
        }
    }
}

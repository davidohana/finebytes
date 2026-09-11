using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Mfr.App.Ui.ViewModels;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Applies <see cref="StatusHintDisplay"/> runs onto a <see cref="TextBlock"/>.
    /// </summary>
    internal static class StatusHintInlines
    {
        /// <summary>
        /// Clears <paramref name="target"/> and adds styled inlines for <paramref name="display"/>.
        /// </summary>
        /// <param name="resourceHost">
        /// Control used for theme resource lookup (falls back to <see cref="Application.Current"/>).
        /// </param>
        /// <param name="target">Text block that receives the inlines.</param>
        /// <param name="display">Styled runs to render.</param>
        public static void Apply(StyledElement resourceHost, TextBlock target, StatusHintDisplay display)
        {
            ArgumentNullException.ThrowIfNull(resourceHost);
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(display);

            target.Inlines?.Clear();
            if (display.IsEmpty)
            {
                return;
            }

            foreach (var run in display.Runs)
            {
                var inline = new Run { Text = run.Text };
                if (run.FontWeight.HasValue)
                {
                    inline.FontWeight = run.FontWeight.Value;
                }

                if (
                    !string.IsNullOrEmpty(run.ForegroundResourceKey)
                    && _TryResolveBrush(resourceHost, run.ForegroundResourceKey) is { } brush
                )
                {
                    inline.Foreground = brush;
                }

                target.Inlines!.Add(inline);
            }
        }

        /// <summary>
        /// Resolves a brush resource from the host, then the application.
        /// </summary>
        private static IBrush? _TryResolveBrush(StyledElement resourceHost, string resourceKey)
        {
            if (
                resourceHost.TryGetResource(resourceKey, resourceHost.ActualThemeVariant, out var resource)
                && resource is IBrush hostBrush
            )
            {
                return hostBrush;
            }

            if (
                Application.Current?.TryGetResource(resourceKey, resourceHost.ActualThemeVariant, out resource) == true
                && resource is IBrush appBrush
            )
            {
                return appBrush;
            }

            return null;
        }
    }
}

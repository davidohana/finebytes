using Avalonia.Controls;
using Avalonia.Media;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// Readable tooltip body: wraps at a fixed max width, with optional title and bullets.
    /// <para>
    /// Use as <c>ToolTip.Tip</c> content. In AXAML, nest <see cref="TextBlock"/> children with
    /// <c>rich-tip-title</c>, <c>rich-tip-body</c>, or <c>rich-tip-bullet</c> classes; or build
    /// via <see cref="Wrap"/> / <see cref="Create"/>.
    /// </para>
    /// </summary>
    public sealed class RichToolTip : StackPanel
    {
        /// <summary>
        /// Default max width so long help text wraps instead of stretching across the screen.
        /// </summary>
        public const double DefaultMaxWidth = 320;

        /// <summary>
        /// Style class for a bold lead-in line.
        /// </summary>
        public const string TitleClass = "rich-tip-title";

        /// <summary>
        /// Style class for a wrapping paragraph.
        /// </summary>
        public const string BodyClass = "rich-tip-body";

        /// <summary>
        /// Style class for a bullet line (text should include the bullet glyph).
        /// </summary>
        public const string BulletClass = "rich-tip-bullet";

        /// <summary>
        /// Initializes a rich tooltip panel with default spacing and max width.
        /// </summary>
        public RichToolTip()
        {
            Spacing = 4;
            MaxWidth = DefaultMaxWidth;
        }

        /// <summary>
        /// Builds a wrapping tip from plain text. Newlines become separate paragraphs.
        /// </summary>
        /// <param name="body">Plain help text; use <c>\n</c> to split paragraphs.</param>
        /// <returns>Tooltip content ready for <see cref="ToolTip.SetTip"/>.</returns>
        public static RichToolTip Wrap(string body)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(body);

            var tip = new RichToolTip();
            var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var line in lines)
            {
                tip.Children.Add(_Body(line));
            }

            return tip;
        }

        /// <summary>
        /// Builds a tip with an optional bold title, optional body, and bullet lines.
        /// </summary>
        /// <param name="title">Bold lead-in; omit or null when unused.</param>
        /// <param name="body">Paragraph under the title; omit or null when unused.</param>
        /// <param name="bullets">Short bullet lines without a leading glyph.</param>
        /// <returns>Tooltip content ready for <see cref="ToolTip.SetTip"/>.</returns>
        public static RichToolTip Create(string? title = null, string? body = null, params string[] bullets)
        {
            var tip = new RichToolTip();

            if (!string.IsNullOrWhiteSpace(title))
            {
                tip.Children.Add(_Title(title));
            }

            if (!string.IsNullOrWhiteSpace(body))
            {
                tip.Children.Add(_Body(body));
            }

            foreach (var bullet in bullets)
            {
                if (string.IsNullOrWhiteSpace(bullet))
                {
                    continue;
                }

                tip.Children.Add(_Bullet(bullet));
            }

            if (tip.Children.Count == 0)
            {
                throw new ArgumentException("Rich tooltip needs a title, body, or at least one bullet.");
            }

            return tip;
        }

        private static TextBlock _Title(string text)
        {
            return new TextBlock
            {
                Classes = { TitleClass },
                Text = text,
                TextWrapping = TextWrapping.Wrap,
            };
        }

        private static TextBlock _Body(string text)
        {
            return new TextBlock
            {
                Classes = { BodyClass },
                Text = text,
                TextWrapping = TextWrapping.Wrap,
            };
        }

        private static TextBlock _Bullet(string text)
        {
            return new TextBlock
            {
                Classes = { BulletClass },
                Text = "• " + text,
                TextWrapping = TextWrapping.Wrap,
            };
        }
    }
}

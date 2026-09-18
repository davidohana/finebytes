using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels.FilterChainPane;
using Mfr.App.Ui.ViewModels.FilterEditors;
using Mfr.App.Ui.ViewModels.FilterEditors.Case;
using Mfr.App.Ui.ViewModels.FilterEditors.Formatting;
using Mfr.App.Ui.ViewModels.FilterEditors.Replace;
using Mfr.App.Ui.Views.FilterEditors;
using Mfr.Filters;

namespace Mfr.Tests.Ui.FilterEditors
{
    /// <summary>
    /// One-shot capture of Filter Configuration options bodies for help PNGs.
    /// <para>
    /// Run with <c>MFR_CAPTURE_HELP_SCREENSHOTS=1</c>. No-ops otherwise.
    /// </para>
    /// </summary>
    public sealed class HelpScreenshotCaptureTests
    {
        private static readonly HashSet<string> OptionlessTypes = new(StringComparer.Ordinal)
        {
            "ShrinkSpaces",
            "RemoveSpaces",
            "StripSpacesLeft",
            "StripSpacesRight",
            "SeparateCapitalizedText",
            "UppercaseInitials",
        };

        private static readonly HashSet<string> WideTypes = new(StringComparer.Ordinal)
        {
            "AudioTagSetter",
            "Id3v2FieldSetter",
            "ReplaceList",
            "NameList",
            "LettersCase",
            "PathMover",
            "Counter",
            "Inserter",
            "TrimLeft",
            "TrimRight",
            "ExtractLeft",
            "ExtractRight",
            "TrimBetween",
            "Replacer",
            "Formatter",
            "TokenMover",
        };

        private const double CaptureMaxWidth = 720.0;

        /// <summary>
        /// Renders every option-bearing filter editor to <c>help/images/{Type}.png</c>.
        /// </summary>
        [AvaloniaFact]
        public void Capture_filter_option_bodies_to_help_images()
        {
            if (Environment.GetEnvironmentVariable("MFR_CAPTURE_HELP_SCREENSHOTS") != "1")
            {
                return;
            }

            var outputDir = _ResolveHelpImagesDirectory();
            Directory.CreateDirectory(outputDir);
            var locator = new FilterEditorViewLocator();

            var captured = new List<string>();
            foreach (var entry in FilterCatalog.Entries.Where(e => !OptionlessTypes.Contains(e.Type)))
            {
                var filter = FilterCatalog.CreateDefault(entry);
                var step = new FilterChainStepViewModel(entry.DisplayName, filter);
                var editor = FilterOptionsEditorFactory.Create(step);
                Assert.NotNull(editor);

                editor.FormatTokenPickerExpanded = false;
                _ApplyDemoContent(editor);

                var view = locator.Build(editor);
                Assert.NotNull(view);

                // Measure at a generous max width, then size the bitmap to DesiredSize (no forced stretch).
                var measureWidth = WideTypes.Contains(entry.Type) ? CaptureMaxWidth : 520.0;
                var host = new Border
                {
                    Background = Brushes.White,
                    Padding = new Thickness(8),
                    Child = view,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                };

                var window = new Window
                {
                    Width = CaptureMaxWidth + 48,
                    Height = 900,
                    Background = Brushes.White,
                    Content = host,
                };

                try
                {
                    window.Show();
                    host.Measure(new Size(measureWidth, double.PositiveInfinity));
                    var desired = host.DesiredSize;
                    var pixelWidth = Math.Max(1, (int)Math.Ceiling(Math.Min(desired.Width, CaptureMaxWidth)));
                    var pixelHeight = Math.Max(1, (int)Math.Ceiling(desired.Height));
                    if (pixelWidth < 120 || pixelHeight < 16)
                    {
                        throw new InvalidOperationException(
                            $"{entry.Type} rendered unexpectedly small ({pixelWidth}x{pixelHeight})."
                        );
                    }

                    host.Width = pixelWidth;
                    host.Height = pixelHeight;
                    host.Arrange(new Rect(0, 0, pixelWidth, pixelHeight));
                    window.UpdateLayout();
                    Dispatcher.UIThread.RunJobs();

                    var path = Path.Combine(outputDir, $"{entry.Type}.png");
                    using var bitmap = new RenderTargetBitmap(
                        new PixelSize(pixelWidth, pixelHeight),
                        new Vector(96, 96)
                    );
                    bitmap.Render(host);
                    bitmap.Save(path, PngBitmapEncoderOptions.Default);
                    captured.Add(entry.Type);
                }
                finally
                {
                    window.Close();
                }
            }

            Assert.True(captured.Count >= 30, "Expected at least 30 option-bearing filters; got " + captured.Count);
        }

        private static void _ApplyDemoContent(FilterOptionsEditorViewModel editor)
        {
            switch (editor)
            {
                case ReplacerFilterEditorViewModel replacer:
                    replacer.Find = "_";
                    replacer.Replacement = " ";
                    break;
                case ReplaceListFilterEditorViewModel replaceList:
                    replaceList.EntriesText = "a => b\nBlue Train => Blue_Train";
                    break;
                case CasingListFilterEditorViewModel casingList:
                    casingList.WordsText = "McDonald HTML NASA";
                    break;
                case NameListFilterEditorViewModel nameList:
                    nameList.EntriesText = "Intro\nVerse\nChorus";
                    break;
                case FormatterFilterEditorViewModel formatter:
                    formatter.Template = "<counter:initial=1,step=1>.<file-name>";
                    break;
                case InserterFilterEditorViewModel inserter:
                    inserter.InsertText = " - <file-ext>";
                    break;
                default:
                    break;
            }
        }

        private static string _ResolveHelpImagesDirectory([CallerFilePath] string sourceFile = "")
        {
            var testsDir = Path.GetDirectoryName(sourceFile)!;
            var root = Path.GetFullPath(Path.Combine(testsDir, "..", "..", ".."));
            return Path.Combine(root, "help", "images");
        }
    }
}

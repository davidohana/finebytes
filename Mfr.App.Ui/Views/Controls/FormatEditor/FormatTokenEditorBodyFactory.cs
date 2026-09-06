using Avalonia.Controls;
using Mfr.App.Ui.ViewModels.Controls.FormatEditor;
using Mfr.App.Ui.ViewModels.Controls.FormatEditor.TokenEditors;
using Mfr.App.Ui.Views.Controls.FormatEditor.TokenEditors;

namespace Mfr.App.Ui.Views.Controls.FormatEditor
{
    /// <summary>
    /// Creates Avalonia bodies for format-token parameter editor view-models.
    /// </summary>
    internal static class FormatTokenEditorBodyFactory
    {
        /// <summary>
        /// Creates the Avalonia body control for <paramref name="editor"/>.
        /// </summary>
        /// <param name="editor">Editor view-model from <see cref="FormatTokenEditorRegistry.TryCreate"/>.</param>
        /// <returns>UserControl bound to <paramref name="editor"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the editor type is unknown.</exception>
        public static Control Create(IFormatTokenEditorViewModel editor)
        {
            ArgumentNullException.ThrowIfNull(editor);
            return editor switch
            {
                CounterFormatTokenEditorViewModel vm => new CounterFormatTokenEditorView { DataContext = vm },
                ParentFolderFormatTokenEditorViewModel vm => new ParentFolderFormatTokenEditorView { DataContext = vm },
                NowFormatTokenEditorViewModel vm => new NowFormatTokenEditorView { DataContext = vm },
                ExifDateFormatTokenEditorViewModel vm => new ExifDateFormatTokenEditorView { DataContext = vm },
                RandomCharFormatTokenEditorViewModel vm => new RandomCharFormatTokenEditorView { DataContext = vm },
                SubstrFormatTokenEditorViewModel vm => new SubstrFormatTokenEditorView { DataContext = vm },
                TokenFormatTokenEditorViewModel vm => new TokenFormatTokenEditorView { DataContext = vm },
                FileDateFormatTokenEditorViewModel vm => new FileDateFormatTokenEditorView { DataContext = vm },
                FileSizeFormatTokenEditorViewModel vm => new FileSizeFormatTokenEditorView { DataContext = vm },
                Id3v2FormatTokenEditorViewModel vm => new Id3v2FormatTokenEditorView { DataContext = vm },
                ExifFormatTokenEditorViewModel vm => new ExifFormatTokenEditorView { DataContext = vm },
                _ => throw new ArgumentOutOfRangeException(nameof(editor), editor.GetType(), "Unknown token editor."),
            };
        }
    }
}

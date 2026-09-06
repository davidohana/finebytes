using Mfr.App.Ui.ViewModels.Controls.FormatEditor.TokenEditors;

namespace Mfr.App.Ui.ViewModels.Controls.FormatEditor
{
    /// <summary>
    /// Maps canonical format-token names to parameter editor view-models.
    /// </summary>
    public static class FormatTokenEditorRegistry
    {
        /// <summary>
        /// Returns whether <paramref name="canonicalName"/> has a parameter editor.
        /// </summary>
        /// <param name="canonicalName">Token canonical name.</param>
        /// <returns><see langword="true"/> when an editor is registered.</returns>
        public static bool HasEditor(string canonicalName)
        {
            return _TryResolveFactory(canonicalName, out _);
        }

        /// <summary>
        /// Creates a parameter editor for <paramref name="canonicalName"/> when registered.
        /// </summary>
        /// <param name="canonicalName">Token canonical name.</param>
        /// <param name="args">Existing argument text (may be empty).</param>
        /// <param name="editor">Created editor when the method returns <see langword="true"/>.</param>
        /// <returns><see langword="true"/> when an editor was created.</returns>
        public static bool TryCreate(string canonicalName, string args, out IFormatTokenEditorViewModel? editor)
        {
            editor = null;
            if (!_TryResolveFactory(canonicalName, out var factory))
            {
                return false;
            }

            editor = factory(args ?? string.Empty);
            return true;
        }

        private static bool _TryResolveFactory(
            string? canonicalName,
            out Func<string, IFormatTokenEditorViewModel> factory
        )
        {
            factory = null!;
            if (string.IsNullOrWhiteSpace(canonicalName))
            {
                return false;
            }

            factory = canonicalName.Trim().ToLowerInvariant() switch
            {
                "counter" => static args => new CounterFormatTokenEditorViewModel(args),
                "parent-folder" => static args => new ParentFolderFormatTokenEditorViewModel(args),
                "now" => static args => new NowFormatTokenEditorViewModel(args),
                "exif-date" => static args => new ExifDateFormatTokenEditorViewModel(args),
                "random-char" => static args => new RandomCharFormatTokenEditorViewModel(args),
                "substr" => static args => new SubstrFormatTokenEditorViewModel(args),
                "token" => static args => new TokenFormatTokenEditorViewModel(args),
                "file-date" => static args => new FileDateFormatTokenEditorViewModel(args),
                "file-size" => static args => new FileSizeFormatTokenEditorViewModel(args),
                "id3v2" => static args => new Id3v2FormatTokenEditorViewModel(args),
                "exif" => static args => new ExifFormatTokenEditorViewModel(args),
                _ => null!,
            };
            return factory is not null;
        }
    }
}

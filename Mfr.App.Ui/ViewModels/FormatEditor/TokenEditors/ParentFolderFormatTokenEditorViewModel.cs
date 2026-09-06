using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Mfr.App.Ui.ViewModels.FormatEditor.TokenEditors
{
    /// <summary>
    /// Parameter editor for the <c>&lt;parent-folder&gt;</c> format token.
    /// </summary>
    internal sealed partial class ParentFolderFormatTokenEditorViewModel : FormatTokenEditorViewModelBase
    {
        /// <summary>
        /// Initializes the editor from existing arguments (empty → level 1).
        /// </summary>
        /// <param name="args">Optional level argument.</param>
        public ParentFolderFormatTokenEditorViewModel(string? args)
            : base("Parent Folder", "parent-folder")
        {
            if (
                !string.IsNullOrWhiteSpace(args)
                && int.TryParse(args.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var level)
                && level >= 1
            )
            {
                Level = level;
            }
        }

        /// <summary>
        /// Gets or sets the 1-based ancestor level (1 = immediate parent).
        /// </summary>
        [ObservableProperty]
        private decimal _level = 1;

        /// <inheritdoc />
        public override string BuildInnerText()
        {
            var level = Math.Max(1, (int)Level);
            return level == 1 ? CanonicalName : CanonicalName + ":" + NamedFormatOptionsBuilder.FormatInt(level);
        }
    }
}

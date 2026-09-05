using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Filters.Replace;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Replace
{
    /// <summary>
    /// Shared Mode / CaseSensitive / ReplaceAll / WholeWord fields for Replacer and Replace List editors.
    /// </summary>
    /// <remarks>
    /// Initializes match fields with the given whole-word default (Replacer off, Replace List on).
    /// </remarks>
    /// <param name="defaultWholeWord">Initial <see cref="WholeWord"/> before the first load.</param>
    internal sealed partial class ReplacerMatchOptionsEditor(bool defaultWholeWord) : ObservableObject
    {
        private Action? _onChanged;

        /// <summary>
        /// Gets or sets the pattern interpretation mode.
        /// </summary>
        [ObservableProperty]
        private ReplacerMode _mode = ReplacerMode.Literal;

        /// <summary>
        /// Gets or sets whether matching is case-sensitive.
        /// </summary>
        [ObservableProperty]
        private bool _caseSensitive;

        /// <summary>
        /// Gets or sets whether all matches are replaced.
        /// </summary>
        [ObservableProperty]
        private bool _replaceAll = true;

        /// <summary>
        /// Gets or sets whether matching is constrained to whole words.
        /// </summary>
        [ObservableProperty]
        private bool _wholeWord = defaultWholeWord;

        /// <summary>
        /// Registers the callback invoked when any match field changes.
        /// </summary>
        /// <param name="onChanged">Handler (typically parent apply + dependent PropertyChanged).</param>
        public void Bind(Action onChanged)
        {
            _onChanged = onChanged;
        }

        /// <summary>
        /// Copies values from filter options into the editor fields.
        /// </summary>
        /// <param name="options">Match options from the applied filter.</param>
        public void Load(ReplacerMatchOptions options)
        {
            Mode = options.Mode;
            CaseSensitive = options.CaseSensitive;
            ReplaceAll = options.ReplaceAll;
            WholeWord = options.WholeWord;
        }

        /// <summary>
        /// Builds filter match options from the current editor fields.
        /// </summary>
        /// <returns>A <see cref="ReplacerMatchOptions"/> snapshot.</returns>
        public ReplacerMatchOptions ToOptions()
        {
            return new(Mode: Mode, CaseSensitive: CaseSensitive, ReplaceAll: ReplaceAll, WholeWord: WholeWord);
        }

        partial void OnModeChanged(ReplacerMode value) => _onChanged?.Invoke();

        partial void OnCaseSensitiveChanged(bool value) => _onChanged?.Invoke();

        partial void OnReplaceAllChanged(bool value) => _onChanged?.Invoke();

        partial void OnWholeWordChanged(bool value) => _onChanged?.Invoke();
    }
}

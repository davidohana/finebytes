using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Models.Tags.Id3v1;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Audio
{
    /// <summary>
    /// One per-field row: three-state mode checkbox + format/value text (+ optional auto-increment).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IsActive"/> maps to Avalonia three-state <c>IsChecked</c>:
    /// <c>true</c> = always set, <c>null</c> = only if empty, <c>false</c> = omit field from options.
    /// </para>
    /// </remarks>
    internal sealed partial class AudioTagSetterFieldRowViewModel : ObservableObject
    {
        /// <summary>
        /// <c>Tag</c> value on the track auto-increment checkbox (headless lookup).
        /// </summary>
        public const string AutoIncrementTag = "AutoIncrement";

        private static readonly IReadOnlyList<string> _SortedGenreNames =
        [
            .. Id3v1Genres.Names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase),
        ];

        private readonly Action _onChanged;

        /// <summary>
        /// Initializes a row from catalog metadata.
        /// </summary>
        /// <param name="choice">Label, tip, and field kind.</param>
        /// <param name="onChanged">Invoked when mode, text, or auto-increment changes.</param>
        public AudioTagSetterFieldRowViewModel(AudioTagSetterFieldChoice choice, Action onChanged)
        {
            ArgumentNullException.ThrowIfNull(choice);
            ArgumentNullException.ThrowIfNull(onChanged);
            Kind = choice.Kind;
            Group = choice.Group;
            Label = choice.Label;
            Tip = choice.Tip;
            Watermark = choice.Watermark;
            ShowsAutoIncrement = choice.ShowsAutoIncrement;
            Multiline = choice.Multiline;
            UsesGenreCombo = choice.UsesGenreCombo;
            _onChanged = onChanged;
            _autoIncrement = choice.ShowsAutoIncrement;
        }

        /// <summary>
        /// Gets which overlay field this row edits.
        /// </summary>
        public AudioTagSetterFieldKind Kind { get; }

        /// <summary>
        /// Gets which options fieldset this row belongs to.
        /// </summary>
        public AudioTagSetterFieldGroup Group { get; }

        /// <summary>
        /// Gets the three-state checkbox label.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Gets the per-field tooltip text.
        /// </summary>
        public string Tip { get; }

        /// <summary>
        /// Gets the value-box watermark.
        /// </summary>
        public string Watermark { get; }

        /// <summary>
        /// Gets whether the auto-increment checkbox is shown (track only).
        /// </summary>
        public bool ShowsAutoIncrement { get; }

        /// <summary>
        /// Gets whether the value box is multi-line (lyrics).
        /// </summary>
        public bool Multiline { get; }

        /// <summary>
        /// Gets whether the value control is an editable genre ComboBox.
        /// </summary>
        public bool UsesGenreCombo { get; }

        /// <summary>
        /// Gets whether the single-line text box is shown (not genre combo / lyrics).
        /// </summary>
        public bool ShowsPlainText => !Multiline && !UsesGenreCombo;

        /// <summary>
        /// Gets ID3v1 genre suggestions for the genre ComboBox (alphabetical, MFR7 Sorted).
        /// </summary>
        public IReadOnlyList<string> GenreSuggestions => UsesGenreCombo ? _SortedGenreNames : [];

        /// <summary>
        /// Gets or sets the three-state activation (<c>true</c> set, <c>null</c> only-if-empty, <c>false</c> omit).
        /// </summary>
        [ObservableProperty]
        private bool? _isActive = false;

        /// <summary>
        /// Gets or sets the format/literal text for this field.
        /// </summary>
        [ObservableProperty]
        private string _text = string.Empty;

        /// <summary>
        /// Gets or sets whether track auto-increment is enabled (meaningful only when <see cref="ShowsAutoIncrement"/>).
        /// </summary>
        [ObservableProperty]
        private bool _autoIncrement;

        partial void OnIsActiveChanged(bool? value) => _onChanged();

        partial void OnTextChanged(string value)
        {
            // Typing into an omitted field enables overwrite mode so the value is applied.
            if (IsActive is false && !string.IsNullOrEmpty(value))
            {
                IsActive = true;
                return;
            }

            _onChanged();
        }

        partial void OnAutoIncrementChanged(bool value) => _onChanged();
    }
}

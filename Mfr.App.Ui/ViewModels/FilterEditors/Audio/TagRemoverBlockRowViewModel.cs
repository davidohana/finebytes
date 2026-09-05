using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Models.Tags;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Audio
{
    /// <summary>
    /// One selective block-type checkbox row for <see cref="TagRemoverFilterEditorViewModel"/>.
    /// </summary>
    internal sealed partial class TagRemoverBlockRowViewModel : ObservableObject
    {
        private readonly Action _onSelectionChanged;

        /// <summary>
        /// Initializes a row from catalog metadata.
        /// </summary>
        /// <param name="choice">Display name, tip, and block kind.</param>
        /// <param name="onSelectionChanged">Invoked when <see cref="IsSelected"/> changes.</param>
        public TagRemoverBlockRowViewModel(AudioTagBlockKindChoice choice, Action onSelectionChanged)
        {
            ArgumentNullException.ThrowIfNull(choice);
            ArgumentNullException.ThrowIfNull(onSelectionChanged);
            Kind = choice.Kind;
            DisplayName = choice.DisplayName;
            Tip = choice.Tip;
            _onSelectionChanged = onSelectionChanged;
        }

        /// <summary>
        /// Gets the modeled tag block kind this row toggles.
        /// </summary>
        public AudioTagBlockKind Kind { get; }

        /// <summary>
        /// Gets the checkbox label.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the per-kind tooltip text.
        /// </summary>
        public string Tip { get; }

        /// <summary>
        /// Gets or sets whether this block kind is selected for selective removal.
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;

        partial void OnIsSelectedChanged(bool value) => _onSelectionChanged();
    }
}

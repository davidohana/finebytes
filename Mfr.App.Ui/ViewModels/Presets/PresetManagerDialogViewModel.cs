using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.Presets
{
    /// <summary>
    /// Sorted preset list and selection for the Preset Manager dialog.
    /// </summary>
    public sealed partial class PresetManagerDialogViewModel : ViewModelBase
    {
        private readonly AppliedFiltersViewModel _appliedFilters;

        /// <summary>
        /// Initializes the manager list from <paramref name="appliedFilters"/>.
        /// </summary>
        /// <param name="appliedFilters">Applied Filters pane that owns the <see cref="Engine.Presets.PresetManager"/>.</param>
        public PresetManagerDialogViewModel(AppliedFiltersViewModel appliedFilters)
        {
            ArgumentNullException.ThrowIfNull(appliedFilters);
            _appliedFilters = appliedFilters;
            Presets = [];
            Refresh();
        }

        /// <summary>
        /// Gets presets sorted by name (ordinal-ignore-case, then ordinal).
        /// </summary>
        public ObservableCollection<FilterPreset> Presets { get; }

        /// <summary>
        /// Gets or sets the selected preset in the list.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasSelection))]
        [NotifyPropertyChangedFor(nameof(SelectedDescription))]
        private FilterPreset? _selectedPreset;

        /// <summary>
        /// Gets whether a preset is selected (enables Load / Delete / Rename).
        /// </summary>
        public bool HasSelection => SelectedPreset is not null;

        /// <summary>
        /// Gets the selected preset’s description for the read-only pane (empty when none).
        /// </summary>
        public string SelectedDescription => SelectedPreset?.Description ?? string.Empty;

        /// <summary>
        /// Rebuilds <see cref="Presets"/> from the manager, preserving selection by name when possible.
        /// </summary>
        /// <param name="preferredName">
        /// Optional name to select after rebuild (e.g. after rename). When null, keeps the current
        /// selection name when still present.
        /// </param>
        public void Refresh(string? preferredName = null)
        {
            var selectedName = preferredName ?? SelectedPreset?.Name;
            Presets.Clear();
            foreach (var preset in PresetNameOrder.ByName(_appliedFilters.PresetManager.NameToPreset.Values))
            {
                Presets.Add(preset);
            }

            if (selectedName is null)
            {
                SelectedPreset = Presets.FirstOrDefault();
                return;
            }

            SelectedPreset =
                Presets.FirstOrDefault(preset => string.Equals(preset.Name, selectedName, StringComparison.Ordinal))
                ?? Presets.FirstOrDefault();
        }
    }
}

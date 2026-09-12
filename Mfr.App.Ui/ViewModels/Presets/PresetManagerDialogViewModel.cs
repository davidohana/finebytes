using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Models.Filters;
using Mfr.Utils;

namespace Mfr.App.Ui.ViewModels.Presets
{
    /// <summary>
    /// Ordered preset list and multi-selection for the Preset Manager dialog.
    /// </summary>
    public sealed partial class PresetManagerDialogViewModel : ViewModelBase
    {
        private readonly AppliedFiltersViewModel _appliedFilters;
        private readonly List<FilterPreset> _selectedPresets = [];

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
        /// Gets presets in <see cref="Engine.Presets.PresetManager.Presets"/> stored order.
        /// </summary>
        public ObservableCollection<FilterPreset> Presets { get; }

        /// <summary>
        /// Gets the current multi-selection (list order).
        /// </summary>
        public IReadOnlyList<FilterPreset> SelectedPresets => _selectedPresets;

        /// <summary>
        /// Gets whether at least one preset is selected (enables Delete).
        /// </summary>
        public bool HasSelection => _selectedPresets.Count > 0;

        /// <summary>
        /// Gets whether exactly one preset is selected (enables Load / Rename / description).
        /// </summary>
        public bool HasSingleSelection => _selectedPresets.Count == 1;

        /// <summary>
        /// Gets the sole selected preset’s description, or empty when not exactly one is selected.
        /// </summary>
        public string SelectedDescription =>
            HasSingleSelection ? _selectedPresets[0].Description ?? string.Empty : string.Empty;

        /// <summary>
        /// Replaces the current multi-selection.
        /// </summary>
        /// <param name="presets">Selected presets in list order.</param>
        public void SetSelectedPresets(IReadOnlyList<FilterPreset> presets)
        {
            ArgumentNullException.ThrowIfNull(presets);

            _selectedPresets.Clear();
            foreach (var preset in presets)
            {
                if (Presets.Contains(preset))
                {
                    _selectedPresets.Add(preset);
                }
            }

            OnPropertyChanged(nameof(SelectedPresets));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(HasSingleSelection));
            OnPropertyChanged(nameof(SelectedDescription));
            _NotifySelectionCommandsChanged();
        }

        /// <summary>
        /// Rebuilds <see cref="Presets"/> from the manager, restoring selection by name when possible.
        /// </summary>
        /// <param name="preferredName">
        /// Optional sole name to select after rebuild (e.g. after rename). When null, keeps current
        /// selection names that are still present.
        /// </param>
        /// <remarks>
        /// When every previously selected name is gone (e.g. after delete), selects the row at the
        /// prior first-selected index (clamped), matching Applied Filters / Rename List remove.
        /// </remarks>
        public void Refresh(string? preferredName = null)
        {
            IReadOnlyList<string> selectedNames = preferredName is not null
                ? [preferredName]
                : [.. _selectedPresets.Select(preset => preset.Name)];
            var anchorIndex = _selectedPresets
                .Select(Presets.IndexOf)
                .Where(index => index >= 0)
                .DefaultIfEmpty(-1)
                .Min();

            _ReloadPresetsFromManager();

            if (selectedNames.Count == 0)
            {
                SetSelectedPresets(Presets.Count > 0 ? [Presets[0]] : []);
                return;
            }

            var restored = Presets
                .Where(preset => selectedNames.Contains(preset.Name, StringComparer.Ordinal))
                .ToList();
            if (restored.Count == 0)
            {
                restored = [.. _SelectPresetsAfterRemove(anchorIndex)];
            }

            SetSelectedPresets(restored);
        }

        /// <summary>
        /// Moves the selected presets one position up and persists.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedUp))]
        public void MoveSelectedUp()
        {
            _MoveSelected(offset: -1);
        }

        /// <summary>
        /// Moves the selected presets one position down and persists.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanMoveSelectedDown))]
        public void MoveSelectedDown()
        {
            _MoveSelected(offset: 1);
        }

        /// <summary>
        /// Moves presets at <paramref name="sourceIndices"/> to <paramref name="targetIndex"/> and persists.
        /// </summary>
        /// <param name="sourceIndices">Indices of rows to move.</param>
        /// <param name="targetIndex">Destination index in <c>[0, Count]</c> before the move.</param>
        public void MovePresetsTo(IReadOnlyList<int> sourceIndices, int targetIndex)
        {
            ArgumentNullException.ThrowIfNull(sourceIndices);

            if (!_appliedFilters.TryMovePresetsTo(sourceIndices, targetIndex, out var newIndices))
            {
                return;
            }

            _ReloadPresetsFromManager();
            SetSelectedPresets([.. newIndices.Select(index => Presets[index])]);
        }

        private void _MoveSelected(int offset)
        {
            if (_selectedPresets.Count == 0)
            {
                return;
            }

            var names = _selectedPresets.Select(preset => preset.Name).ToList();
            if (!_appliedFilters.TryMovePresetsTowardNeighbor(names, offset))
            {
                return;
            }

            Refresh();
        }

        /// <summary>
        /// Replaces <see cref="Presets"/> from the engine list without changing selection.
        /// </summary>
        private void _ReloadPresetsFromManager()
        {
            Presets.Clear();
            foreach (var preset in _appliedFilters.PresetManager.Presets)
            {
                Presets.Add(preset);
            }
        }

        /// <summary>
        /// Picks the row to focus after delete: same index when possible, otherwise the last row.
        /// </summary>
        /// <param name="anchorIndex">First selected index before the remove.</param>
        /// <returns>Zero or one preset to select.</returns>
        private IReadOnlyList<FilterPreset> _SelectPresetsAfterRemove(int anchorIndex)
        {
            if (Presets.Count == 0 || anchorIndex < 0)
            {
                return [];
            }

            var nextIndex = Math.Min(anchorIndex, Presets.Count - 1);
            return [Presets[nextIndex]];
        }

        private bool _CanMoveSelectedUp()
        {
            return _selectedPresets.Count > 0
                && ListReorder.CanMoveSelectedTowardNeighbor(Presets, _selectedPresets.ToHashSet(), offset: -1);
        }

        private bool _CanMoveSelectedDown()
        {
            return _selectedPresets.Count > 0
                && ListReorder.CanMoveSelectedTowardNeighbor(Presets, _selectedPresets.ToHashSet(), offset: 1);
        }

        private void _NotifySelectionCommandsChanged()
        {
            MoveSelectedUpCommand.NotifyCanExecuteChanged();
            MoveSelectedDownCommand.NotifyCanExecuteChanged();
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Models.Filters;
using Mfr.Utils;

namespace Mfr.App.Ui.ViewModels.Presets
{
    /// <summary>
    /// Draft state for the Save Preset dialog (name, description, optional Rename List columns).
    /// </summary>
    public sealed partial class SavePresetDialogViewModel : ViewModelBase
    {
        private readonly Dictionary<string, FilterPreset> _nameToPreset;

        /// <summary>
        /// Initializes draft fields, optionally prefilled from the last-loaded preset.
        /// </summary>
        /// <param name="lastLoaded">
        /// Preset to prefill from when present; when <see langword="null"/>, fields start empty.
        /// </param>
        /// <param name="existingPresets">
        /// Current presets for the Name suggestions list; when <see langword="null"/>, suggestions are empty.
        /// </param>
        /// <param name="prefillFromLastLoaded">
        /// When <see langword="true"/> and <paramref name="lastLoaded"/> is set, Name / description /
        /// columns start from that preset. Pass <see langword="false"/> when Applied Filters is empty
        /// so the Name field starts blank.
        /// </param>
        public SavePresetDialogViewModel(
            FilterPreset? lastLoaded = null,
            IEnumerable<FilterPreset>? existingPresets = null,
            bool prefillFromLastLoaded = true
        )
        {
            var presets = existingPresets?.ToList() ?? [];
            _nameToPreset = presets.ToDictionary(preset => preset.Name, StringComparer.Ordinal);
            ExistingNames = [.. presets.Select(preset => preset.Name)];

            if (lastLoaded is null || !prefillFromLastLoaded)
            {
                return;
            }

            Name = lastLoaded.Name;
            Description = lastLoaded.Description ?? string.Empty;
            SaveRenameListColumns = lastLoaded.VisibleColumns is not null;
        }

        /// <summary>
        /// Gets existing preset names for the Name suggestions list (caller order).
        /// </summary>
        public IReadOnlyList<string> ExistingNames { get; }

        /// <summary>
        /// Gets or sets the preset display name.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanSave))]
        private string _name = string.Empty;

        /// <summary>
        /// Gets or sets the optional preset description.
        /// </summary>
        [ObservableProperty]
        private string _description = string.Empty;

        /// <summary>
        /// Gets or sets whether the current Rename List visible columns should be stored.
        /// </summary>
        [ObservableProperty]
        private bool _saveRenameListColumns;

        /// <summary>
        /// Gets whether Save is enabled (non-blank name).
        /// </summary>
        public bool CanSave => !string.IsNullOrWhiteSpace(Name);

        /// <summary>
        /// Gets the trimmed preset name for save.
        /// </summary>
        public string TrimmedName => Name.Trim();

        /// <summary>
        /// Gets the trimmed description, or <see langword="null"/> when blank.
        /// </summary>
        public string? TrimmedDescriptionOrNull => Description.TrimmedOrNull();

        /// <summary>
        /// Prefills description and columns from an existing preset chosen in the Name suggestions list.
        /// <para>
        /// Call only on ComboBox selection — not on free-typed Name changes — so typing an existing
        /// name does not wipe a description the user already edited.
        /// </para>
        /// </summary>
        /// <param name="selectedName">Selected suggestion name, or <see langword="null"/> when cleared.</param>
        public void ApplySuggestion(string? selectedName)
        {
            if (selectedName is null)
            {
                return;
            }

            var trimmed = selectedName.Trim();
            if (trimmed.Length == 0 || !_nameToPreset.TryGetValue(trimmed, out var preset))
            {
                return;
            }

            Description = preset.Description ?? string.Empty;
            SaveRenameListColumns = preset.VisibleColumns is not null;
        }
    }
}

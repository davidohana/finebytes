using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Engine.Presets;
using Mfr.Models.Filters;

namespace Mfr.App.Ui.ViewModels.Presets
{
    /// <summary>
    /// Checklist state for choosing curated sample presets to import.
    /// </summary>
    public sealed partial class ImportSamplePresetsDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a checked row for every sample preset.
        /// </summary>
        /// <param name="presets">
        /// Samples to display; when <see langword="null"/>, uses <see cref="SamplePresetCatalog"/>.
        /// </param>
        public ImportSamplePresetsDialogViewModel(IEnumerable<FilterPreset>? presets = null)
        {
            Items =
            [
                .. (presets ?? SamplePresetCatalog.Presets).Select(preset =>
                {
                    var item = new SamplePresetSelectionViewModel(preset);
                    item.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(SamplePresetSelectionViewModel.IsSelected))
                        {
                            OnPropertyChanged(nameof(CanImport));
                        }
                    };
                    return item;
                }),
            ];
        }

        /// <summary>
        /// Gets the sample preset checklist rows.
        /// </summary>
        public IReadOnlyList<SamplePresetSelectionViewModel> Items { get; }

        /// <summary>
        /// Gets whether at least one sample is checked.
        /// </summary>
        public bool CanImport => Items.Any(item => item.IsSelected);

        /// <summary>
        /// Gets the exact names of the checked samples.
        /// </summary>
        /// <returns>Selected sample names in catalog order.</returns>
        public IReadOnlyList<string> SelectedNames()
        {
            return [.. Items.Where(item => item.IsSelected).Select(item => item.Name)];
        }

        /// <summary>
        /// Checks every sample row.
        /// </summary>
        public void SelectAll()
        {
            foreach (var item in Items)
            {
                item.IsSelected = true;
            }
        }

        /// <summary>
        /// Clears every sample row.
        /// </summary>
        public void SelectNone()
        {
            foreach (var item in Items)
            {
                item.IsSelected = false;
            }
        }
    }

    /// <summary>
    /// One selectable sample preset row.
    /// </summary>
    public sealed partial class SamplePresetSelectionViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a checked row from a sample preset.
        /// </summary>
        /// <param name="preset">Sample preset to display.</param>
        public SamplePresetSelectionViewModel(FilterPreset preset)
        {
            ArgumentNullException.ThrowIfNull(preset);
            Name = preset.Name;
            Description = preset.Description ?? string.Empty;
        }

        /// <summary>
        /// Gets the exact sample preset name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the sample preset description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets or sets whether this sample will be imported.
        /// </summary>
        [ObservableProperty]
        private bool _isSelected = true;
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Attributes;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Attributes
{
    /// <summary>
    /// Filter Configuration editor for <see cref="AttributesSetterFilter"/>.
    /// </summary>
    internal sealed partial class AttributesSetterFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public AttributesSetterFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets the read-only flag tri-state (<see langword="true"/> set, <see langword="false"/> clear, <see langword="null"/> keep).
        /// </summary>
        [ObservableProperty]
        private bool? _readOnlyChecked;

        /// <summary>
        /// Gets or sets the hidden flag tri-state (<see langword="true"/> set, <see langword="false"/> clear, <see langword="null"/> keep).
        /// </summary>
        [ObservableProperty]
        private bool? _hiddenChecked;

        /// <summary>
        /// Gets or sets the archive flag tri-state (<see langword="true"/> set, <see langword="false"/> clear, <see langword="null"/> keep).
        /// </summary>
        [ObservableProperty]
        private bool? _archiveChecked;

        /// <summary>
        /// Gets or sets the system flag tri-state (<see langword="true"/> set, <see langword="false"/> clear, <see langword="null"/> keep).
        /// </summary>
        [ObservableProperty]
        private bool? _systemChecked;

        partial void OnReadOnlyCheckedChanged(bool? value) => _ApplyOptions();

        partial void OnHiddenCheckedChanged(bool? value) => _ApplyOptions();

        partial void OnArchiveCheckedChanged(bool? value) => _ApplyOptions();

        partial void OnSystemCheckedChanged(bool? value) => _ApplyOptions();

        /// <summary>
        /// Loads checkbox states from the step filter without pushing option replaces.
        /// </summary>
        private void _SyncFromFilter()
        {
            if (Step.Filter is not AttributesSetterFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                ReadOnlyChecked = _ToChecked(filter.Options.ReadOnly);
                HiddenChecked = _ToChecked(filter.Options.Hidden);
                ArchiveChecked = _ToChecked(filter.Options.Archive);
                SystemChecked = _ToChecked(filter.Options.System);
            });
        }

        /// <summary>
        /// Builds options from the four tri-state checkboxes and replaces the step filter when changed.
        /// </summary>
        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not AttributesSetterFilter filter)
            {
                return;
            }

            var options = new AttributesSetterOptions(
                ReadOnly: _FromChecked(ReadOnlyChecked),
                Hidden: _FromChecked(HiddenChecked),
                Archive: _FromChecked(ArchiveChecked),
                System: _FromChecked(SystemChecked)
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }

        /// <summary>
        /// Maps a filter tri-state to a three-state checkbox value.
        /// </summary>
        private static bool? _ToChecked(AttributeTriState state)
        {
            return state switch
            {
                AttributeTriState.Set => true,
                AttributeTriState.Clear => false,
                AttributeTriState.Keep => null,
                _ => null,
            };
        }

        /// <summary>
        /// Maps a three-state checkbox value to a filter tri-state.
        /// </summary>
        private static AttributeTriState _FromChecked(bool? isChecked)
        {
            return isChecked switch
            {
                true => AttributeTriState.Set,
                false => AttributeTriState.Clear,
                _ => AttributeTriState.Keep,
            };
        }
    }
}

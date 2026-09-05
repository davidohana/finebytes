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
        /// Gets or sets the read-only flag mode (On / Off / Keep).
        /// </summary>
        [ObservableProperty]
        private AttributeTriState _readOnly;

        /// <summary>
        /// Gets or sets the hidden flag mode (On / Off / Keep).
        /// </summary>
        [ObservableProperty]
        private AttributeTriState _hidden;

        /// <summary>
        /// Gets or sets the archive flag mode (On / Off / Keep).
        /// </summary>
        [ObservableProperty]
        private AttributeTriState _archive;

        /// <summary>
        /// Gets or sets the system flag mode (On / Off / Keep).
        /// </summary>
        [ObservableProperty]
        private AttributeTriState _system;

        partial void OnReadOnlyChanged(AttributeTriState value) => _ApplyOptions();

        partial void OnHiddenChanged(AttributeTriState value) => _ApplyOptions();

        partial void OnArchiveChanged(AttributeTriState value) => _ApplyOptions();

        partial void OnSystemChanged(AttributeTriState value) => _ApplyOptions();

        /// <summary>
        /// Loads radio states from the step filter without pushing option replaces.
        /// </summary>
        private void _SyncFromFilter()
        {
            if (Step.Filter is not AttributesSetterFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                ReadOnly = filter.Options.ReadOnly;
                Hidden = filter.Options.Hidden;
                Archive = filter.Options.Archive;
                System = filter.Options.System;
            });
        }

        /// <summary>
        /// Builds options from the four attribute modes and replaces the step filter when changed.
        /// </summary>
        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not AttributesSetterFilter filter)
            {
                return;
            }

            var options = new AttributesSetterOptions(
                ReadOnly: ReadOnly,
                Hidden: Hidden,
                Archive: Archive,
                System: System
            );
            ApplyIfChanged(filter, filter with { Options = options });
        }
    }
}

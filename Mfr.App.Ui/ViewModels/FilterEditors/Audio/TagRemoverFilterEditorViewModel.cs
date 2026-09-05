using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters.Audio;
using Mfr.Models.Tags;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Audio
{
    /// <summary>
    /// Filter Configuration editor for <see cref="TagRemoverFilter"/>.
    /// </summary>
    internal sealed partial class TagRemoverFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        public TagRemoverFilterEditorViewModel(AppliedFilterStepViewModel step)
            : base(step)
        {
            BlockRows =
            [
                .. AudioTagBlockKindChoice.All.Select(choice => new TagRemoverBlockRowViewModel(
                    choice,
                    _OnBlockChanged
                )),
            ];
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets the selective block-type checkbox rows (one per <see cref="AudioTagBlockKind"/>).
        /// </summary>
        public IReadOnlyList<TagRemoverBlockRowViewModel> BlockRows { get; }

        /// <summary>
        /// Gets or sets whether every TagLib tag type is stripped (nuclear mode).
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AreBlockTypesEnabled))]
        private bool _removeAll = true;

        /// <summary>
        /// Gets whether selective block-type checkboxes are enabled.
        /// </summary>
        public bool AreBlockTypesEnabled => !RemoveAll;

        partial void OnRemoveAllChanged(bool value)
        {
            if (IsLoading)
            {
                return;
            }

            if (value)
            {
                LoadWithoutApplying(_ClearBlockSelections);
            }

            _ApplyOptions();
        }

        /// <summary>
        /// Applies selective block selection (empty selection is a no-op, not nuclear).
        /// </summary>
        private void _OnBlockChanged()
        {
            if (IsLoading)
            {
                return;
            }

            _ApplyOptions();
        }

        /// <summary>
        /// Copies current filter options into editor properties without live replace.
        /// </summary>
        private void _SyncFromFilter()
        {
            if (Step.Filter is not TagRemoverFilter filter)
            {
                return;
            }

            LoadWithoutApplying(() =>
            {
                var all = filter.Options.All;
                var blocks = filter.Options.Blocks ?? [];

                RemoveAll = all;
                if (all)
                {
                    _ClearBlockSelections();
                    return;
                }

                var selected = blocks.ToHashSet();
                foreach (var row in BlockRows)
                {
                    row.IsSelected = selected.Contains(row.Kind);
                }
            });
        }

        /// <summary>
        /// Replaces the step filter when nuclear/selective options change.
        /// </summary>
        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not TagRemoverFilter filter)
            {
                return;
            }

            TagRemoverOptions options;
            if (RemoveAll)
            {
                options = new TagRemoverOptions(All: true);
            }
            else
            {
                // Empty selection is intentional no-op (matches unchecked = leave tags alone).
                options = new TagRemoverOptions(All: false, Blocks: _SelectedBlocks());
            }

            ApplyIfChanged(filter, filter with { Options = options });
        }

        /// <summary>
        /// Clears all selective block checkboxes (nuclear UI state).
        /// </summary>
        private void _ClearBlockSelections()
        {
            foreach (var row in BlockRows)
            {
                row.IsSelected = false;
            }
        }

        /// <summary>
        /// Builds the ordered list of checked selective block kinds.
        /// </summary>
        private List<AudioTagBlockKind> _SelectedBlocks()
        {
            return [.. BlockRows.Where(row => row.IsSelected).Select(row => row.Kind)];
        }
    }
}

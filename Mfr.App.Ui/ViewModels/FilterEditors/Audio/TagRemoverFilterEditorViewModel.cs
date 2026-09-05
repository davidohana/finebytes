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
            _SyncFromFilter();
        }

        /// <summary>
        /// Gets or sets whether every TagLib tag type is stripped (nuclear mode).
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AreBlockTypesEnabled))]
        private bool _removeAll = true;

        /// <summary>
        /// Gets or sets whether ID3v1 blocks are removed in selective mode.
        /// </summary>
        [ObservableProperty]
        private bool _removeId3v1;

        /// <summary>
        /// Gets or sets whether ID3v2 blocks are removed in selective mode.
        /// </summary>
        [ObservableProperty]
        private bool _removeId3v2;

        /// <summary>
        /// Gets or sets whether Xiph comment blocks are removed in selective mode.
        /// </summary>
        [ObservableProperty]
        private bool _removeXiph;

        /// <summary>
        /// Gets or sets whether APEv2 blocks are removed in selective mode.
        /// </summary>
        [ObservableProperty]
        private bool _removeApe;

        /// <summary>
        /// Gets or sets whether Apple/iTunes blocks are removed in selective mode.
        /// </summary>
        [ObservableProperty]
        private bool _removeApple;

        /// <summary>
        /// Gets or sets whether ASF blocks are removed in selective mode.
        /// </summary>
        [ObservableProperty]
        private bool _removeAsf;

        /// <summary>
        /// Gets or sets whether RIFF INFO blocks are removed in selective mode.
        /// </summary>
        [ObservableProperty]
        private bool _removeRiffInfo;

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

            // Leaving nuclear does not seed every block kind (no container supports all seven).
            // Stay on nuclear options until the user checks at least one type.
            _ApplyOptions();
        }

        partial void OnRemoveId3v1Changed(bool value) => _OnBlockChanged();

        partial void OnRemoveId3v2Changed(bool value) => _OnBlockChanged();

        partial void OnRemoveXiphChanged(bool value) => _OnBlockChanged();

        partial void OnRemoveApeChanged(bool value) => _OnBlockChanged();

        partial void OnRemoveAppleChanged(bool value) => _OnBlockChanged();

        partial void OnRemoveAsfChanged(bool value) => _OnBlockChanged();

        partial void OnRemoveRiffInfoChanged(bool value) => _OnBlockChanged();

        /// <summary>
        /// Snaps empty selective mode back to nuclear, then applies options.
        /// </summary>
        private void _OnBlockChanged()
        {
            if (IsLoading)
            {
                return;
            }

            if (!RemoveAll && !_AnyBlockSelected())
            {
                LoadWithoutApplying(() => RemoveAll = true);
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
                if (!all && blocks.Count == 0)
                {
                    all = true;
                }

                RemoveAll = all;
                if (all)
                {
                    _ClearBlockFlags();
                    return;
                }

                var selected = blocks.ToHashSet();
                RemoveId3v1 = selected.Contains(AudioTagBlockKind.Id3v1);
                RemoveId3v2 = selected.Contains(AudioTagBlockKind.Id3v2);
                RemoveXiph = selected.Contains(AudioTagBlockKind.Xiph);
                RemoveApe = selected.Contains(AudioTagBlockKind.Ape);
                RemoveApple = selected.Contains(AudioTagBlockKind.Apple);
                RemoveAsf = selected.Contains(AudioTagBlockKind.Asf);
                RemoveRiffInfo = selected.Contains(AudioTagBlockKind.RiffInfo);
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
                var blocks = _SelectedBlocks();
                if (blocks.Count == 0)
                {
                    // Selective UI with nothing checked yet — keep nuclear options on the step.
                    return;
                }

                options = new TagRemoverOptions(All: false, Blocks: blocks);
            }

            ApplyIfChanged(filter, filter with { Options = options });
        }

        /// <summary>
        /// Returns whether any selective block checkbox is checked.
        /// </summary>
        private bool _AnyBlockSelected()
        {
            return RemoveId3v1 || RemoveId3v2 || RemoveXiph || RemoveApe || RemoveApple || RemoveAsf || RemoveRiffInfo;
        }

        /// <summary>
        /// Clears all selective block checkboxes (nuclear UI state).
        /// </summary>
        private void _ClearBlockFlags()
        {
            RemoveId3v1 = false;
            RemoveId3v2 = false;
            RemoveXiph = false;
            RemoveApe = false;
            RemoveApple = false;
            RemoveAsf = false;
            RemoveRiffInfo = false;
        }

        /// <summary>
        /// Builds the ordered list of checked selective block kinds.
        /// </summary>
        private List<AudioTagBlockKind> _SelectedBlocks()
        {
            var blocks = new List<AudioTagBlockKind>(7);
            if (RemoveId3v1)
            {
                blocks.Add(AudioTagBlockKind.Id3v1);
            }

            if (RemoveId3v2)
            {
                blocks.Add(AudioTagBlockKind.Id3v2);
            }

            if (RemoveXiph)
            {
                blocks.Add(AudioTagBlockKind.Xiph);
            }

            if (RemoveApe)
            {
                blocks.Add(AudioTagBlockKind.Ape);
            }

            if (RemoveApple)
            {
                blocks.Add(AudioTagBlockKind.Apple);
            }

            if (RemoveAsf)
            {
                blocks.Add(AudioTagBlockKind.Asf);
            }

            if (RemoveRiffInfo)
            {
                blocks.Add(AudioTagBlockKind.RiffInfo);
            }

            return blocks;
        }
    }
}

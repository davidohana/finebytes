using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.ViewModels.AppliedFilters;
using Mfr.Filters;
using Mfr.Filters.Trimming;
using Mfr.Models.Rename;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Trimming
{
    /// <summary>
    /// Filter Configuration editor for count-based filters (Trim/Extract Left/Right).
    /// </summary>
    internal sealed partial class CountFilterEditorViewModel : FilterOptionsEditorViewModel
    {
        /// <summary>
        /// Initializes the editor from the current step filter.
        /// </summary>
        /// <param name="step">Applied list row.</param>
        /// <param name="sampleRenameItems">Rename List items for Visual Trim Helper init; may be empty.</param>
        /// <param name="resolveRenameItemByFullPath">
        /// Looks up a Rename List row by original full path for helper drag-drop.
        /// </param>
        public CountFilterEditorViewModel(
            AppliedFilterStepViewModel step,
            IReadOnlyList<RenameItem>? sampleRenameItems = null,
            Func<string, RenameItem?>? resolveRenameItemByFullPath = null
        )
            : base(step)
        {
            TrimHelper = new VisualTrimHelperViewModel();
            TrimHelper.SelectionApplied += _OnTrimHelperSelectionApplied;
            TrimHelper.SampleChanged += _OnTrimHelperSampleChanged;
            _ConfigureTrimHelper(resolveRenameItemByFullPath);
            _SyncFromFilter();
            TrimHelper.InitFromRenameItems(sampleRenameItems ?? []);
            TrimHelper.SyncHighlightFromCount(ClampToInt(Count, 0, 9999));
        }

        /// <summary>
        /// Gets the Visual Trim Helper for this editor.
        /// </summary>
        public VisualTrimHelperViewModel TrimHelper { get; }

        /// <summary>
        /// Gets a tooltip describing what the count means for the selected filter.
        /// </summary>
        public string CountToolTip =>
            Step.Filter switch
            {
                TrimLeftFilter => "Removes this many characters from the start of the target.",
                TrimRightFilter => "Removes this many characters from the end of the target.",
                ExtractLeftFilter => "Keeps this many characters from the start; drops the rest.",
                ExtractRightFilter => "Keeps this many characters from the end; drops the rest.",
                _ => "How many characters this filter uses.",
            };

        /// <summary>
        /// Gets or sets the number of characters.
        /// </summary>
        [ObservableProperty]
        private decimal _count;

        partial void OnCountChanged(decimal value)
        {
            _ApplyOptions();
            if (!IsLoading)
            {
                TrimHelper.SyncHighlightFromCount(ClampToInt(Count, 0, 9999));
            }
        }

        private void _ConfigureTrimHelper(Func<string, RenameItem?>? resolveRenameItemByFullPath)
        {
            if (Step.Filter is not StringTargetFilter stringFilter)
            {
                return;
            }

            var mode = Step.Filter switch
            {
                TrimLeftFilter or ExtractLeftFilter => VisualTrimHelperMapping.Mode.LeftEdge,
                TrimRightFilter or ExtractRightFilter => VisualTrimHelperMapping.Mode.RightEdge,
                _ => VisualTrimHelperMapping.Mode.LeftEdge,
            };
            TrimHelper.Configure(mode, stringFilter.Target, resolveRenameItemByFullPath);
        }

        private void _OnTrimHelperSelectionApplied(object? sender, EventArgs e)
        {
            if (IsLoading || TrimHelper.AppliedCount is not { } appliedCount)
            {
                return;
            }

            Count = appliedCount;
        }

        private void _OnTrimHelperSampleChanged(object? sender, EventArgs e)
        {
            if (IsLoading)
            {
                return;
            }

            TrimHelper.SyncHighlightFromCount(ClampToInt(Count, 0, 9999));
        }

        private void _SyncFromFilter()
        {
            if (Step.Filter is not ICountOptionsFilter countFilter)
            {
                return;
            }

            LoadWithoutApplying(() => Count = countFilter.Options.Count);
        }

        private void _ApplyOptions()
        {
            if (IsLoading || Step.Filter is not ICountOptionsFilter countFilter)
            {
                return;
            }

            var options = new CountFilterOptions(Count: ClampToInt(Count, 0, 9999));
            ApplyIfChanged(Step.Filter, countFilter.WithOptions(options));
        }
    }
}

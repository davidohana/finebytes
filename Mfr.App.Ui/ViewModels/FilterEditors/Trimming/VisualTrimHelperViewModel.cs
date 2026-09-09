using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Filters.Trimming;
using Mfr.Models.Filters;
using Mfr.Models.Rename;

namespace Mfr.App.Ui.ViewModels.FilterEditors.Trimming
{
    /// <summary>
    /// Shared Visual Trim Helper state for Count and Trim Between editors (MFR7 parity).
    /// </summary>
    internal sealed partial class VisualTrimHelperViewModel : ObservableObject
    {
        /// <summary>
        /// Placeholder shown until a Rename List item or drop fills the sample.
        /// </summary>
        public const string PlaceholderText = "[ Drag Item Here ]";

        private VisualTrimHelperMapping.Mode _mode = VisualTrimHelperMapping.Mode.LeftEdge;
        private FilterTarget? _target;
        private Func<string, RenameItem?>? _resolveRenameItemByFullPath;
        private bool _isApplyingSelection;

        /// <summary>
        /// Raised when the user releases a selection that should update filter options.
        /// </summary>
        public event EventHandler? SelectionApplied;

        /// <summary>
        /// Raised when the TextBox selection should be updated to match options.
        /// </summary>
        public event EventHandler? HighlightChanged;

        /// <summary>
        /// Raised when the sample string changes (init or Rename List drop).
        /// </summary>
        public event EventHandler? SampleChanged;

        /// <summary>
        /// Gets the text shown in the helper box (sample or placeholder).
        /// </summary>
        [ObservableProperty]
        private string _displayText = PlaceholderText;

        /// <summary>
        /// Gets whether <see cref="DisplayText"/> is real sample text (not the placeholder).
        /// </summary>
        public bool HasSample => SampleText.Length > 0;

        /// <summary>
        /// Gets the underlying sample string (empty when showing the placeholder).
        /// </summary>
        public string SampleText { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the highlight start requested after options or selection sync.
        /// </summary>
        public int HighlightStart { get; private set; }

        /// <summary>
        /// Gets the highlight length requested after options or selection sync.
        /// </summary>
        public int HighlightLength { get; private set; }

        /// <summary>
        /// Gets the count derived from the last left/right selection apply, or <see langword="null"/> for range mode.
        /// </summary>
        public int? AppliedCount { get; private set; }

        /// <summary>
        /// Gets the left-anchored start from the last range selection apply, or <see langword="null"/>.
        /// </summary>
        public int? AppliedRangeStart { get; private set; }

        /// <summary>
        /// Gets the left-anchored end from the last range selection apply, or <see langword="null"/>.
        /// </summary>
        public int? AppliedRangeEnd { get; private set; }

        /// <summary>
        /// Configures mapping mode and the Apply Target used to resolve Rename List drops/init.
        /// </summary>
        /// <param name="mode">Left edge, right edge, or inclusive range.</param>
        /// <param name="target">Current filter apply target.</param>
        /// <param name="resolveRenameItemByFullPath">
        /// Looks up a Rename List row by original full path for drag-drop; optional.
        /// </param>
        public void Configure(
            VisualTrimHelperMapping.Mode mode,
            FilterTarget target,
            Func<string, RenameItem?>? resolveRenameItemByFullPath = null
        )
        {
            ArgumentNullException.ThrowIfNull(target);
            _mode = mode;
            _target = target;
            _resolveRenameItemByFullPath = resolveRenameItemByFullPath;
        }

        /// <summary>
        /// Fills the sample from the first Rename List item when present (MFR7 <c>OnTrimHelperInit</c>).
        /// </summary>
        /// <param name="items">Current Rename List engine items.</param>
        public void InitFromRenameItems(IReadOnlyList<RenameItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            if (items.Count == 0 || _target is null)
            {
                return;
            }

            if (FilterTargetText.TryGet(items[0], _target, out var text))
            {
                _SetSampleText(text);
            }
        }

        /// <summary>
        /// Replaces the sample with explicit text (e.g. text/file drop).
        /// </summary>
        /// <param name="text">Sample string; empty clears back to the placeholder.</param>
        public void SetSampleText(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            _SetSampleText(text);
        }

        /// <summary>
        /// Updates highlight from a left/right count spinner.
        /// </summary>
        /// <param name="count">Current filter count.</param>
        public void SyncHighlightFromCount(int count)
        {
            if (!HasSample)
            {
                return;
            }

            if (_mode == VisualTrimHelperMapping.Mode.LeftEdge)
            {
                VisualTrimHelperMapping.CountToLeftHighlight(count, SampleText.Length, out var start, out var length);
                _SetHighlight(start, length);
                return;
            }

            if (_mode == VisualTrimHelperMapping.Mode.RightEdge)
            {
                VisualTrimHelperMapping.CountToRightHighlight(count, SampleText.Length, out var start, out var length);
                _SetHighlight(start, length);
            }
        }

        /// <summary>
        /// Updates highlight from Trim Between positions.
        /// </summary>
        /// <param name="start">Inclusive start position.</param>
        /// <param name="end">Inclusive end position.</param>
        public void SyncHighlightFromRange(Position start, Position end)
        {
            ArgumentNullException.ThrowIfNull(start);
            ArgumentNullException.ThrowIfNull(end);
            if (!HasSample)
            {
                return;
            }

            if (
                !TrimBetweenFilter.TryGetSelectionRange(
                    SampleText,
                    start,
                    end,
                    out var highlightStart,
                    out var highlightLength
                )
            )
            {
                return;
            }

            _SetHighlight(highlightStart, highlightLength);
        }

        /// <summary>
        /// Applies a TextBox selection after pointer release (MFR7 MouseUp).
        /// </summary>
        /// <param name="selectionStart">Current selection start.</param>
        /// <param name="selectionLength">Current selection length.</param>
        /// <returns><see langword="true"/> when options should be updated from applied fields.</returns>
        public bool TryApplyPointerSelection(int selectionStart, int selectionLength)
        {
            if (!HasSample || _isApplyingSelection)
            {
                return false;
            }

            _isApplyingSelection = true;
            try
            {
                AppliedCount = null;
                AppliedRangeStart = null;
                AppliedRangeEnd = null;

                switch (_mode)
                {
                    case VisualTrimHelperMapping.Mode.LeftEdge:
                    {
                        var count = VisualTrimHelperMapping.SelectionToLeftCount(
                            selectionStart,
                            selectionLength,
                            out var highlightStart,
                            out var highlightLength
                        );
                        AppliedCount = count;
                        _SetHighlight(highlightStart, highlightLength);
                        SelectionApplied?.Invoke(this, EventArgs.Empty);
                        return true;
                    }
                    case VisualTrimHelperMapping.Mode.RightEdge:
                    {
                        var count = VisualTrimHelperMapping.SelectionToRightCount(
                            selectionStart,
                            SampleText.Length,
                            out var highlightStart,
                            out var highlightLength
                        );
                        AppliedCount = count;
                        _SetHighlight(highlightStart, highlightLength);
                        SelectionApplied?.Invoke(this, EventArgs.Empty);
                        return true;
                    }
                    case VisualTrimHelperMapping.Mode.Range:
                    {
                        if (
                            !VisualTrimHelperMapping.SelectionToLeftAnchoredRange(
                                selectionStart,
                                selectionLength,
                                out var startValue,
                                out var endValue
                            )
                        )
                        {
                            return false;
                        }

                        AppliedRangeStart = startValue;
                        AppliedRangeEnd = endValue;
                        _SetHighlight(selectionStart, selectionLength);
                        SelectionApplied?.Invoke(this, EventArgs.Empty);
                        return true;
                    }
                    default:
                        return false;
                }
            }
            finally
            {
                _isApplyingSelection = false;
            }
        }

        /// <summary>
        /// Tries to resolve a Rename List drag into sample text for the current Apply Target.
        /// </summary>
        /// <param name="fullPathFromRenameList">Original full path from Rename List drag payload.</param>
        /// <param name="resolved">Sample text when the path resolves to a list row.</param>
        /// <returns><see langword="true"/> when a sample string was produced.</returns>
        public bool TryResolveRenameListDrop(string fullPathFromRenameList, out string resolved)
        {
            if (string.IsNullOrEmpty(fullPathFromRenameList) || _target is null)
            {
                resolved = string.Empty;
                return false;
            }

            var item = _resolveRenameItemByFullPath?.Invoke(fullPathFromRenameList);
            if (item is not null && FilterTargetText.TryGet(item, _target, out resolved))
            {
                return true;
            }

            resolved = string.Empty;
            return false;
        }

        private void _SetSampleText(string text)
        {
            if (SampleText == text)
            {
                return;
            }

            SampleText = text;
            DisplayText = text.Length == 0 ? PlaceholderText : text;
            SampleChanged?.Invoke(this, EventArgs.Empty);
        }

        private void _SetHighlight(int start, int length)
        {
            HighlightStart = start;
            HighlightLength = length;
            HighlightChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

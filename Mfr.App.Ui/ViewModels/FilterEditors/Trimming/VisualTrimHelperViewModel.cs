using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Filters.Trimming;
using Mfr.Models.Filters;
using Mfr.Models.Rename;
using Mfr.Utils;

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
        private Func<IReadOnlyList<RenameItem>>? _resolveRenameItems;
        private IReadOnlyList<RenameItem> _renameItems = [];
        private int _itemIndex;
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
        /// Raised when the sample string changes (init, navigation, or Rename List drop).
        /// </summary>
        public event EventHandler? SampleChanged;

        /// <summary>
        /// Gets the text shown in the helper box (sample or placeholder).
        /// </summary>
        [ObservableProperty]
        private string _displayText = PlaceholderText;

        /// <summary>
        /// Gets the 1-based item index label, or empty when the Rename List is empty.
        /// </summary>
        [ObservableProperty]
        private string _itemIndexLabel = string.Empty;

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
        /// Gets whether Previous is enabled.
        /// </summary>
        public bool CanGoPrevious => _renameItems.Count > 0 && _itemIndex > 0;

        /// <summary>
        /// Gets whether Next is enabled.
        /// </summary>
        public bool CanGoNext => _renameItems.Count > 0 && _itemIndex < _renameItems.Count - 1;

        /// <summary>
        /// Configures mapping mode and Rename List resolvers used for init, navigation, and drops.
        /// </summary>
        /// <param name="mode">Left edge, right edge, or inclusive range.</param>
        /// <param name="target">Current filter apply target.</param>
        /// <param name="resolveRenameItemByFullPath">
        /// Looks up a Rename List row by original full path for drag-drop; optional.
        /// </param>
        /// <param name="resolveRenameItems">
        /// Returns the current Rename List engine items for init/navigation; optional.
        /// </param>
        public void Configure(
            VisualTrimHelperMapping.Mode mode,
            FilterTarget target,
            Func<string, RenameItem?>? resolveRenameItemByFullPath = null,
            Func<IReadOnlyList<RenameItem>>? resolveRenameItems = null
        )
        {
            ArgumentNullException.ThrowIfNull(target);
            _mode = mode;
            _target = target;
            _resolveRenameItemByFullPath = resolveRenameItemByFullPath;
            _resolveRenameItems = resolveRenameItems;
        }

        /// <summary>
        /// Fills the sample from the first Rename List item when present (MFR7 <c>OnTrimHelperInit</c>).
        /// </summary>
        /// <param name="items">
        /// Optional explicit snapshot; when null, uses the resolver from <see cref="Configure"/>.
        /// </param>
        public void InitFromRenameItems(IReadOnlyList<RenameItem>? items = null)
        {
            _ReloadRenameItems(items);
            _itemIndex = 0;
            if (_renameItems.Count == 0 || _target is null)
            {
                ItemIndexLabel = string.Empty;
                _NotifyNavigationChanged();
                return;
            }

            _ApplyCurrentItem();
        }

        /// <summary>
        /// Reloads Rename List items from the configured resolver and updates navigation chrome.
        /// <para>
        /// When the helper still shows the placeholder and items are now available, selects the first item.
        /// Otherwise keeps the current sample and rebinds the index when possible.
        /// </para>
        /// </summary>
        public void RefreshRenameItems()
        {
            _ReloadRenameItems();
            if (_renameItems.Count == 0 || _target is null)
            {
                _itemIndex = 0;
                ItemIndexLabel = string.Empty;
                _NotifyNavigationChanged();
                return;
            }

            if (!HasSample)
            {
                _itemIndex = 0;
                _ApplyCurrentItem();
                return;
            }

            var matchedIndex = _FindItemIndexBySampleText(SampleText);
            _itemIndex = matchedIndex >= 0 ? matchedIndex : Math.Clamp(_itemIndex, 0, _renameItems.Count - 1);
            ItemIndexLabel = (_itemIndex + 1).ToString(CultureInfo.InvariantCulture);
            _NotifyNavigationChanged();
        }

        /// <summary>
        /// Replaces the sample with explicit text (e.g. tests or a drop not in the list).
        /// </summary>
        /// <param name="text">Sample string; empty clears back to the placeholder.</param>
        public void SetSampleText(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            _SetSampleText(text);
        }

        /// <summary>
        /// Moves to the previous Rename List item when available.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoPrevious))]
        public void GoPrevious()
        {
            _ReloadRenameItems();
            if (!CanGoPrevious)
            {
                _NotifyNavigationChanged();
                return;
            }

            _itemIndex--;
            _ApplyCurrentItem();
        }

        /// <summary>
        /// Moves to the next Rename List item when available.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoNext))]
        public void GoNext()
        {
            _ReloadRenameItems();
            if (!CanGoNext)
            {
                _NotifyNavigationChanged();
                return;
            }

            _itemIndex++;
            _ApplyCurrentItem();
        }

        /// <summary>
        /// Updates highlight from a left/right count spinner.
        /// </summary>
        /// <param name="count">Current filter count.</param>
        public void SyncHighlightFromCount(int count)
        {
            // MFR7 updates selection on the TextBox text, including the placeholder.
            var textLength = DisplayText.Length;
            if (_mode == VisualTrimHelperMapping.Mode.LeftEdge)
            {
                VisualTrimHelperMapping.CountToLeftHighlight(count, textLength, out var start, out var length);
                _SetHighlight(start, length);
                return;
            }

            if (_mode == VisualTrimHelperMapping.Mode.RightEdge)
            {
                VisualTrimHelperMapping.CountToRightHighlight(count, textLength, out var start, out var length);
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

            // MFR7 updates selection on the TextBox text, including the placeholder.
            if (
                !TrimBetweenFilter.TryGetSelectionRange(
                    DisplayText,
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
            // MFR7 maps selection against whatever is in the box, including "[ Drag Item Here ]".
            if (_isApplyingSelection)
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
                            DisplayText.Length,
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
                            !TrimBetweenFilter.TryGetPositionsFromSelection(
                                selectionStart,
                                selectionLength,
                                out var start,
                                out var end
                            )
                        )
                        {
                            return false;
                        }

                        AppliedRangeStart = start.Value;
                        AppliedRangeEnd = end.Value;
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

        /// <summary>
        /// Applies a Rename List drop: syncs the navigator index when the path is in the list.
        /// </summary>
        /// <param name="fullPathFromRenameList">Original full path from Rename List drag payload.</param>
        /// <returns><see langword="true"/> when the sample was updated.</returns>
        public bool TryApplyRenameListDrop(string fullPathFromRenameList)
        {
            if (!TryResolveRenameListDrop(fullPathFromRenameList, out var resolved))
            {
                return false;
            }

            _ReloadRenameItems();
            var index = _FindItemIndex(fullPathFromRenameList);
            if (index >= 0)
            {
                _itemIndex = index;
                _ApplyCurrentItem();
                return true;
            }

            _SetSampleText(resolved);
            ItemIndexLabel = string.Empty;
            _NotifyNavigationChanged();
            return true;
        }

        private void _ReloadRenameItems(IReadOnlyList<RenameItem>? items = null)
        {
            if (items is not null)
            {
                _renameItems = items;
                return;
            }

            _renameItems = _resolveRenameItems?.Invoke() ?? [];
        }

        private void _ApplyCurrentItem()
        {
            if (_renameItems.Count == 0 || _target is null)
            {
                ItemIndexLabel = string.Empty;
                _NotifyNavigationChanged();
                return;
            }

            var item = _renameItems[_itemIndex];
            if (FilterTargetText.TryGet(item, _target, out var text))
            {
                _SetSampleText(text, forceNotify: true);
            }

            ItemIndexLabel = (_itemIndex + 1).ToString(CultureInfo.InvariantCulture);
            _NotifyNavigationChanged();
        }

        private int _FindItemIndex(string fullPath)
        {
            for (var i = 0; i < _renameItems.Count; i++)
            {
                if (PathComparers.Os.Equals(_renameItems[i].Original.FullPath, fullPath))
                {
                    return i;
                }
            }

            return -1;
        }

        private int _FindItemIndexBySampleText(string sampleText)
        {
            if (_target is null)
            {
                return -1;
            }

            for (var i = 0; i < _renameItems.Count; i++)
            {
                if (
                    FilterTargetText.TryGet(_renameItems[i], _target, out var text)
                    && string.Equals(text, sampleText, StringComparison.Ordinal)
                )
                {
                    return i;
                }
            }

            return -1;
        }

        private void _SetSampleText(string text, bool forceNotify = false)
        {
            if (!forceNotify && SampleText == text)
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

        private void _NotifyNavigationChanged()
        {
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
            GoPreviousCommand.NotifyCanExecuteChanged();
            GoNextCommand.NotifyCanExecuteChanged();
        }
    }
}

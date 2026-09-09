using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mfr.App.Ui.ViewModels.FilterEditors.Trimming;
using Mfr.App.Ui.Views.RenameList;

namespace Mfr.App.Ui.Views.Controls
{
    /// <summary>
    /// Read-only sample text box that maps mouse selection to trim/extract options (MFR7 Visual Trim Helper).
    /// </summary>
    public partial class VisualTrimHelperView : UserControl
    {
        private VisualTrimHelperViewModel? _helper;
        private bool _isApplyingHighlight;
        private bool _highlightApplyQueued;

        /// <summary>
        /// Initializes the control and wires selection / drop handlers.
        /// </summary>
        public VisualTrimHelperView()
        {
            InitializeComponent();
            DataContextChanged += _OnDataContextChanged;
            AttachedToVisualTree += (_, _) => _QueueApplyHighlight();
            TrimHelperText.AddHandler(PointerReleasedEvent, _OnPointerReleased, RoutingStrategies.Tunnel);
            DragDrop.SetAllowDrop(TrimHelperText, true);
            TrimHelperText.AddHandler(DragDrop.DragOverEvent, _OnDragOver);
            TrimHelperText.AddHandler(DragDrop.DropEvent, _OnDrop);
        }

        private void _OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_helper is not null)
            {
                _helper.HighlightChanged -= _OnHighlightChanged;
                _helper.PropertyChanged -= _OnHelperPropertyChanged;
            }

            _helper = DataContext as VisualTrimHelperViewModel;
            if (_helper is not null)
            {
                _helper.HighlightChanged += _OnHighlightChanged;
                _helper.PropertyChanged += _OnHelperPropertyChanged;
                _QueueApplyHighlight();
            }
        }

        private void _OnHelperPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (
                e.PropertyName
                is nameof(VisualTrimHelperViewModel.DisplayText)
                    or nameof(VisualTrimHelperViewModel.SampleText)
            )
            {
                _QueueApplyHighlight();
            }
        }

        private void _OnHighlightChanged(object? sender, EventArgs e)
        {
            _QueueApplyHighlight();
        }

        private void _QueueApplyHighlight()
        {
            if (_highlightApplyQueued)
            {
                return;
            }

            _highlightApplyQueued = true;
            Dispatcher.UIThread.Post(
                () =>
                {
                    _highlightApplyQueued = false;
                    _ApplyHighlight();
                },
                DispatcherPriority.Loaded
            );
        }

        private void _ApplyHighlight()
        {
            if (_helper is null || !_helper.HasSample || _isApplyingHighlight)
            {
                return;
            }

            _isApplyingHighlight = true;
            try
            {
                // Binding may not have flushed yet on first show; sync text before selection.
                if (TrimHelperText.Text != _helper.DisplayText)
                {
                    TrimHelperText.Text = _helper.DisplayText;
                }

                var textLength = TrimHelperText.Text?.Length ?? 0;
                var start = Math.Clamp(_helper.HighlightStart, 0, textLength);
                var maxLength = Math.Max(0, textLength - start);
                var length = Math.Clamp(_helper.HighlightLength, 0, maxLength);
                TrimHelperText.SelectionStart = start;
                TrimHelperText.SelectionEnd = start + length;
            }
            finally
            {
                _isApplyingHighlight = false;
            }
        }

        private void _OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_helper is null || !_helper.HasSample || _isApplyingHighlight)
            {
                return;
            }

            var start = Math.Min(TrimHelperText.SelectionStart, TrimHelperText.SelectionEnd);
            var end = Math.Max(TrimHelperText.SelectionStart, TrimHelperText.SelectionEnd);
            if (_helper.TryApplyPointerSelection(start, end - start))
            {
                _QueueApplyHighlight();
            }
        }

        private void _OnDragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = _CanAcceptDrop(e) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void _OnDrop(object? sender, DragEventArgs e)
        {
            if (_helper is null)
            {
                return;
            }

            var payload = RenameListSampleDragPayload.TryRead(e.DataTransfer);
            if (payload is null)
            {
                return;
            }

            if (_helper.TryResolveRenameListDrop(payload.FullPath, out var resolved))
            {
                _helper.SetSampleText(resolved);
                e.Handled = true;
            }
        }

        private static bool _CanAcceptDrop(DragEventArgs e)
        {
            return e.DataTransfer is not null
                && e.DataTransfer.Formats.Contains(RenameListSampleDragPayload.Format)
                && RenameListSampleDragPayload.TryRead(e.DataTransfer) is not null;
        }
    }
}

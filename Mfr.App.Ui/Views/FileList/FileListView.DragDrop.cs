using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mfr.App.Ui.Views.RenameList;

namespace Mfr.App.Ui.Views.FileList
{
    public partial class FileListView
    {
        /// <summary>
        /// Removes selected Rename List rows when dropped onto the File List, set by the main window shell.
        /// </summary>
        public static readonly StyledProperty<ICommand?> RemoveSelectedFromRenameListCommandProperty =
            AvaloniaProperty.Register<FileListView, ICommand?>(nameof(RemoveSelectedFromRenameListCommand));

        /// <summary>
        /// Gets or sets the command invoked when Rename List rows are dropped onto the File List.
        /// </summary>
        public ICommand? RemoveSelectedFromRenameListCommand
        {
            get => GetValue(RemoveSelectedFromRenameListCommandProperty);
            set => SetValue(RemoveSelectedFromRenameListCommandProperty, value);
        }

        private void _WireRenameListDragBackDrop()
        {
            DragDrop.SetAllowDrop(this, true);
            var routing = RoutingStrategies.Tunnel | RoutingStrategies.Bubble;
            AddHandler(DragDrop.DragOverEvent, _OnRenameListDragOver, routing);
            AddHandler(DragDrop.DropEvent, _OnRenameListDrop, routing);
        }

        private void _OnRenameListDragOver(object? sender, DragEventArgs e)
        {
            if (e.DataTransfer?.Formats.Contains(RenameListView.InternalReorderFormat) != true)
            {
                return;
            }

            var canAccept = _CanAcceptRenameListDragBack(e);
            e.Handled = canAccept;
            e.DragEffects = canAccept ? DragDropEffects.Move : DragDropEffects.None;
        }

        private void _OnRenameListDrop(object? sender, DragEventArgs e)
        {
            if (!_CanAcceptRenameListDragBack(e))
            {
                return;
            }

            e.Handled = true;
            var command = RemoveSelectedFromRenameListCommand;
            if (command is null)
            {
                return;
            }

            // Pointer release can select the row under the cursor; clear File List selection afterward.
            _isRenameListDragBackDrop = true;
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    command.Execute(null);
                    _viewModel?.SetSelectedEntries([]);
                    _SyncSelectionToActiveListing(force: true);
                }
                finally
                {
                    _isRenameListDragBackDrop = false;
                }
            });
        }

        private bool _CanAcceptRenameListDragBack(DragEventArgs e)
        {
            var isRenameListDrag = e.DataTransfer?.Formats.Contains(RenameListView.InternalReorderFormat) == true;
            if (!isRenameListDrag)
            {
                return false;
            }

            var command = RemoveSelectedFromRenameListCommand;
            return command is not null && command.CanExecute(null);
        }
    }
}

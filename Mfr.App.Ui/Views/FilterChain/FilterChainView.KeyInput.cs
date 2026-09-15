using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Mfr.App.Ui.Input;

namespace Mfr.App.Ui.Views.FilterChain
{
    public partial class FilterChainView
    {
        private void _WireKeyHandlers()
        {
            FilterChainList.AddHandler(KeyDownEvent, _OnListKeyDown, RoutingStrategies.Tunnel);
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_TryHandleFilterChainShortcut(e))
            {
                return;
            }

            base.OnKeyDown(e);
        }

        private void _OnListKeyDown(object? sender, KeyEventArgs e)
        {
            _ = _TryHandleFilterChainShortcut(e, fromList: true);
        }

        private bool _TryHandleFilterChainShortcut(KeyEventArgs e, bool fromList = false)
        {
            if (_viewModel is null || e.Handled)
            {
                return false;
            }

            var shouldHandle = fromList || _IsFilterChainFocused() || _IsEventFromFilterChainList(e);
            if (!shouldHandle)
            {
                return false;
            }

            if (
                KeyGestureMatch.TryExecute(e, AppShortcuts.RemoveSelectedFilterDelete, _viewModel.RemoveSelectedCommand)
            )
            {
                return true;
            }

            if (KeyGestureMatch.TryExecute(e, AppShortcuts.MoveFilterUp, _viewModel.MoveSelectedUpCommand))
            {
                return true;
            }

            if (KeyGestureMatch.TryExecute(e, AppShortcuts.MoveFilterDown, _viewModel.MoveSelectedDownCommand))
            {
                return true;
            }

            return false;
        }

        private bool _IsFilterChainFocused()
        {
            var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
            if (focused is null)
            {
                return false;
            }

            if (ReferenceEquals(focused, FilterChainList))
            {
                return true;
            }

            return focused is Visual visual && visual.GetVisualAncestors().Contains(FilterChainList);
        }

        private bool _IsEventFromFilterChainList(KeyEventArgs e)
        {
            return e.Source is Visual source && source.GetVisualAncestors().Contains(FilterChainList);
        }
    }
}

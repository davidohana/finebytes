using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Mfr.App.Ui.Input;

namespace Mfr.App.Ui.Views.AppliedFilters
{
    public partial class AppliedFiltersView
    {
        private void _WireKeyHandlers()
        {
            AppliedFiltersList.AddHandler(KeyDownEvent, _OnListKeyDown, RoutingStrategies.Tunnel);
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_TryHandleAppliedFiltersShortcut(e))
            {
                return;
            }

            base.OnKeyDown(e);
        }

        private void _OnListKeyDown(object? sender, KeyEventArgs e)
        {
            _ = _TryHandleAppliedFiltersShortcut(e, fromList: true);
        }

        private bool _TryHandleAppliedFiltersShortcut(KeyEventArgs e, bool fromList = false)
        {
            if (_viewModel is null || e.Handled)
            {
                return false;
            }

            var shouldHandle = fromList || _IsAppliedFiltersFocused() || _IsEventFromAppliedList(e);
            if (!shouldHandle)
            {
                return false;
            }

            if (_MatchesGesture(e, AppShortcuts.RemoveSelectedFilterDelete))
            {
                if (_viewModel.RemoveSelectedCommand.CanExecute(null))
                {
                    _viewModel.RemoveSelectedCommand.Execute(null);
                    e.Handled = true;
                    return true;
                }
            }

            if (_MatchesGesture(e, AppShortcuts.MoveFilterUp))
            {
                if (_viewModel.MoveSelectedUpCommand.CanExecute(null))
                {
                    _viewModel.MoveSelectedUpCommand.Execute(null);
                    e.Handled = true;
                    return true;
                }
            }

            if (_MatchesGesture(e, AppShortcuts.MoveFilterDown))
            {
                if (_viewModel.MoveSelectedDownCommand.CanExecute(null))
                {
                    _viewModel.MoveSelectedDownCommand.Execute(null);
                    e.Handled = true;
                    return true;
                }
            }

            return false;
        }

        private bool _IsAppliedFiltersFocused()
        {
            var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
            if (focused is null)
            {
                return false;
            }

            if (ReferenceEquals(focused, AppliedFiltersList))
            {
                return true;
            }

            return focused is Visual visual && visual.GetVisualAncestors().Contains(AppliedFiltersList);
        }

        private bool _IsEventFromAppliedList(KeyEventArgs e)
        {
            return e.Source is Visual source && source.GetVisualAncestors().Contains(AppliedFiltersList);
        }

        private static bool _MatchesGesture(KeyEventArgs e, KeyGesture gesture)
        {
            return e.Key == gesture.Key && e.KeyModifiers == gesture.KeyModifiers;
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Mfr.App.Ui.ViewModels.FormatEditor;

namespace Mfr.App.Ui.Views.FormatEditor
{
    /// <summary>
    /// Searchable format-token catalog (grouped tree or flat filter results).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Expects a <see cref="FormatTokenInsertPickerViewModel"/> as <see cref="StyledElement.DataContext"/>.
    /// Used by the per-field Insert flyout and by <see cref="FormatTokenToolsHost"/>.
    /// </para>
    /// </remarks>
    public partial class FormatTokenInsertPicker : UserControl
    {
        /// <summary>
        /// Initializes insert-picker interaction handlers.
        /// </summary>
        public FormatTokenInsertPicker()
        {
            InitializeComponent();
            InsertList.AddHandler(TappedEvent, _OnInsertItemTapped, RoutingStrategies.Bubble);
            InsertList.AddHandler(TreeViewItem.ExpandedEvent, _OnInsertGroupExpanded);
            InsertList.AddHandler(KeyDownEvent, _OnInsertPickerKeyDown, RoutingStrategies.Tunnel);
            InsertSearchBox.AddHandler(KeyDownEvent, _OnInsertPickerKeyDown, RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Focuses the search box (e.g. when a host expands or a flyout opens).
        /// </summary>
        public void FocusSearch()
        {
            InsertSearchBox.Focus();
        }

        /// <summary>
        /// Inserts the tapped catalog leaf, or expands/collapses a group folder (pointer/touch).
        /// Keyboard highlight alone must not insert.
        /// </summary>
        private void _OnInsertItemTapped(object? sender, TappedEventArgs e)
        {
            if (e.Source is not Visual source)
            {
                return;
            }

            var item = source as TreeViewItem ?? source.FindAncestorOfType<TreeViewItem>();
            if (item?.DataContext is not FormatInsertPickerNode node)
            {
                return;
            }

            if (node.IsGroup)
            {
                if (!_OriginatedFromExpandChevron(source, item))
                {
                    item.IsExpanded = !item.IsExpanded;
                    e.Handled = true;
                }

                return;
            }

            if (node.Entry is not { } entry)
            {
                return;
            }

            e.Handled = true;
            if (DataContext is FormatTokenInsertPickerViewModel vm)
            {
                vm.InsertEntryCommand.Execute(entry);
            }
        }

        /// <summary>
        /// True when the tap started on this item's expand/collapse chevron (already toggles
        /// <see cref="TreeViewItem.IsExpanded"/>).
        /// </summary>
        private static bool _OriginatedFromExpandChevron(Visual source, TreeViewItem item)
        {
            var current = source;
            while (current is not null && !ReferenceEquals(current, item))
            {
                if (current is ToggleButton)
                {
                    return true;
                }

                current = current.GetVisualParent();
            }

            return false;
        }

        /// <summary>
        /// Accordion: opening a folder collapses sibling folders (tap, chevron, or keyboard).
        /// </summary>
        private void _OnInsertGroupExpanded(object? sender, RoutedEventArgs e)
        {
            if (e.Source is not TreeViewItem expanded)
            {
                return;
            }

            _CollapseSiblingGroups(expanded);
        }

        /// <summary>
        /// Collapses other expanded folders that share <paramref name="expanded"/>'s parent.
        /// </summary>
        private static void _CollapseSiblingGroups(TreeViewItem expanded)
        {
            var parent = ItemsControl.ItemsControlFromItemContainer(expanded);
            if (parent is null)
            {
                return;
            }

            for (var i = 0; i < parent.ItemCount; i++)
            {
                if (parent.ContainerFromIndex(i) is not TreeViewItem sibling)
                {
                    continue;
                }

                if (ReferenceEquals(sibling, expanded) || !sibling.IsExpanded)
                {
                    continue;
                }

                sibling.IsExpanded = false;
            }
        }

        /// <summary>
        /// Enter inserts the highlighted catalog leaf from search or list focus (not SelectionChanged).
        /// </summary>
        private void _OnInsertPickerKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || !_TryInsertHighlighted())
            {
                return;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Inserts <see cref="TreeView.SelectedItem"/> when it is a catalog leaf.
        /// </summary>
        /// <returns><see langword="true"/> when a leaf was inserted.</returns>
        private bool _TryInsertHighlighted()
        {
            if (
                InsertList.SelectedItem is not FormatInsertPickerNode { Entry: { } entry }
                || DataContext is not FormatTokenInsertPickerViewModel vm
            )
            {
                return false;
            }

            vm.InsertEntryCommand.Execute(entry);
            return true;
        }
    }
}

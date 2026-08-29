using Mfr.App.Ui.ViewModels.RenameList;

namespace Mfr.Tests.Ui.RenameList
{
    /// <summary>
    /// Tests <see cref="OrderedDraft{TKey,TItem}"/> selection clamp and list mutations.
    /// </summary>
    public sealed class OrderedDraftTests
    {
        [Fact]
        public void TryAdd_skips_duplicate_keys_and_selects_last()
        {
            var draft = new OrderedDraft<string, string>(["a"], item => item);

            Assert.True(draft.TryAdd("b"));
            Assert.Equal(1, draft.SelectedIndex);
            Assert.Equal(["a", "b"], draft.Items);

            Assert.False(draft.TryAdd("a"));
            Assert.Equal(1, draft.SelectedIndex);
            Assert.Equal(["a", "b"], draft.Items);
        }

        [Fact]
        public void TryRemoveAtIndices_clamps_selection_index_for_a_single_row()
        {
            var draft = new OrderedDraft<string, string>(["a", "b", "c"], item => item) { SelectedIndex = 2 };

            Assert.Equal(1, draft.TryRemoveAtIndices([2]));
            Assert.Equal(["a", "b"], draft.Items);
            Assert.Equal(1, draft.SelectedIndex);

            draft.SelectedIndex = 1;
            Assert.Equal(1, draft.TryRemoveAtIndices([1]));
            Assert.Equal(["a"], draft.Items);
            Assert.Equal(0, draft.SelectedIndex);

            Assert.Equal(1, draft.TryRemoveAtIndices([0]));
            Assert.Empty(draft.Items);
            Assert.Equal(-1, draft.SelectedIndex);
        }

        [Fact]
        public void TryMoveBlock_respects_single_item_bounds()
        {
            var draft = new OrderedDraft<string, string>(["a", "b", "c"], item => item) { SelectedIndex = 0 };

            Assert.False(draft.TryMoveBlock([0], -1));
            Assert.Equal(["a", "b", "c"], draft.Items);
            Assert.Equal(0, draft.SelectedIndex);

            draft.SelectedIndex = 1;
            Assert.True(draft.TryMoveBlock([1], -1));
            Assert.Equal(["b", "a", "c"], draft.Items);
            Assert.Equal(0, draft.SelectedIndex);
            Assert.False(draft.TryMoveBlock([0], -1));

            draft.SelectedIndex = 2;
            Assert.False(draft.TryMoveBlock([2], 1));
            Assert.True(draft.TryMoveBlock([2], -1));
            Assert.Equal(["b", "c", "a"], draft.Items);
            Assert.Equal(1, draft.SelectedIndex);
        }

        [Fact]
        public void Clear_resets_items_and_selection()
        {
            var draft = new OrderedDraft<string, string>(["a", "b"], item => item) { SelectedIndex = 0 };

            draft.Clear();

            Assert.Empty(draft.Items);
            Assert.Equal(-1, draft.SelectedIndex);
            Assert.False(draft.HasItems);
            Assert.False(draft.CanRemove);
        }

        [Fact]
        public void CanRemove_follows_selection()
        {
            var draft = new OrderedDraft<string, string>(["a", "b"], item => item) { SelectedIndex = 0 };

            Assert.True(draft.CanRemove);

            draft.SelectedIndex = -1;
            Assert.False(draft.CanRemove);
        }

        [Fact]
        public void GetInsertIndexBelow_uses_last_selected_index_or_end()
        {
            var draft = new OrderedDraft<string, string>(["a", "b", "c"], item => item);

            Assert.Equal(3, draft.GetInsertIndexBelow([]));

            Assert.Equal(1, draft.GetInsertIndexBelow([0]));
            Assert.Equal(3, draft.GetInsertIndexBelow([0, 2]));
            Assert.Equal(3, draft.GetInsertIndexBelow([2]));
        }

        [Fact]
        public void CanMoveBlock_matches_independent_swap_rules()
        {
            var draft = new OrderedDraft<string, string>(["a", "b", "c"], item => item);

            Assert.False(draft.CanMoveBlock([0], -1));
            Assert.True(draft.CanMoveBlock([0], 1));
            Assert.False(draft.CanMoveBlock([0, 1], -1));
            Assert.True(draft.CanMoveBlock([0, 2], -1));
            Assert.False(draft.CanMoveBlock([], 1));
        }

        [Fact]
        public void TryInsertAt_inserts_at_index_and_skips_duplicates()
        {
            var draft = new OrderedDraft<string, string>(["a", "c"], item => item) { SelectedIndex = 0 };

            Assert.True(draft.TryInsertAt(1, "b"));
            Assert.Equal(["a", "b", "c"], draft.Items);
            Assert.Equal(1, draft.SelectedIndex);

            Assert.False(draft.TryInsertAt(0, "a"));
            Assert.Equal(["a", "b", "c"], draft.Items);
        }

        [Fact]
        public void TryInsertMany_inserts_in_order_and_skips_duplicates()
        {
            var draft = new OrderedDraft<string, string>(["a"], item => item) { SelectedIndex = 0 };

            var insertedCount = draft.TryInsertMany(1, ["b", "a", "c"]);

            Assert.Equal(2, insertedCount);
            Assert.Equal(["a", "b", "c"], draft.Items);
            Assert.Equal(2, draft.SelectedIndex);
        }

        [Fact]
        public void TryRemoveAtIndices_removes_multiple_rows_and_clamps_selection()
        {
            var draft = new OrderedDraft<string, string>(["a", "b", "c", "d"], item => item) { SelectedIndex = 2 };

            Assert.Equal(2, draft.TryRemoveAtIndices([1, 3]));

            Assert.Equal(["a", "c"], draft.Items);
            Assert.Equal(1, draft.SelectedIndex);
        }

        [Fact]
        public void TryMoveBlock_moves_contiguous_selection()
        {
            var draft = new OrderedDraft<string, string>(["a", "b", "c", "d"], item => item) { SelectedIndex = 1 };

            Assert.True(draft.TryMoveBlock([1, 2], 1));
            Assert.Equal(["a", "d", "b", "c"], draft.Items);
            Assert.Equal(2, draft.SelectedIndex);

            Assert.True(draft.TryMoveBlock([2, 3], -1));
            Assert.Equal(["a", "b", "c", "d"], draft.Items);
            Assert.Equal(1, draft.SelectedIndex);
        }

        [Fact]
        public void TryMoveBlock_moves_non_contiguous_items_independently()
        {
            var draft = new OrderedDraft<string, string>(["a", "b", "c"], item => item) { SelectedIndex = 2 };

            Assert.True(draft.TryMoveBlock([0, 2], -1, out var newIndices));

            Assert.Equal(["a", "c", "b"], draft.Items);
            Assert.Equal([0, 1], newIndices);
            Assert.Equal(1, draft.SelectedIndex);
        }

        [Fact]
        public void TryMoveIndicesTo_reorders_block_to_target()
        {
            var draft = new OrderedDraft<string, string>(["a", "b", "c", "d"], item => item) { SelectedIndex = 1 };

            Assert.True(draft.TryMoveIndicesTo([1, 2], 4, out var newIndices));
            Assert.Equal(["a", "d", "b", "c"], draft.Items);
            Assert.Equal([2, 3], newIndices);
            Assert.Equal(2, draft.SelectedIndex);
        }

        [Fact]
        public void TrySetItem_replaces_when_key_matches()
        {
            var draft = new OrderedDraft<string, (string Key, int Value)>([("a", 1), ("b", 2)], item => item.Key)
            {
                SelectedIndex = 0,
            };

            Assert.True(draft.TrySetItem(0, ("a", 9)));
            Assert.Equal([("a", 9), ("b", 2)], draft.Items);
            Assert.False(draft.TrySetItem(0, ("c", 9)));
            Assert.False(draft.TrySetItem(9, ("a", 9)));
        }
    }
}

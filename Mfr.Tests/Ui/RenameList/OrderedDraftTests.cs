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
        public void TryRemoveSelected_clamps_selection_index()
        {
            var draft = new OrderedDraft<string, string>(["a", "b", "c"], item => item) { SelectedIndex = 2 };

            Assert.True(draft.TryRemoveSelected());
            Assert.Equal(["a", "b"], draft.Items);
            Assert.Equal(1, draft.SelectedIndex);

            draft.SelectedIndex = 1;
            Assert.True(draft.TryRemoveSelected());
            Assert.Equal(["a"], draft.Items);
            Assert.Equal(0, draft.SelectedIndex);

            Assert.True(draft.TryRemoveSelected());
            Assert.Empty(draft.Items);
            Assert.Equal(-1, draft.SelectedIndex);
        }

        [Fact]
        public void TryMoveSelected_respects_bounds()
        {
            var draft = new OrderedDraft<string, string>(["a", "b", "c"], item => item) { SelectedIndex = 0 };

            Assert.False(draft.TryMoveSelected(-1));
            Assert.Equal(["a", "b", "c"], draft.Items);
            Assert.Equal(0, draft.SelectedIndex);

            draft.SelectedIndex = 1;
            Assert.True(draft.TryMoveSelected(-1));
            Assert.Equal(["b", "a", "c"], draft.Items);
            Assert.Equal(0, draft.SelectedIndex);
            Assert.False(draft.TryMoveSelected(-1));

            draft.SelectedIndex = 2;
            Assert.False(draft.TryMoveSelected(1));
            Assert.True(draft.TryMoveSelected(-1));
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
        public void CanExecute_flags_follow_selection()
        {
            var draft = new OrderedDraft<string, string>(["a", "b"], item => item) { SelectedIndex = 0 };

            Assert.True(draft.CanRemove);
            Assert.False(draft.CanMoveUp);
            Assert.True(draft.CanMoveDown);

            draft.SelectedIndex = 1;
            Assert.True(draft.CanMoveUp);
            Assert.False(draft.CanMoveDown);

            draft.SelectedIndex = -1;
            Assert.False(draft.CanRemove);
            Assert.False(draft.CanMoveUp);
            Assert.False(draft.CanMoveDown);
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

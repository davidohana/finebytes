using System.Collections.Specialized;
using Mfr.App.Ui.Collections;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests bulk add/insert/replace notification behavior of <see cref="RangeObservableCollection{T}"/>.
    /// </summary>
    public sealed class RangeObservableCollectionTests
    {
        /// <summary>
        /// Verifies <see cref="RangeObservableCollection{T}.AddRange"/> appends items and raises one Reset.
        /// </summary>
        [Fact]
        public void AddRange_Appends_With_Single_Reset()
        {
            var collection = new RangeObservableCollection<string> { "existing" };
            var notifications = new List<NotifyCollectionChangedAction>();
            collection.CollectionChanged += (_, e) => notifications.Add(e.Action);

            collection.AddRange(["a", "b", "c"]);

            Assert.Equal(["existing", "a", "b", "c"], collection);
            Assert.Equal(4, collection.Count);
            Assert.Equal([NotifyCollectionChangedAction.Reset], notifications);
        }

        /// <summary>
        /// Verifies an empty batch does not raise <see cref="INotifyCollectionChanged.CollectionChanged"/>.
        /// </summary>
        [Fact]
        public void AddRange_Empty_Does_Not_Notify()
        {
            var collection = new RangeObservableCollection<int>();
            var notified = false;
            collection.CollectionChanged += (_, _) => notified = true;

            collection.AddRange([]);

            Assert.Empty(collection);
            Assert.False(notified);
        }

        /// <summary>
        /// Verifies a null batch throws.
        /// </summary>
        [Fact]
        public void AddRange_Null_Throws()
        {
            var collection = new RangeObservableCollection<int>();
            Assert.Throws<ArgumentNullException>(() => collection.AddRange(null!));
        }

        /// <summary>
        /// Verifies <see cref="RangeObservableCollection{T}.ReplaceAll"/> replaces contents with one Reset.
        /// </summary>
        [Fact]
        public void ReplaceAll_Replaces_With_Single_Reset()
        {
            var collection = new RangeObservableCollection<string> { "a", "b", "c" };
            var notifications = new List<NotifyCollectionChangedAction>();
            collection.CollectionChanged += (_, e) => notifications.Add(e.Action);

            collection.ReplaceAll(["c", "a"]);

            Assert.Equal(["c", "a"], collection);
            Assert.Equal([NotifyCollectionChangedAction.Reset], notifications);
        }

        /// <summary>
        /// Verifies <see cref="RangeObservableCollection{T}.InsertRange"/> inserts items and raises one Reset.
        /// </summary>
        [Fact]
        public void InsertRange_Inserts_With_Single_Reset()
        {
            var collection = new RangeObservableCollection<string> { "a", "c" };
            var notifications = new List<NotifyCollectionChangedAction>();
            collection.CollectionChanged += (_, e) => notifications.Add(e.Action);

            collection.InsertRange(1, ["b1", "b2"]);

            Assert.Equal(["a", "b1", "b2", "c"], collection);
            Assert.Equal([NotifyCollectionChangedAction.Reset], notifications);
        }
    }
}

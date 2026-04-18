using System.IO;
using Mfr.Filters.Attributes;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Attributes
{
    /// <summary>
    /// Tests for <see cref="AttributesSetterFilter"/>.
    /// </summary>
    public sealed class AttributesSetterFilterTests
    {
        private static AttributesSetterFilter _Filter(AttributesSetterOptions options) =>
            new(Target: new AttributesTarget(), Options: options);

        [Fact]
        public void Apply_SetHidden_adds_hidden()
        {
            var item = FilterTestHelpers.CreateRenameItem(attributes: FileAttributes.Normal);
            var filter = _Filter(new AttributesSetterOptions(
                ReadOnly: AttributeTriState.Keep,
                Hidden: AttributeTriState.Set,
                Archive: AttributeTriState.Keep,
                System: AttributeTriState.Keep));
            filter.Setup();
            filter.Apply(item);

            Assert.True(item.Preview.Attributes.HasFlag(FileAttributes.Hidden));
        }

        [Fact]
        public void Apply_ClearArchive_removes_archive()
        {
            var item = FilterTestHelpers.CreateRenameItem(attributes: FileAttributes.Archive);
            var filter = _Filter(new AttributesSetterOptions(
                ReadOnly: AttributeTriState.Keep,
                Hidden: AttributeTriState.Keep,
                Archive: AttributeTriState.Clear,
                System: AttributeTriState.Keep));
            filter.Setup();
            filter.Apply(item);

            Assert.False(item.Preview.Attributes.HasFlag(FileAttributes.Archive));
        }

        [Fact]
        public void Apply_Keep_leaves_flag_unchanged()
        {
            var item = FilterTestHelpers.CreateRenameItem(attributes: FileAttributes.ReadOnly);
            var filter = _Filter(new AttributesSetterOptions(
                ReadOnly: AttributeTriState.Keep,
                Hidden: AttributeTriState.Keep,
                Archive: AttributeTriState.Keep,
                System: AttributeTriState.Keep));
            filter.Setup();
            filter.Apply(item);

            Assert.True(item.Preview.Attributes.HasFlag(FileAttributes.ReadOnly));
        }

        [Fact]
        public void Apply_preserves_directory_bit()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "folder",
                extension: "",
                attributes: FileAttributes.Directory);
            var filter = _Filter(new AttributesSetterOptions(
                ReadOnly: AttributeTriState.Set,
                Hidden: AttributeTriState.Keep,
                Archive: AttributeTriState.Keep,
                System: AttributeTriState.Keep));
            filter.Setup();
            filter.Apply(item);

            Assert.True(item.Preview.Attributes.HasFlag(FileAttributes.Directory));
            Assert.True(item.Preview.Attributes.HasFlag(FileAttributes.ReadOnly));
        }

        [Fact]
        public void Chain_second_filter_composes_on_preview()
        {
            var item = FilterTestHelpers.CreateRenameItem(attributes: FileAttributes.Normal);
            var setHidden = _Filter(new AttributesSetterOptions(
                ReadOnly: AttributeTriState.Keep,
                Hidden: AttributeTriState.Set,
                Archive: AttributeTriState.Keep,
                System: AttributeTriState.Keep));
            var setReadOnly = _Filter(new AttributesSetterOptions(
                ReadOnly: AttributeTriState.Set,
                Hidden: AttributeTriState.Keep,
                Archive: AttributeTriState.Keep,
                System: AttributeTriState.Keep));
            var chain = FilterChain.CreateAllEnabled([setHidden, setReadOnly]);
            chain.SetupFilters();
            chain.ApplyFilters(item);

            Assert.True(item.Preview.Attributes.HasFlag(FileAttributes.Hidden));
            Assert.True(item.Preview.Attributes.HasFlag(FileAttributes.ReadOnly));
        }
    }
}

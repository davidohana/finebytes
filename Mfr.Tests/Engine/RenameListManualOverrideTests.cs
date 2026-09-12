using Mfr.Filters.Case;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Utils;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests manual field overrides (F2) on <see cref="RenameList"/> preview and refresh.
    /// </summary>
    public sealed class RenameListManualOverrideTests : IDisposable
    {
        private readonly string _tempRoot;

        /// <summary>
        /// Initializes a new test instance with an isolated temporary directory.
        /// </summary>
        public RenameListManualOverrideTests()
        {
            _tempRoot = Directory
                .GetCurrentDirectory()
                .CombinePath("mfr_override_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempRoot))
                {
                    Directory.Delete(_tempRoot, recursive: true);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        /// <summary>
        /// Verifies an original-side override seeds filters (PreviewStart) and shows blue/overridden display.
        /// </summary>
        [Fact]
        public void Preview_applies_original_override_before_filters()
        {
            var path = TestHelpers.CreateFile(_tempRoot, "hello.txt");
            var renameList = new RenameList();
            renameList.AddSources([path]);
            var item = renameList.RenameItems[0];

            var originalName = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var previewName = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            item.SetOverride(originalName, "seed");

            Assert.True(item.IsOverridden(originalName));
            Assert.Equal("seed", RenameListFieldCatalog.Resolve(item, originalName));

            renameList.Preview(_UppercasePrefixChain());

            Assert.Equal("SEED", item.Preview.Prefix);
            Assert.Equal("SEED", RenameListFieldCatalog.Resolve(item, previewName));
            Assert.True(RenameListFieldCatalog.IsPreviewChanged(item, previewName));
            Assert.True(item.IsOverridden(originalName));
        }

        /// <summary>
        /// Verifies a preview-side override overwrites filter output (PreviewEnd).
        /// </summary>
        [Fact]
        public void Preview_applies_preview_override_after_filters()
        {
            var path = TestHelpers.CreateFile(_tempRoot, "hello.txt");
            var renameList = new RenameList();
            renameList.AddSources([path]);
            var item = renameList.RenameItems[0];

            var previewName = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            item.SetOverride(previewName, "forced");

            renameList.Preview(_UppercasePrefixChain());

            Assert.Equal("forced", item.Preview.Prefix);
            Assert.Equal("forced", RenameListFieldCatalog.Resolve(item, previewName));
            Assert.True(item.IsOverridden(previewName));
            Assert.True(RenameListFieldCatalog.IsPreviewChanged(item, previewName));
        }

        /// <summary>
        /// Verifies the same override value can be set on multiple items and survives a second preview.
        /// </summary>
        [Fact]
        public void Preview_reapplies_overrides_on_second_pass()
        {
            var (aPath, bPath) = TestHelpers.CreateFiles(_tempRoot, "a.txt", "b.txt");
            var renameList = new RenameList();
            renameList.AddSources([aPath, bPath]);

            var previewName = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            foreach (var item in renameList.RenameItems)
            {
                item.SetOverride(previewName, "same");
            }

            renameList.Preview(_UppercasePrefixChain());
            Assert.All(renameList.RenameItems, item => Assert.Equal("same", item.Preview.Prefix));

            renameList.Preview(_UppercasePrefixChain());
            Assert.All(renameList.RenameItems, item => Assert.Equal("same", item.Preview.Prefix));
            Assert.All(renameList.RenameItems, item => Assert.True(item.IsOverridden(previewName)));
        }

        /// <summary>
        /// Verifies a preview-side override survives the mandatory GO preview and is committed.
        /// </summary>
        [Fact]
        public void Preview_override_is_included_in_commit_plan()
        {
            var path = TestHelpers.CreateFile(_tempRoot, "hello.txt");
            var destination = Path.Combine(_tempRoot, "forced.txt");
            var renameList = new RenameList();
            renameList.AddSources([path]);
            var item = renameList.RenameItems[0];

            var previewName = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            item.SetOverride(previewName, "forced");

            var plan = renameList.Preview(FilterChain.CreateAllEnabled([]));
            renameList.Commit(plan, failFast: false);

            Assert.False(File.Exists(path));
            Assert.True(File.Exists(destination));
            Assert.Equal(RenameStatus.CommitOk, item.Status);
        }

        /// <summary>
        /// Verifies clearing one side leaves other overrides intact.
        /// </summary>
        [Fact]
        public void SetOverride_null_clears_one_side_only()
        {
            var path = TestHelpers.CreateFile(_tempRoot, "hello.txt");
            var renameList = new RenameList();
            renameList.AddSources([path]);
            var item = renameList.RenameItems[0];

            var originalName = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var previewName = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            item.SetOverride(originalName, "org");
            item.SetOverride(previewName, "prev");
            item.SetOverride(originalName, null);

            Assert.False(item.IsOverridden(originalName));
            Assert.True(item.IsOverridden(previewName));
            Assert.Equal("prev", RenameListFieldCatalog.Resolve(item, previewName));
        }

        /// <summary>
        /// Verifies RefreshOriginals clears all manual overrides.
        /// </summary>
        [Fact]
        public void RefreshOriginals_clears_all_overrides()
        {
            var path = TestHelpers.CreateFile(_tempRoot, "hello.txt");
            var renameList = new RenameList();
            renameList.AddSources([path]);
            var item = renameList.RenameItems[0];

            var originalName = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            var previewName = RenameListFieldKey.Preview(BasicRenameListField.Group, BasicRenameListFields.Key.Name);
            item.SetOverride(originalName, "org");
            item.SetOverride(previewName, "prev");

            renameList.RefreshOriginals();

            Assert.False(item.IsOverridden(originalName));
            Assert.False(item.IsOverridden(previewName));
        }

        /// <summary>
        /// Verifies an invalid full-path override surfaces as a preview error.
        /// </summary>
        [Fact]
        public void Preview_invalid_override_sets_preview_error()
        {
            var path = TestHelpers.CreateFile(_tempRoot, "hello.txt");
            var renameList = new RenameList();
            renameList.AddSources([path]);
            var item = renameList.RenameItems[0];

            var previewPath = RenameListFieldKey.Preview(
                BasicRenameListField.Group,
                BasicRenameListFields.Key.FullPath
            );
            item.SetOverride(previewPath, "not-a-full-path");

            renameList.Preview(FilterChain.CreateAllEnabled([]));

            Assert.Equal(RenameStatus.PreviewError, item.Status);
            Assert.NotNull(item.PreviewError);
        }

        private static FilterChain _UppercasePrefixChain()
        {
            return FilterChain.CreateAllEnabled([
                new LettersCaseFilter(
                    new FilePrefixTarget(),
                    new LettersCaseOptions(LettersCaseMode.UpperCase, CapitalizeSkipWords: [])
                ),
            ]);
        }
    }
}

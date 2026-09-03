using Mfr.Filters.Case;
using Mfr.Utils;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests for <see cref="RenameList.Preview(FilterChain)"/>.
    /// </summary>
    public sealed class RenameListPreviewChainTests : IDisposable
    {
        private readonly string _tempRoot;

        /// <summary>
        /// Initializes an isolated temporary directory.
        /// </summary>
        public RenameListPreviewChainTests()
        {
            _tempRoot = Directory
                .GetCurrentDirectory()
                .CombinePath("mfr_preview_chain_" + Guid.NewGuid().ToString("N"));
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
        /// Verifies Preview sets up the chain and transforms preview names.
        /// </summary>
        [Fact]
        public void Preview_FilterChain_applies_enabled_steps()
        {
            var path = Path.Combine(_tempRoot, "hello.txt");
            File.WriteAllText(path, "x");

            var renameList = new RenameList(includeHidden: false);
            renameList.AddSources([path]);

            var chain = FilterChain.CreateAllEnabled([
                new LettersCaseFilter(
                    new FilePrefixTarget(),
                    new LettersCaseOptions(LettersCaseMode.UpperCase, CapitalizeSkipWords: [])
                ),
            ]);

            var plan = renameList.Preview(chain);

            Assert.Equal("HELLO", renameList.RenameItems[0].Preview.Prefix);
            Assert.Equal(RenameStatus.PreviewOk, renameList.RenameItems[0].Status);
            Assert.Equal(1, plan.ChangedCount);
            Assert.Equal(0, plan.UnchangedCount);
            Assert.Equal(0, plan.ErrorCount);
        }

        /// <summary>
        /// Verifies an empty chain resets preview to match original.
        /// </summary>
        [Fact]
        public void Preview_empty_chain_resets_to_identity()
        {
            var path = Path.Combine(_tempRoot, "hello.txt");
            File.WriteAllText(path, "x");

            var renameList = new RenameList(includeHidden: false);
            renameList.AddSources([path]);

            _ = renameList.Preview(
                FilterChain.CreateAllEnabled([
                    new LettersCaseFilter(
                        new FilePrefixTarget(),
                        new LettersCaseOptions(LettersCaseMode.UpperCase, CapitalizeSkipWords: [])
                    ),
                ])
            );
            Assert.Equal("HELLO", renameList.RenameItems[0].Preview.Prefix);

            var identityPlan = renameList.Preview(new FilterChain { Steps = [] });

            Assert.Equal(renameList.RenameItems[0].Original.Prefix, renameList.RenameItems[0].Preview.Prefix);
            Assert.Equal("hello", renameList.RenameItems[0].Preview.Prefix);
            Assert.Equal(0, identityPlan.ChangedCount);
            Assert.Equal(1, identityPlan.UnchangedCount);
            Assert.Equal(0, identityPlan.ErrorCount);
        }
    }
}

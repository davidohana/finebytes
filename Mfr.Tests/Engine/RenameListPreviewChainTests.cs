using Mfr.Filters.Case;
using Mfr.Filters.Formatting;
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

            var renameList = new RenameList();
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

            var renameList = new RenameList();
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

        /// <summary>
        /// Verifies cancel stops remaining items without throwing.
        /// </summary>
        [Fact]
        public void Preview_cancel_stops_without_throwing()
        {
            for (var i = 0; i < 20; i++)
            {
                var path = Path.Combine(_tempRoot, $"f{i:D2}.txt");
                File.WriteAllText(path, "x");
            }

            var renameList = new RenameList();
            renameList.AddSources(Directory.GetFiles(_tempRoot));

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var chain = FilterChain.CreateAllEnabled([
                new LettersCaseFilter(
                    new FilePrefixTarget(),
                    new LettersCaseOptions(LettersCaseMode.UpperCase, CapitalizeSkipWords: [])
                ),
            ]);

            var plan = renameList.Preview(chain, cts.Token);

            Assert.Equal(0, plan.ChangedCount);
            Assert.All(renameList.RenameItems, item => Assert.Equal(item.Original.Prefix, item.Preview.Prefix));
        }

        /// <summary>
        /// Verifies an unknown formatter token in Inserter setup becomes a per-row preview error (MFR7 parity).
        /// </summary>
        [Fact]
        public void Preview_UnknownFormatterToken_MarksAllItemsPreviewError()
        {
            var path = Path.Combine(_tempRoot, "!_RENAME.BAT");
            File.WriteAllText(path, "x");

            var renameList = new RenameList();
            renameList.AddSources([path]);

            var chain = FilterChain.CreateAllEnabled([
                new InserterFilter(
                    new FilePrefixTarget(),
                    new InserterOptions(
                        Text: "ddAA <fsssss> AA111",
                        Position: 1,
                        StartFrom: InserterOrigin.Beginning,
                        Overwrite: false
                    )
                ),
            ]);

            var plan = renameList.Preview(chain);

            Assert.Equal(1, plan.ErrorCount);
            var item = renameList.RenameItems[0];
            Assert.Equal(RenameStatus.PreviewError, item.Status);
            Assert.NotNull(item.PreviewError);
            Assert.Contains("Unknown formatter token", item.PreviewError.Message, StringComparison.Ordinal);
            Assert.Contains("fsssss", item.PreviewError.Message, StringComparison.Ordinal);
            Assert.Contains("Inserter", item.PreviewError.Message, StringComparison.Ordinal);
            // Setup failed: preview stays at identity (no stale insert from a prior pass).
            Assert.Equal(item.Original.Prefix, item.Preview.Prefix);
        }
    }
}

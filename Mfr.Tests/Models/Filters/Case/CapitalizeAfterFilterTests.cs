using Mfr.Filters.Case;
using Mfr.Models;

namespace Mfr.Tests.Models.Filters.Case
{
    public class CapitalizeAfterFilterTests
    {
        private readonly FilterTarget _target = new FileNameTarget(FileNamePart.Prefix);

        [Fact]
        public void Apply_Default_CapitalizesAfterDefaultChars()
        {
            var filter = new CapitalizeAfterFilter(_target, new CapitalizeAfterOptions());

            Assert.Equal("hello,World!(Again)[Is]It-Fine?", FilterTestHelpers.ApplyToPrefix(filter, "hello,world!(again)[is]it-fine?"));
        }

        [Fact]
        public void Apply_CustomChars_CapitalizesOnlyAfterThose()
        {
            var filter = new CapitalizeAfterFilter(_target, new CapitalizeAfterOptions("._"));

            Assert.Equal("hello.World_Again", FilterTestHelpers.ApplyToPrefix(filter, "hello.world_again"));
            Assert.Equal("a,b", FilterTestHelpers.ApplyToPrefix(filter, "a,b")); // , is not in custom set
        }

        [Fact]
        public void Apply_EmptyInput_ReturnsEmpty()
        {
            var filter = new CapitalizeAfterFilter(_target, new CapitalizeAfterOptions());
            Assert.Equal("", FilterTestHelpers.ApplyToPrefix(filter, ""));
        }

        [Fact]
        public void Apply_NoMatches_LeavesUnchanged()
        {
            var filter = new CapitalizeAfterFilter(_target, new CapitalizeAfterOptions());
            Assert.Equal("hello world", FilterTestHelpers.ApplyToPrefix(filter, "hello world"));
        }

        [Fact]
        public void Apply_MatchAtEnd_DoesNotThrow()
        {
            var filter = new CapitalizeAfterFilter(_target, new CapitalizeAfterOptions());
            Assert.Equal("hello!", FilterTestHelpers.ApplyToPrefix(filter, "hello!"));
        }
    }
}

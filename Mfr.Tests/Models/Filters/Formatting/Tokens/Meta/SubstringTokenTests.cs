using Mfr.Filters.Formatting;
using Mfr.Filters.Formatting.Tokens.Meta;

namespace Mfr.Tests.Models.Filters.Formatting.Tokens.Meta
{
    /// <summary>
    /// Tests for <see cref="SubstringToken"/> and its argument parsing.
    /// </summary>
    public sealed class SubstringTokenTests
    {
        private static readonly SubstringToken _token = new();

        // ── Spec examples ─────────────────────────────────────────────────────
        // Item: C:\Example\MyTestFileName.123
        //   <file-name>      = MyTestFileName
        //   <full-name>      = MyTestFileName.123
        //   <file-extension> = .123

        /// <summary>
        /// Spec example 1: <c>&lt;substr:1,5,&lt;file-name&gt;&gt;</c> → <c>MyTes</c>.
        /// </summary>
        [Fact]
        public void Resolve_SpecExample1_PositiveRange_ReturnsFirstFiveChars()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "MyTestFileName", extension: ".123");
            Assert.Equal("MyTes", _token.Compile("1,5,MyTestFileName")(item));
        }

        /// <summary>
        /// Spec example 2: <c>&lt;substr:5,-6,&lt;full-name&gt;&gt;</c> → <c>stFileNam</c>.
        /// Negative end resolves to position 13 in "MyTestFileName.123" (length 18).
        /// </summary>
        [Fact]
        public void Resolve_SpecExample2_NegativeEnd_ReturnsMiddleSlice()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "MyTestFileName", extension: ".123");
            Assert.Equal("stFileNam", _token.Compile("5,-6,MyTestFileName.123")(item));
        }

        /// <summary>
        /// Spec example 3: <c>&lt;substr:-1,2,&lt;file-extension&gt;45&gt;</c> → <c>2345</c>.
        /// Source resolves to ".12345" (length 6). start=-1 → 6, end=2 → 2.
        /// Crossed positions return the range (end, start] → chars 3–6 = "2345".
        /// </summary>
        [Fact]
        public void Resolve_SpecExample3_CrossedPositions_ReturnsCrossedRange()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "MyTestFileName", extension: ".123");
            var compiled = FormatStringCompiler.Compile("<substr:-1,2,<file-extension>45>");
            Assert.Equal("2345", compiled(item));
        }

        // ── Positive positions ─────────────────────────────────────────────────

        /// <summary>Extracts the single first character.</summary>
        [Fact]
        public void Resolve_Start1_End1_ReturnsSingleFirstChar()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "Hello", extension: ".txt");
            Assert.Equal("H", _token.Compile("1,1,Hello.txt")(item));
        }

        /// <summary>Extracts the entire string when start=1 and end equals length.</summary>
        [Fact]
        public void Resolve_FullRange_ReturnsEntireSource()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "abc", extension: ".txt");
            Assert.Equal("abc", _token.Compile("1,3,abc")(item));
        }

        // ── Negative positions ────────────────────────────────────────────────

        /// <summary>Negative start resolves to the last character.</summary>
        [Fact]
        public void Resolve_NegativeStart1_ReturnsLastChar()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "Hello", extension: ".txt");
            Assert.Equal("o", _token.Compile("-1,-1,Hello")(item));
        }

        /// <summary>Extracts using two negative positions that form a valid left-to-right range.</summary>
        [Fact]
        public void Resolve_BothNegative_LeftToRight_ReturnsSlice()
        {
            // "Hello" length 5. start=-4 → 2, end=-2 → 4. Range [2,4] = "ell".
            var item = FilterTestHelpers.CreateRenameItem(prefix: "Hello", extension: ".txt");
            Assert.Equal("ell", _token.Compile("-4,-2,Hello")(item));
        }

        // ── Out-of-range clamping ─────────────────────────────────────────────

        /// <summary>A start position beyond the string length is clamped to the last character.</summary>
        [Fact]
        public void Resolve_StartBeyondLength_ClampsToEnd()
        {
            // start=99 → clamped to 5, end=99 → clamped to 5. Single last char.
            var item = FilterTestHelpers.CreateRenameItem(prefix: "Hello", extension: ".txt");
            Assert.Equal("o", _token.Compile("99,99,Hello")(item));
        }

        /// <summary>A negative start beyond the left edge is clamped to position 1.</summary>
        [Fact]
        public void Resolve_NegativeStartBeyondLength_ClampsToStart()
        {
            // start=-99 → clamped to 1, end=3. Range [1,3] = "Hel".
            var item = FilterTestHelpers.CreateRenameItem(prefix: "Hello", extension: ".txt");
            Assert.Equal("Hel", _token.Compile("-99,3,Hello")(item));
        }

        // ── Nested format string ───────────────────────────────────────────────

        /// <summary>
        /// Verifies the nested form resolves the inner <c>&lt;file-name&gt;</c> before extracting.
        /// </summary>
        [Fact]
        public void ResolveTemplate_NestedFileName_ResolvesInnerFirst()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "MyTestFileName", extension: ".123");
            var compiled = FormatStringCompiler.Compile("<substr:1,5,<file-name>>");
            Assert.Equal("MyTes", compiled(item));
        }

        /// <summary>
        /// Verifies a nested full-name token matches spec example 2.
        /// </summary>
        [Fact]
        public void ResolveTemplate_SpecExample2_NestedFullName()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "MyTestFileName", extension: ".123");
            var compiled = FormatStringCompiler.Compile("<substr:5,-6,<full-name>>");
            Assert.Equal("stFileNam", compiled(item));
        }

        // ── Empty source ───────────────────────────────────────────────────────

        /// <summary>An empty resolved source returns an empty string.</summary>
        [Fact]
        public void Resolve_EmptySource_ReturnsEmpty()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "track", extension: ".mp3");
            Assert.Equal(string.Empty, _token.Compile("1,3,")(item));
        }

        // ── Deep nesting (multiple levels) ───────────────────────────────────

        /// <summary>
        /// Two <c>substr</c> tokens nested: the outer extracts chars 1–3 of what the
        /// inner (<c>substr:2,5</c>) produces from <c>&lt;full-name&gt;</c>.
        /// "Hello.txt" → inner: chars 2–5 = "ello" → outer: chars 1–3 = "ell".
        /// </summary>
        [Fact]
        public void ResolveTemplate_SubstrInsideSubstr_TwoLevels()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "Hello", extension: ".txt");
            var compiled = FormatStringCompiler.Compile("<substr:1,3,<substr:2,5,<full-name>>>");
            Assert.Equal("ell", compiled(item));
        }

        /// <summary>
        /// Three <c>substr</c> tokens nested. "Hello.txt":
        /// inner: chars 2–5 = "ello" → middle: chars 1–3 = "ell" → outer: chars 1–2 = "el".
        /// </summary>
        [Fact]
        public void ResolveTemplate_SubstrInsideSubstrInsideSubstr_ThreeLevels()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "Hello", extension: ".txt");
            var compiled = FormatStringCompiler.Compile("<substr:1,2,<substr:1,3,<substr:2,5,<full-name>>>>");
            Assert.Equal("el", compiled(item));
        }

        /// <summary>
        /// <c>substr</c> wrapping <c>token</c> wrapping <c>&lt;full-name&gt;</c>.
        /// File "13_-_Smog_-_Cold.mp3": token extracts part 1 split by "-" = "13_";
        /// substr takes chars 1–2 = "13".
        /// </summary>
        [Fact]
        public void ResolveTemplate_SubstrWrappingToken_TwoLevels()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "13_-_Smog_-_Cold",
                extension: ".mp3");
            var compiled = FormatStringCompiler.Compile("<substr:1,2,<token:1,-,false,false,<full-name>>>");
            Assert.Equal("13", compiled(item));
        }

        /// <summary>
        /// <c>token</c> wrapping <c>substr</c> wrapping <c>&lt;full-name&gt;</c>.
        /// File "My-Test-File.txt": substr takes chars 1–7 = "My-Test";
        /// token extracts part 2 split by "-" = "Test".
        /// </summary>
        [Fact]
        public void ResolveTemplate_TokenWrappingSubstr_TwoLevels()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "My-Test-File", extension: ".txt");
            var compiled = FormatStringCompiler.Compile("<token:2,-,false,false,<substr:1,7,<full-name>>>");
            Assert.Equal("Test", compiled(item));
        }

        // ── Error cases ───────────────────────────────────────────────────────

        /// <summary>A missing argument string throws with a descriptive message.</summary>
        [Fact]
        public void Resolve_EmptyArg_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            var ex = Assert.Throws<ArgumentException>(() => _token.Compile("")(item));
            Assert.Contains("<substr>", ex.Message);
        }

        /// <summary>Fewer than three comma-separated arguments throws.</summary>
        [Fact]
        public void Resolve_TooFewArgs_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem();
            var ex = Assert.Throws<ArgumentException>(() => _token.Compile("1,5")(item));
            Assert.Contains("3 comma-separated", ex.Message);
        }

        /// <summary>A start-position of zero throws.</summary>
        [Fact]
        public void Resolve_StartPositionZero_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "abc", extension: ".txt");
            var ex = Assert.Throws<ArgumentException>(() => _token.Compile("0,3,abc")(item));
            Assert.Contains("start-position", ex.Message);
        }

        /// <summary>An end-position of zero throws.</summary>
        [Fact]
        public void Resolve_EndPositionZero_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "abc", extension: ".txt");
            var ex = Assert.Throws<ArgumentException>(() => _token.Compile("1,0,abc")(item));
            Assert.Contains("end-position", ex.Message);
        }

        /// <summary>A non-integer start-position throws.</summary>
        [Fact]
        public void Resolve_NonIntegerStart_Throws()
        {
            var item = FilterTestHelpers.CreateRenameItem(prefix: "abc", extension: ".txt");
            var ex = Assert.Throws<ArgumentException>(() => _token.Compile("x,3,abc")(item));
            Assert.Contains("start-position", ex.Message);
        }
    }
}

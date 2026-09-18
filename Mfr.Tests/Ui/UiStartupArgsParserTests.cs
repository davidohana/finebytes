using Mfr.App.Ui;

namespace Mfr.Tests.Ui
{
    /// <summary>
    /// Tests <see cref="UiStartupArgsParser"/> desktop argv shapes.
    /// </summary>
    public sealed class UiStartupArgsParserTests
    {
        /// <summary>
        /// Verifies empty argv yields an empty result with no intents.
        /// </summary>
        [Fact]
        public void Parse_Empty_Args_Returns_Empty()
        {
            var result = UiStartupArgsParser.Parse([]);
            Assert.Same(UiStartupArgs.Empty, result);
            Assert.False(result.HasAnyIntent);
            Assert.Empty(result.Sources);
            Assert.Null(result.InitialFolder);
            Assert.Null(result.IncludeFiles);
            Assert.Null(result.IncludeFolders);
            Assert.Null(result.IncludeSubdirs);
            Assert.Null(result.IncludeHidden);
        }

        /// <summary>
        /// Verifies positional sources-only parsing with no modifier overrides.
        /// </summary>
        [Fact]
        public void Parse_Sources_Only()
        {
            var result = UiStartupArgsParser.Parse(["C:\\Music\\*.mp3", "C:\\Podcasts"]);
            Assert.Equal(["C:\\Music\\*.mp3", "C:\\Podcasts"], result.Sources);
            Assert.Null(result.InitialFolder);
            Assert.Null(result.IncludeFiles);
            Assert.Null(result.IncludeFolders);
            Assert.Null(result.IncludeSubdirs);
            Assert.Null(result.IncludeHidden);
            Assert.True(result.HasAnyIntent);
        }

        /// <summary>
        /// Verifies <c>--initial-folder</c> alone sets browse path without sources or modifiers.
        /// </summary>
        [Fact]
        public void Parse_InitialFolder_Only()
        {
            var result = UiStartupArgsParser.Parse(["--initial-folder", "C:\\photos"]);
            Assert.Empty(result.Sources);
            Assert.Equal("C:\\photos", result.InitialFolder);
            Assert.Null(result.IncludeFiles);
            Assert.Null(result.IncludeFolders);
            Assert.Null(result.IncludeSubdirs);
            Assert.Null(result.IncludeHidden);
            Assert.True(result.HasAnyIntent);
        }

        /// <summary>
        /// Verifies add modifiers parse and omitted ones stay unset.
        /// </summary>
        [Fact]
        public void Parse_Add_Modifiers()
        {
            var result = UiStartupArgsParser.Parse([
                "C:\\batch",
                "--files",
                "yes",
                "--folders",
                "no",
                "-r",
                "--include-hidden",
            ]);
            Assert.Equal(["C:\\batch"], result.Sources);
            Assert.True(result.IncludeFiles);
            Assert.False(result.IncludeFolders);
            Assert.True(result.IncludeSubdirs);
            Assert.True(result.IncludeHidden);
            Assert.Null(result.InitialFolder);
        }

        /// <summary>
        /// Verifies <c>--recursive</c> long form and case-insensitive yes|no.
        /// </summary>
        [Fact]
        public void Parse_Recursive_Long_And_YesNo_CaseInsensitive()
        {
            var result = UiStartupArgsParser.Parse([
                "C:\\Music",
                "--files",
                "NO",
                "--folders",
                "YeS",
                "--recursive",
                "--initial-folder",
                "  C:\\Music  ",
            ]);
            Assert.False(result.IncludeFiles);
            Assert.True(result.IncludeFolders);
            Assert.True(result.IncludeSubdirs);
            Assert.Equal("C:\\Music", result.InitialFolder);
        }

        /// <summary>
        /// Verifies sources and <c>--initial-folder</c> can be combined in one argv.
        /// </summary>
        [Fact]
        public void Parse_Sources_And_InitialFolder_Combined()
        {
            var result = UiStartupArgsParser.Parse([
                "C:\\batch\\a.jpg",
                "C:\\batch\\b.jpg",
                "--initial-folder",
                "C:\\batch",
            ]);
            Assert.Equal(["C:\\batch\\a.jpg", "C:\\batch\\b.jpg"], result.Sources);
            Assert.Equal("C:\\batch", result.InitialFolder);
            Assert.True(result.HasAnyIntent);
        }

        /// <summary>
        /// Verifies whitespace-only positionals are dropped and yield an empty result.
        /// </summary>
        [Fact]
        public void Parse_Whitespace_Only_Positionals_Returns_Empty()
        {
            var result = UiStartupArgsParser.Parse(["  ", "\t"]);
            Assert.Same(UiStartupArgs.Empty, result);
            Assert.False(result.HasAnyIntent);
        }

        /// <summary>
        /// Verifies invalid yes|no values throw a clear <see cref="UserException"/>.
        /// </summary>
        [Fact]
        public void Parse_Rejects_Invalid_YesNo()
        {
            var ex = Assert.Throws<UserException>(() => UiStartupArgsParser.Parse(["C:\\Music", "--files", "maybe"]));
            Assert.Contains("Invalid value for --files", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies unknown flags throw a clear <see cref="UserException"/>.
        /// </summary>
        [Fact]
        public void Parse_Rejects_Unknown_Flag()
        {
            var ex = Assert.Throws<UserException>(() =>
                UiStartupArgsParser.Parse(["C:\\Music", "--not-a-real-option"])
            );
            Assert.Contains("Unknown option", ex.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies missing option values throw a clear <see cref="UserException"/>.
        /// </summary>
        [Theory]
        [InlineData(new[] { "--initial-folder" }, "--initial-folder")]
        [InlineData(new[] { "--files", "--folders", "yes" }, "--files")]
        [InlineData(new[] { "--initial-folder", "   " }, "--initial-folder")]
        public void Parse_Rejects_Missing_Option_Value(string[] args, string optionName)
        {
            var ex = Assert.Throws<UserException>(() => UiStartupArgsParser.Parse(args));
            Assert.Contains($"Missing value for {optionName}", ex.Message, StringComparison.Ordinal);
        }
    }
}

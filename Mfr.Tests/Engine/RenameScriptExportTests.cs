using System.Text;
using Mfr.Engine.RenameScript;
using Mfr.Models.Tags;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Golden bat/ps1 coverage for rename-script IR collection and formatting.
    /// </summary>
    public sealed class RenameScriptExportTests : IDisposable
    {
        private static readonly string _HeaderBat =
            "REM **********************************************************\n"
            + "REM *\n"
            + "REM This file was created by Magic File Renamer\n"
            + "REM https://www.finebytes.com/mfr\n"
            + "REM *\n"
            + "REM **********************************************************\n"
            + "\n";

        private static readonly string _HeaderPs1 =
            "# **********************************************************\n"
            + "# *\n"
            + "# This file was created by Magic File Renamer\n"
            + "# https://www.finebytes.com/mfr\n"
            + "# *\n"
            + "# **********************************************************\n"
            + "\n";

        private readonly string _tempRoot;

        /// <summary>
        /// Creates a unique temp folder for export encoding tests.
        /// </summary>
        public RenameScriptExportTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "mfr_rename_script_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }

        /// <summary>
        /// Same-folder rename emits <c>ren</c> / <c>Rename-Item</c> only.
        /// </summary>
        [Fact]
        public void Format_rename_only_matches_golden_bat_and_ps1()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                fileName: "Blue hills",
                extension: "GIF",
                directory: @"C:\_mfr_test\img1"
            );
            item.Preview.FileName = "BOHNL";

            _AssertFormats(
                [item],
                expectedBat: _HeaderBat + "ren \"C:\\_mfr_test\\img1\\Blue hills.GIF\" \"BOHNL.GIF\"\n" + "\n",
                expectedPs1: _HeaderPs1
                    + "Rename-Item -LiteralPath 'C:\\_mfr_test\\img1\\Blue hills.GIF' -NewName 'BOHNL.GIF'\n"
                    + "\n"
            );
        }

        /// <summary>
        /// Case-only same-folder rename uses rename, not move.
        /// </summary>
        [Fact]
        public void Format_case_only_same_folder_uses_rename_not_move()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                fileName: "Album",
                extension: "",
                directory: @"D:\Music",
                attributes: FileAttributes.Directory
            );
            item.Preview.FileName = "album";

            var groups = RenameScriptCollector.Collect([item]);
            var ops = Assert.Single(groups);
            var rename = Assert.IsType<RenameSameFolder>(Assert.Single(ops));
            Assert.Equal(@"D:\Music\Album", rename.SourceFullPath);
            Assert.Equal("album", rename.DestinationFileName);

            var bat = RenameScriptFormatter.Format(groups, RenameScriptFormat.Bat);
            Assert.Contains("ren ", bat, StringComparison.Ordinal);
            Assert.DoesNotContain("move ", bat, StringComparison.Ordinal);
            Assert.DoesNotContain("mkdir ", bat, StringComparison.Ordinal);
        }

        /// <summary>
        /// Folder change emits mkdir/New-Item plus move/Move-Item.
        /// </summary>
        [Fact]
        public void Format_move_with_mkdir_matches_golden_bat_and_ps1()
        {
            var item = FilterTestHelpers.CreateRenameItem(fileName: "song", extension: "mp3", directory: @"D:\In");
            item.Preview.DirectoryPath = @"D:\Out";

            _AssertFormats(
                [item],
                expectedBat: _HeaderBat
                    + "if not exist \"D:\\Out\" mkdir \"D:\\Out\"\n"
                    + "move \"D:\\In\\song.mp3\" \"D:\\Out\\song.mp3\"\n"
                    + "\n",
                expectedPs1: _HeaderPs1
                    + "if (-not (Test-Path -LiteralPath 'D:\\Out')) { New-Item -ItemType Directory -Path 'D:\\Out' | Out-Null }\n"
                    + "Move-Item -LiteralPath 'D:\\In\\song.mp3' -Destination 'D:\\Out\\song.mp3'\n"
                    + "\n"
            );
        }

        /// <summary>
        /// Attribute-only rows emit attrib / PowerShell bit ops on the (unchanged) path.
        /// </summary>
        [Fact]
        public void Format_attrs_only_matches_golden_bat_and_ps1()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                fileName: "Blue hills",
                extension: "GIF",
                directory: @"C:\_mfr_test\img1",
                attributes: FileAttributes.Normal
            );
            item.Preview.Attributes = FileAttributes.Hidden;

            _AssertFormats(
                [item],
                expectedBat: _HeaderBat + "attrib +H \"C:\\_mfr_test\\img1\\Blue hills.GIF\"\n" + "\n",
                expectedPs1: _HeaderPs1
                    + "$item = Get-Item -LiteralPath 'C:\\_mfr_test\\img1\\Blue hills.GIF' -Force\n"
                    + "$item.Attributes = $item.Attributes -bor [System.IO.FileAttributes]::Hidden\n"
                    + "\n"
            );
        }

        /// <summary>
        /// Rename plus attrs applies attrib / set on the preview destination path.
        /// </summary>
        [Fact]
        public void Format_rename_plus_attrs_targets_preview_path()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                fileName: "Blue hills",
                extension: "GIF",
                directory: @"C:\_mfr_test\img1",
                attributes: FileAttributes.Archive
            );
            item.Preview.FileName = "BOHNL";
            item.Preview.Attributes = FileAttributes.Archive | FileAttributes.Hidden;

            _AssertFormats(
                [item],
                expectedBat: _HeaderBat
                    + "ren \"C:\\_mfr_test\\img1\\Blue hills.GIF\" \"BOHNL.GIF\"\n"
                    + "attrib +H \"C:\\_mfr_test\\img1\\BOHNL.GIF\"\n"
                    + "\n",
                expectedPs1: _HeaderPs1
                    + "Rename-Item -LiteralPath 'C:\\_mfr_test\\img1\\Blue hills.GIF' -NewName 'BOHNL.GIF'\n"
                    + "$item = Get-Item -LiteralPath 'C:\\_mfr_test\\img1\\BOHNL.GIF' -Force\n"
                    + "$item.Attributes = $item.Attributes -bor [System.IO.FileAttributes]::Hidden\n"
                    + "\n"
            );
        }

        /// <summary>
        /// Clearing RAHS bits emits <c>-Letter</c> / <c>-band -bnot</c> on the path.
        /// </summary>
        [Fact]
        public void Format_attrs_clear_matches_golden_bat_and_ps1()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                fileName: "notes",
                extension: "txt",
                directory: @"D:\Docs",
                attributes: FileAttributes.ReadOnly | FileAttributes.Archive
            );
            item.Preview.Attributes = FileAttributes.Archive;

            _AssertFormats(
                [item],
                expectedBat: _HeaderBat + "attrib -R \"D:\\Docs\\notes.txt\"\n" + "\n",
                expectedPs1: _HeaderPs1
                    + "$item = Get-Item -LiteralPath 'D:\\Docs\\notes.txt' -Force\n"
                    + "$item.Attributes = $item.Attributes -band (-bnot [System.IO.FileAttributes]::ReadOnly)\n"
                    + "\n"
            );
        }

        /// <summary>
        /// Move plus attrs applies attrib / set on the preview destination path (not the source).
        /// </summary>
        [Fact]
        public void Format_move_plus_attrs_targets_preview_path()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                fileName: "song",
                extension: "mp3",
                directory: @"D:\In",
                attributes: FileAttributes.Normal
            );
            item.Preview.DirectoryPath = @"D:\Out";
            item.Preview.Attributes = FileAttributes.Hidden;

            _AssertFormats(
                [item],
                expectedBat: _HeaderBat
                    + "if not exist \"D:\\Out\" mkdir \"D:\\Out\"\n"
                    + "move \"D:\\In\\song.mp3\" \"D:\\Out\\song.mp3\"\n"
                    + "attrib +H \"D:\\Out\\song.mp3\"\n"
                    + "\n",
                expectedPs1: _HeaderPs1
                    + "if (-not (Test-Path -LiteralPath 'D:\\Out')) { New-Item -ItemType Directory -Path 'D:\\Out' | Out-Null }\n"
                    + "Move-Item -LiteralPath 'D:\\In\\song.mp3' -Destination 'D:\\Out\\song.mp3'\n"
                    + "$item = Get-Item -LiteralPath 'D:\\Out\\song.mp3' -Force\n"
                    + "$item.Attributes = $item.Attributes -bor [System.IO.FileAttributes]::Hidden\n"
                    + "\n"
            );
        }

        /// <summary>
        /// Embedded quotes in paths are escaped for cmd (<c>""</c>) and PowerShell (<c>''</c>).
        /// </summary>
        [Fact]
        public void Format_escapes_quotes_in_paths()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                fileName: "O'Brien \"quote\"",
                extension: "txt",
                directory: @"D:\Artist's ""Folder"""
            );
            item.Preview.FileName = "done";

            _AssertFormats(
                [item],
                expectedBat: _HeaderBat
                    + "ren \"D:\\Artist's \"\"Folder\"\"\\O'Brien \"\"quote\"\".txt\" \"done.txt\"\n"
                    + "\n",
                expectedPs1: _HeaderPs1
                    + "Rename-Item -LiteralPath 'D:\\Artist''s \"Folder\"\\O''Brien \"quote\".txt' -NewName 'done.txt'\n"
                    + "\n"
            );
        }

        /// <summary>
        /// Percent signs in bat paths are doubled so cmd does not expand <c>%VAR%</c>; PowerShell keeps them.
        /// </summary>
        [Fact]
        public void Format_escapes_percent_in_bat_paths()
        {
            var item = FilterTestHelpers.CreateRenameItem(
                fileName: "100%TEMP%done",
                extension: "txt",
                directory: @"D:\100%OFF"
            );
            item.Preview.FileName = "safe";

            _AssertFormats(
                [item],
                expectedBat: _HeaderBat + "ren \"D:\\100%%OFF\\100%%TEMP%%done.txt\" \"safe.txt\"\n" + "\n",
                expectedPs1: _HeaderPs1
                    + "Rename-Item -LiteralPath 'D:\\100%OFF\\100%TEMP%done.txt' -NewName 'safe.txt'\n"
                    + "\n"
            );
        }

        /// <summary>
        /// PreviewError rows are omitted from the script.
        /// </summary>
        [Fact]
        public void Collect_skips_preview_error_rows()
        {
            var ok = FilterTestHelpers.CreateRenameItem(fileName: "ok", extension: "txt", directory: @"D:\A");
            ok.Preview.FileName = "ok2";

            var errored = FilterTestHelpers.CreateRenameItem(fileName: "bad", extension: "txt", directory: @"D:\A");
            errored.Preview.FileName = "bad2";
            errored.SetPreviewError(message: "preview failed", cause: null);

            var groups = RenameScriptCollector.Collect([errored, ok]);
            var ops = Assert.Single(groups);
            var rename = Assert.IsType<RenameSameFolder>(Assert.Single(ops));
            Assert.Equal(@"D:\A\ok.txt", rename.SourceFullPath);
        }

        /// <summary>
        /// Tag-only (and date-only) preview changes do not emit script ops.
        /// </summary>
        [Fact]
        public void Collect_skips_tag_only_and_date_only_rows()
        {
            var tagOnly = FilterTestHelpers.CreateRenameItem(fileName: "song", extension: "mp3", directory: @"D:\In");
            var merged = SemanticAudioTag.FromOverlay(tagOnly.Preview.AudioTagOverlay) with { Title = "PreviewTitle" };
            tagOnly.Preview.AudioTagOverlay.MergeSemantic(merged);
            Assert.True(tagOnly.HasPreviewChanges());
            Assert.True(tagOnly.IsPreviewPathUnchanged());

            var dateOnly = FilterTestHelpers.CreateRenameItem(fileName: "doc", extension: "txt", directory: @"D:\In");
            dateOnly.Preview.LastWriteTime = dateOnly.Original.LastWriteTime.AddHours(1);
            Assert.True(dateOnly.HasPreviewChanges());

            Assert.Empty(RenameScriptCollector.Collect([tagOnly, dateOnly]));
        }

        /// <summary>
        /// <see cref="RenameList.ExportRenameScript"/> writes UTF-8; PowerShell includes a BOM.
        /// </summary>
        [Fact]
        public void ExportRenameScript_writes_utf8_ps1_with_bom_and_bat_without()
        {
            var dir = Path.Combine(_tempRoot, "files");
            Directory.CreateDirectory(dir);
            var sourcePath = Path.Combine(dir, "alpha.txt");
            File.WriteAllText(sourcePath, "x");

            var renameList = new RenameList();
            renameList.AddSources([sourcePath]);
            var item = Assert.Single(renameList.RenameItems);
            item.Preview.FileName = "beta";

            var batPath = Path.Combine(_tempRoot, "out.bat");
            var ps1Path = Path.Combine(_tempRoot, "out.ps1");
            Assert.Equal(1, renameList.ExportRenameScript(batPath, RenameScriptFormat.Bat));
            Assert.Equal(1, renameList.ExportRenameScript(ps1Path, RenameScriptFormat.PowerShell));

            var batBytes = File.ReadAllBytes(batPath);
            var ps1Bytes = File.ReadAllBytes(ps1Path);
            Assert.False(_HasUtf8Bom(batBytes));
            Assert.True(_HasUtf8Bom(ps1Bytes));

            var batText = Encoding.UTF8.GetString(batBytes);
            var ps1Text = Encoding.UTF8.GetString(ps1Bytes.AsSpan(3));
            Assert.Contains($"ren \"{item.Original.FullPath}\" \"beta.txt\"", batText, StringComparison.Ordinal);
            Assert.Contains(
                $"Rename-Item -LiteralPath '{item.Original.FullPath}' -NewName 'beta.txt'",
                ps1Text,
                StringComparison.Ordinal
            );
        }

        /// <summary>
        /// Empty Collect returns 0 and does not create the destination file.
        /// </summary>
        [Fact]
        public void ExportRenameScript_empty_collect_returns_zero_without_writing()
        {
            var dir = Path.Combine(_tempRoot, "empty");
            Directory.CreateDirectory(dir);
            var sourcePath = Path.Combine(dir, "same.txt");
            File.WriteAllText(sourcePath, "x");

            var renameList = new RenameList();
            renameList.AddSources([sourcePath]);
            Assert.Single(renameList.RenameItems);

            var outPath = Path.Combine(_tempRoot, "empty.bat");
            Assert.Equal(0, renameList.ExportRenameScript(outPath, RenameScriptFormat.Bat));
            Assert.False(File.Exists(outPath));
            Assert.Equal(0, renameList.CountRenameScriptItems());
        }

        private static void _AssertFormats(IEnumerable<RenameItem> items, string expectedBat, string expectedPs1)
        {
            var groups = RenameScriptCollector.Collect(items);
            var bat = RenameScriptFormatter.Format(groups, RenameScriptFormat.Bat);
            var ps1 = RenameScriptFormatter.Format(groups, RenameScriptFormat.PowerShell);
            Assert.Equal(_NormalizeNewlines(expectedBat), _NormalizeNewlines(bat));
            Assert.Equal(_NormalizeNewlines(expectedPs1), _NormalizeNewlines(ps1));
        }

        private static string _NormalizeNewlines(string text)
        {
            return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        }

        private static bool _HasUtf8Bom(byte[] bytes)
        {
            return bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        }
    }
}

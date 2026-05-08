using Mfr.Core;
using Mfr.Filters.Formatting;
using Mfr.Filters.Misc;
using Mfr.Filters.Replace;
using Mfr.Models;
using Mfr.Tests.TestSupport;
using Mfr.Utils;
using FormatterFilter = Mfr.Filters.Formatting.FormatterFilter;

namespace Mfr.Tests.Core
{
    /// <summary>
    /// End-to-end tests covering rename batches that mix folder renames with their descendants,
    /// folder swaps, case-only renames, and the <see cref="MoverFilter"/> directing items into a
    /// folder that is itself being renamed in the same batch.
    /// </summary>
    public sealed class RenameListFolderChildTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Disposes temporary test resources created for this test method.
        /// </summary>
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Renames a parent folder while also renaming a file inside it; both changes should land cleanly
        /// without conflicts and the file should end up at the rebased path.
        /// </summary>
        [Fact]
        public void Folder_and_child_rename_in_same_batch_succeed()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var oldFolderPath = dir.CombinePath("Album");
            Directory.CreateDirectory(oldFolderPath);
            var oldFilePath = oldFolderPath.CombinePath("track.mp3");
            File.WriteAllText(oldFilePath, "x");

            var newFolderPath = dir.CombinePath("AlbumRenamed");
            var renamedFilePath = newFolderPath.CombinePath("song.mp3");

            var renameList = new RenameList(includeHidden: true);
            // Add the folder by itself (folders-only) and the file as a direct source.
            renameList.AddSource(
                source: oldFolderPath,
                includeFiles: false,
                includeFolders: true);
            renameList.AddSource(
                source: oldFilePath,
                includeFiles: true,
                includeFolders: false);
            Assert.Equal(2, renameList.RenameItems.Count);

            var folderItem = renameList.RenameItems.Single(item => item.Original.FullPath == oldFolderPath);
            var fileItem = renameList.RenameItems.Single(item => item.Original.FullPath == oldFilePath);

            // Two literal replacers in a single chain: rename folder (prefix "Album" -> "AlbumRenamed")
            // and rename file (prefix "track" -> "song"). Both target FilePrefixTarget so each item's
            // matching prefix is independently rewritten without affecting the other.
            var preset = _CreatePresetAllEnabled(
                "folder-and-child",
                _LiteralPrefixReplacer("Album", "AlbumRenamed"),
                _LiteralPrefixReplacer("track", "song"));
            _SetupPreview(renameList, preset);

            Assert.Equal(newFolderPath, folderItem.Preview.FullPath);
            Assert.Equal(renamedFilePath, fileItem.Preview.FullPath);
            Assert.DoesNotContain(renameList.RenameItems, item => item.PreviewError is not null);

            var result = renameList.Commit(failFast: false);
            Assert.Equal(2, result.Count(r => r.Status == RenameStatus.CommitOk));
            Assert.False(Directory.Exists(oldFolderPath));
            Assert.True(Directory.Exists(newFolderPath));
            Assert.True(File.Exists(renamedFilePath));
            Assert.False(File.Exists(oldFilePath));
        }

        /// <summary>
        /// Performs a folder swap (FolderA &lt;-&gt; FolderB). The planner must stash one folder while
        /// the other moves and then finalize the stashed one.
        /// </summary>
        [Fact]
        public void Two_folder_swap_completes_via_stash_and_finalize()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var folderA = dir.CombinePath("FolderA");
            var folderB = dir.CombinePath("FolderB");
            Directory.CreateDirectory(folderA);
            Directory.CreateDirectory(folderB);
            File.WriteAllText(folderA.CombinePath("a.txt"), "from-a");
            File.WriteAllText(folderB.CombinePath("b.txt"), "from-b");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources(
                sources: [folderA, folderB],
                includeFiles: false,
                includeFolders: true);
            Assert.Equal(2, renameList.RenameItems.Count);

            // A pair of replacers performs FolderA -> tmp -> FolderB while FolderB -> FolderA via case differences
            // would not work; instead we use FullPathTarget formatters keyed by source path so each item gets a
            // distinct preview destination without collisions.
            var preset = _CreatePresetAllEnabled(
                "folder-swap",
                _LiteralPrefixReplacer("FolderA", "__SWAP_PLACEHOLDER__"),
                _LiteralPrefixReplacer("FolderB", "FolderA"),
                _LiteralPrefixReplacer("__SWAP_PLACEHOLDER__", "FolderB"));
            _SetupPreview(renameList, preset);

            var itemA = renameList.RenameItems.Single(item => item.Original.FullPath == folderA);
            var itemB = renameList.RenameItems.Single(item => item.Original.FullPath == folderB);
            Assert.Equal(folderB, itemA.Preview.FullPath);
            Assert.Equal(folderA, itemB.Preview.FullPath);
            Assert.DoesNotContain(renameList.RenameItems, item => item.PreviewError is not null);

            var result = renameList.Commit(failFast: false);
            Assert.Equal(2, result.Count(r => r.Status == RenameStatus.CommitOk));

            // a.txt now lives where b.txt used to (FolderB).
            Assert.True(File.Exists(folderB.CombinePath("a.txt")));
            // b.txt now lives where a.txt used to (FolderA).
            Assert.True(File.Exists(folderA.CombinePath("b.txt")));
            // The original siblings should not coexist with their swapped counterparts.
            Assert.False(File.Exists(folderA.CombinePath("a.txt")));
            Assert.False(File.Exists(folderB.CombinePath("b.txt")));
        }

        /// <summary>
        /// On Windows, a case-only rename of a file is committed via a temp-name dance, producing the new casing.
        /// </summary>
        [Fact]
        public void Case_only_file_rename_on_windows_applies_new_casing()
        {
            if (!OperatingSystem.IsWindows())
                return;


            var dir = _tempDirectoryFixture.CreateTempDir();
            var sourcePath = dir.CombinePath("track.mp3");
            File.WriteAllText(sourcePath, "x");

            var destinationPath = dir.CombinePath("TRACK.mp3");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSources([sourcePath]);

            var preset = _FormatterFullPathPreset("case-only-rename", destinationPath);
            _SetupPreview(renameList, preset);
            var item = renameList.RenameItems[0];
            Assert.True(item.HasPreviewChanges());
            Assert.Equal(destinationPath, item.Preview.FullPath);
            Assert.Null(item.PreviewError);

            var result = renameList.Commit(failFast: false);
            Assert.Single(result);
            Assert.Equal(RenameStatus.CommitOk, result[0].Status);
            Assert.Contains(result[0].Changes, c => c.Property == "FileName");

            var enumeratedNames = Directory.EnumerateFiles(dir).Select(Path.GetFileName).ToList();
            Assert.Contains("TRACK.mp3", enumeratedNames);
            Assert.DoesNotContain("track.mp3", enumeratedNames);
        }

        /// <summary>
        /// On Windows, a case-only rename of a folder applies via the temp-name dance and preserves the contained file.
        /// </summary>
        [Fact]
        public void Case_only_folder_rename_on_windows_applies_new_casing()
        {
            if (!OperatingSystem.IsWindows())
                return;


            var dir = _tempDirectoryFixture.CreateTempDir();
            var oldFolder = dir.CombinePath("album");
            Directory.CreateDirectory(oldFolder);
            File.WriteAllText(oldFolder.CombinePath("track.mp3"), "x");

            var newFolder = dir.CombinePath("ALBUM");

            var renameList = new RenameList(includeHidden: true);
            renameList.AddSource(
                source: oldFolder,
                includeFiles: false,
                includeFolders: true);

            var preset = _FormatterFullPathPreset("folder-case-rename", newFolder);
            _SetupPreview(renameList, preset);
            var item = renameList.RenameItems[0];
            Assert.True(item.HasPreviewChanges());
            Assert.Null(item.PreviewError);

            var result = renameList.Commit(failFast: false);
            Assert.Single(result);
            Assert.Equal(RenameStatus.CommitOk, result[0].Status);

            var enumeratedDirNames = Directory.EnumerateDirectories(dir)
                .Select(Path.GetFileName)
                .ToList();
            Assert.Contains("ALBUM", enumeratedDirNames);
            Assert.DoesNotContain("album", enumeratedDirNames);
            Assert.True(File.Exists(newFolder.CombinePath("track.mp3")));
        }

        /// <summary>
        /// A <see cref="MoverFilter"/> moves a file into a folder that is itself being renamed in the same batch.
        /// The planner must commit the folder rename first so the destination directory exists when the file is moved.
        /// </summary>
        [Fact]
        public void Mover_into_folder_that_is_being_renamed_succeeds()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var oldFolder = dir.CombinePath("Sink");
            var newFolder = dir.CombinePath("SinkRenamed");
            Directory.CreateDirectory(oldFolder);
            var sourceFile = dir.CombinePath("loose.txt");
            File.WriteAllText(sourceFile, "x");

            var renameList = new RenameList(includeHidden: true);
            // Order: file first, then folder. The planner must still commit the folder rename first
            // so the file's destination directory (newFolder) exists when the file is moved.
            renameList.AddSources(
                sources: [sourceFile, oldFolder],
                includeFiles: true,
                includeFolders: true);
            Assert.Equal(2, renameList.RenameItems.Count);

            // Two filters compose: Mover sends the loose file under newFolder,
            // and a Replacer renames the folder's prefix from "Sink" -> "SinkRenamed".
            var preset = _CreatePresetAllEnabled(
                "mover-into-renamed",
                new MoverFilter(new MoverOptions(RootFolder: newFolder)),
                _LiteralPrefixReplacer("Sink", "SinkRenamed"));
            _SetupPreview(renameList, preset);

            var folderItem = renameList.RenameItems.Single(item =>
                string.Equals(item.Original.FullPath, oldFolder, StringComparison.Ordinal));
            var fileItem = renameList.RenameItems.Single(item =>
                string.Equals(item.Original.FullPath, sourceFile, StringComparison.Ordinal));
            // The Mover applies to every item, so the folder also gets DirectoryPath=newFolder.
            // Combined with the prefix replacer, the folder ends up at newFolder/SinkRenamed (a sub-folder).
            // The loose file should be under newFolder.
            Assert.Equal(newFolder, fileItem.Preview.DirectoryPath);
            Assert.DoesNotContain(renameList.RenameItems, item => item.PreviewError is not null);

            var result = renameList.Commit(failFast: false);
            Assert.Equal(2, result.Count(r => r.Status == RenameStatus.CommitOk));
            Assert.True(Directory.Exists(newFolder));
            Assert.True(File.Exists(newFolder.CombinePath("loose.txt")));
            Assert.False(File.Exists(sourceFile));
            Assert.False(Directory.Exists(oldFolder));
        }

        private static FilterPreset _CreatePresetAllEnabled(string name, params BaseFilter[] filters)
        {
            return new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = null,
                Chain = FilterChain.CreateAllEnabled(filters),
            };
        }

        private static ReplacerFilter _LiteralPrefixReplacer(string find, string replacement)
        {
            return new ReplacerFilter(
                Target: new FilePrefixTarget(),
                Options: new ReplacerOptions(
                    Find: find,
                    Replacement: replacement,
                    Mode: ReplacerMode.Literal,
                    CaseSensitive: true,
                    ReplaceAll: false,
                    WholeWord: false));
        }

        private static FilterPreset _FormatterFullPathPreset(string name, string fullPath)
        {
            return _CreatePresetAllEnabled(
                name,
                new FormatterFilter(
                    Target: new FullPathTarget(),
                    Options: new FormatterOptions(fullPath)));
        }

        private static void _SetupPreview(RenameList renameList, FilterPreset preset)
        {
            preset.Chain.SetupFilters();
            renameList.Preview(preset);
        }
    }
}

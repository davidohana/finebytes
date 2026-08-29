using Mfr.Models.RenameList.Fields.Basic;

namespace Mfr.Tests.Models
{
    /// <summary>
    /// Disk presence checks for Rename List rows.
    /// </summary>
    public sealed class RenameListDiskPathsTests : IDisposable
    {
        private readonly string _tempRoot;

        /// <summary>
        /// Initializes a temp directory for disk-presence tests.
        /// </summary>
        public RenameListDiskPathsTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "mfr-missing-" + Guid.NewGuid().ToString("N"));
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
        /// Verifies an existing file is not missing.
        /// </summary>
        [Fact]
        public void ExistsOnDisk_true_for_existing_file()
        {
            var path = Path.Combine(_tempRoot, "present.txt");
            File.WriteAllText(path, "x");

            var meta = _FileMeta(path);

            Assert.True(RenameListDiskPaths.ExistsOnDisk(meta));
            Assert.False(RenameListDiskPaths.IsMissingFromDisk(new RenameItem(meta)));
        }

        /// <summary>
        /// Verifies a deleted file path is missing.
        /// </summary>
        [Fact]
        public void IsMissingFromDisk_true_when_file_deleted()
        {
            var path = Path.Combine(_tempRoot, "gone.txt");
            File.WriteAllText(path, "x");
            var meta = _FileMeta(path);
            File.Delete(path);

            Assert.False(RenameListDiskPaths.ExistsOnDisk(meta));
            Assert.True(RenameListDiskPaths.IsMissingFromDisk(new RenameItem(meta)));
        }

        /// <summary>
        /// Verifies missing rows sort by field values and stay in normal order (not forced last).
        /// </summary>
        [Fact]
        public void CompareForSort_missing_rows_sort_by_field_not_forced_last()
        {
            var presentPath = Path.Combine(_tempRoot, "zzz.txt");
            File.WriteAllText(presentPath, "z");
            var missingPath = Path.Combine(_tempRoot, "aaa.txt");
            File.WriteAllText(missingPath, "a");
            var missingMeta = _FileMeta(missingPath);
            File.Delete(missingPath);

            var present = new RenameItem(_FileMeta(presentPath));
            var missing = new RenameItem(missingMeta);
            var key = RenameListFieldKey.Original(BasicRenameListField.Group, BasicRenameListFields.Key.FullName);

            Assert.True(RenameListFieldCatalog.CompareForSort(missing, key, present) < 0);
            Assert.True(RenameListFieldCatalog.CompareForSort(present, key, missing) > 0);
        }

        /// <summary>
        /// Verifies Show Load Errors includes missing rows.
        /// </summary>
        [Fact]
        public void ListLoadErrors_includes_missing_from_disk()
        {
            var path = Path.Combine(_tempRoot, "gone.txt");
            File.WriteAllText(path, "x");
            var meta = _FileMeta(path);
            File.Delete(path);

            var item = new RenameItem(meta);
            var error = Assert.Single(RenameListFieldCatalog.ListLoadErrors(item));

            Assert.Equal(RenameListDiskPaths.MissingUserExplanation, error.UserExplanation);
            Assert.Equal(meta.FullPath, error.TechnicalDetails);
            Assert.True(RenameListFieldCatalog.HasAnyLoadError(item));
        }

        /// <summary>
        /// Verifies missing rows do not use per-cell load-error styling.
        /// </summary>
        [Fact]
        public void HasLoadError_false_when_missing_even_with_stored_exception()
        {
            var path = Path.Combine(_tempRoot, "tagged.wav");
            File.WriteAllText(path, "not audio");
            var meta = _FileMeta(path);
            var item = new RenameItem(meta);
            item.SetTagLibMetadataLoadError(new IOException("failed"));
            File.Delete(path);

            var titleKey = RenameListFieldKey.Original("AudioTag", "Title");

            Assert.True(RenameListDiskPaths.IsMissingFromDisk(item));
            Assert.False(RenameListFieldCatalog.HasLoadError(item, titleKey));
        }

        private static FileMeta _FileMeta(string path)
        {
            var attrs = File.GetAttributes(path);
            var directoryPath = Path.GetDirectoryName(path) ?? string.Empty;
            return new FileMeta(
                renameListIndex: 0,
                inFolderIndex: 0,
                directoryPath: directoryPath,
                prefix: Path.GetFileNameWithoutExtension(path),
                extension: Path.GetExtension(path),
                attributes: attrs,
                fileSize: new FileInfo(path).Length
            );
        }
    }
}

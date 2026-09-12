using Mfr.App.Ui.Services.Shell;

namespace Mfr.Tests.Ui.Services.Shell
{
    /// <summary>
    /// Tests factory defaults and recording fake call shapes for <see cref="IFileShellOperations"/>.
    /// </summary>
    public sealed class FileShellOperationsTests
    {
        /// <summary>
        /// Verifies <see cref="FileShellOperations.CreateDefault"/> matches the host OS.
        /// </summary>
        [Fact]
        public void CreateDefault_Matches_Host_Os()
        {
            var ops = FileShellOperations.CreateDefault();

            if (OperatingSystem.IsWindows())
            {
                Assert.Equal(nameof(WindowsFileShellOperations), ops.GetType().Name);
                return;
            }

            Assert.Same(NullFileShellOperations.Instance, ops);
        }

        /// <summary>
        /// Verifies the recording fake captures delete paths and recycle flag.
        /// </summary>
        [Fact]
        public void Recording_Fake_Captures_Delete_Paths_And_Recycle_Flag()
        {
            var ops = new RecordingFileShellOperations();
            var paths = new[] { TestPaths.Absolute("a.txt"), TestPaths.Absolute("b") };
            var hwnd = new IntPtr(42);

            var result = ops.Delete(paths, recycle: true, hwnd);

            Assert.Equal(FileShellOperationResult.Succeeded, result);
            var call = Assert.Single(ops.Deletes);
            Assert.Equal(paths, call.Paths);
            Assert.True(call.Recycle);
            Assert.Equal(hwnd, call.OwnerHwnd);
            Assert.Empty(ops.Copies);
            Assert.Empty(ops.Moves);
        }

        /// <summary>
        /// Verifies the recording fake captures permanent delete when recycle is false.
        /// </summary>
        [Fact]
        public void Recording_Fake_Captures_Permanent_Delete()
        {
            var ops = new RecordingFileShellOperations();

            _ = ops.Delete([TestPaths.Absolute("gone.txt")], recycle: false);

            Assert.False(Assert.Single(ops.Deletes).Recycle);
        }

        /// <summary>
        /// Verifies the recording fake captures copy destination and paths.
        /// </summary>
        [Fact]
        public void Recording_Fake_Captures_Copy_Destination()
        {
            var ops = new RecordingFileShellOperations();
            var paths = new[] { TestPaths.Absolute("src\\one.txt") };
            var dest = TestPaths.Absolute("dest");

            var result = ops.Copy(paths, dest);

            Assert.Equal(FileShellOperationResult.Succeeded, result);
            var call = Assert.Single(ops.Copies);
            Assert.Equal(paths, call.Paths);
            Assert.Equal(dest, call.DestinationDirectory);
            Assert.Empty(ops.Deletes);
            Assert.Empty(ops.Moves);
        }

        /// <summary>
        /// Verifies the recording fake captures move destination and paths.
        /// </summary>
        [Fact]
        public void Recording_Fake_Captures_Move_Destination()
        {
            var ops = new RecordingFileShellOperations();
            var paths = new[] { TestPaths.Absolute("src\\two.txt") };
            var dest = TestPaths.Absolute("dest");

            var result = ops.Move(paths, dest, new IntPtr(7));

            Assert.Equal(FileShellOperationResult.Succeeded, result);
            var call = Assert.Single(ops.Moves);
            Assert.Equal(paths, call.Paths);
            Assert.Equal(dest, call.DestinationDirectory);
            Assert.Equal(new IntPtr(7), call.OwnerHwnd);
            Assert.Empty(ops.Deletes);
            Assert.Empty(ops.Copies);
        }

        /// <summary>
        /// Verifies the recording fake can return a non-success result.
        /// </summary>
        [Fact]
        public void Recording_Fake_Returns_Configured_Result()
        {
            var ops = new RecordingFileShellOperations { ResultToReturn = FileShellOperationResult.Cancelled };

            var result = ops.Delete([TestPaths.Absolute("x")], recycle: true);

            Assert.Equal(FileShellOperationResult.Cancelled, result);
        }

        /// <summary>
        /// Verifies the null implementation succeeds without recording side effects.
        /// </summary>
        [Fact]
        public void Null_Implementation_Succeeds_Without_Throwing()
        {
            var ops = NullFileShellOperations.Instance;

            Assert.Equal(FileShellOperationResult.Succeeded, ops.Delete([TestPaths.Absolute("a")], recycle: true));
            Assert.Equal(
                FileShellOperationResult.Succeeded,
                ops.Copy([TestPaths.Absolute("a")], TestPaths.Absolute("b"))
            );
            Assert.Equal(
                FileShellOperationResult.Succeeded,
                ops.Move([TestPaths.Absolute("a")], TestPaths.Absolute("b"))
            );
        }
    }
}

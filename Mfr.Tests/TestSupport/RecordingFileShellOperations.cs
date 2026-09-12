using Mfr.App.Ui.Services.Shell;

namespace Mfr.Tests.TestSupport
{
    /// <summary>
    /// Records <see cref="IFileShellOperations"/> calls for UI tests.
    /// </summary>
    public sealed class RecordingFileShellOperations : IFileShellOperations
    {
        /// <summary>
        /// Gets delete calls in order.
        /// </summary>
        public List<DeleteCall> Deletes { get; } = [];

        /// <summary>
        /// Gets copy calls in order.
        /// </summary>
        public List<CopyOrMoveCall> Copies { get; } = [];

        /// <summary>
        /// Gets move calls in order.
        /// </summary>
        public List<CopyOrMoveCall> Moves { get; } = [];

        /// <summary>
        /// Gets or sets the result returned from recorded calls.
        /// </summary>
        public FileShellOperationResult ResultToReturn { get; set; } = FileShellOperationResult.Succeeded;

        /// <inheritdoc />
        public FileShellOperationResult Delete(IReadOnlyList<string> paths, bool recycle, IntPtr ownerHwnd = default)
        {
            Deletes.Add(new DeleteCall([.. paths], recycle, ownerHwnd));
            return ResultToReturn;
        }

        /// <inheritdoc />
        public FileShellOperationResult Copy(
            IReadOnlyList<string> paths,
            string destinationDirectory,
            IntPtr ownerHwnd = default
        )
        {
            Copies.Add(new CopyOrMoveCall([.. paths], destinationDirectory, ownerHwnd));
            return ResultToReturn;
        }

        /// <inheritdoc />
        public FileShellOperationResult Move(
            IReadOnlyList<string> paths,
            string destinationDirectory,
            IntPtr ownerHwnd = default
        )
        {
            Moves.Add(new CopyOrMoveCall([.. paths], destinationDirectory, ownerHwnd));
            return ResultToReturn;
        }

        /// <summary>
        /// A recorded delete invocation.
        /// </summary>
        /// <param name="Paths">Paths passed to delete.</param>
        /// <param name="Recycle">Whether Recycle Bin was requested.</param>
        /// <param name="OwnerHwnd">Owner HWND passed through.</param>
        public sealed record DeleteCall(IReadOnlyList<string> Paths, bool Recycle, IntPtr OwnerHwnd);

        /// <summary>
        /// A recorded copy or move invocation.
        /// </summary>
        /// <param name="Paths">Source paths.</param>
        /// <param name="DestinationDirectory">Destination folder.</param>
        /// <param name="OwnerHwnd">Owner HWND passed through.</param>
        public sealed record CopyOrMoveCall(IReadOnlyList<string> Paths, string DestinationDirectory, IntPtr OwnerHwnd);
    }
}

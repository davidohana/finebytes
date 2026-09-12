using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Mfr.App.Ui.Services.Shell
{
    /// <summary>
    /// Deletes, copies, and moves paths via Windows <c>IFileOperation</c> with shell UI.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class WindowsFileShellOperations : IFileShellOperations
    {
        private const uint FofAllowUndo = 0x0040;
        private const uint FofWantNukeWarning = 0x4000;
        private const uint ClsCtxInprocServer = 1;
        private const uint CopyEngineUserCancelled = 0x80270000;
        private const uint ErrorCancelled = 0x800704C7;
        private static readonly Guid ClsidFileOperation = new("3AD05575-8857-4850-9277-11B85BDB8E09");
        private static readonly Guid IidFileOperation = new("947AAB5F-0A5C-4C13-B4D6-4BF7836FC9F8");
        private static readonly Guid IidShellItem = new("43826D1E-E718-42EE-BC55-A1E261C37BFE");

        /// <inheritdoc />
        public FileShellOperationResult Delete(IReadOnlyList<string> paths, bool recycle, IntPtr ownerHwnd = default)
        {
            if (paths.Count == 0)
            {
                return FileShellOperationResult.Succeeded;
            }

            return _Run(
                ownerHwnd,
                operationFlags: recycle ? FofAllowUndo : FofWantNukeWarning,
                destinationDirectory: null,
                (op, _) =>
                {
                    foreach (var path in paths)
                    {
                        var item = _CreateShellItem(path);
                        try
                        {
                            op.DeleteItem(item, IntPtr.Zero);
                        }
                        finally
                        {
                            _ReleaseCom(item);
                        }
                    }
                }
            );
        }

        /// <inheritdoc />
        public FileShellOperationResult Copy(
            IReadOnlyList<string> paths,
            string destinationDirectory,
            IntPtr ownerHwnd = default
        )
        {
            return _CopyOrMove(
                paths,
                destinationDirectory,
                ownerHwnd,
                operationFlags: 0,
                (op, item, dest) => op.CopyItem(item, dest, IntPtr.Zero, IntPtr.Zero)
            );
        }

        /// <inheritdoc />
        public FileShellOperationResult Move(
            IReadOnlyList<string> paths,
            string destinationDirectory,
            IntPtr ownerHwnd = default
        )
        {
            return _CopyOrMove(
                paths,
                destinationDirectory,
                ownerHwnd,
                operationFlags: FofAllowUndo,
                (op, item, dest) => op.MoveItem(item, dest, IntPtr.Zero, IntPtr.Zero)
            );
        }

        /// <summary>
        /// Shared copy/move path: empty/blank destination checks, then schedule + perform.
        /// </summary>
        private static FileShellOperationResult _CopyOrMove(
            IReadOnlyList<string> paths,
            string destinationDirectory,
            IntPtr ownerHwnd,
            uint operationFlags,
            Action<IFileOperation, IShellItem, IShellItem> enqueue
        )
        {
            if (paths.Count == 0)
            {
                return FileShellOperationResult.Succeeded;
            }

            if (string.IsNullOrWhiteSpace(destinationDirectory))
            {
                return FileShellOperationResult.Failed;
            }

            return _Run(
                ownerHwnd,
                operationFlags,
                destinationDirectory,
                (op, dest) =>
                {
                    foreach (var path in paths)
                    {
                        var item = _CreateShellItem(path);
                        try
                        {
                            enqueue(op, item, dest!);
                        }
                        finally
                        {
                            _ReleaseCom(item);
                        }
                    }
                }
            );
        }

        /// <summary>
        /// Creates <c>IFileOperation</c>, applies owner/flags/destination, runs <paramref name="schedule"/>, then performs.
        /// </summary>
        private static FileShellOperationResult _Run(
            IntPtr ownerHwnd,
            uint operationFlags,
            string? destinationDirectory,
            Action<IFileOperation, IShellItem?> schedule
        )
        {
            IFileOperation? operation = null;
            IShellItem? destination = null;
            try
            {
                operation = _CreateOperation();
                if (ownerHwnd != IntPtr.Zero)
                {
                    operation.SetOwnerWindow(ownerHwnd);
                }

                if (operationFlags != 0)
                {
                    operation.SetOperationFlags(operationFlags);
                }

                if (destinationDirectory is not null)
                {
                    destination = _CreateShellItem(destinationDirectory);
                }

                schedule(operation, destination);

                var hr = operation.PerformOperations();
                // MSDN: call after PerformOperations even when hr failed; cancel can still return S_OK.
                operation.GetAnyOperationsAborted(out var aborted);
                if (aborted || _IsCancelHresult(hr))
                {
                    return FileShellOperationResult.Cancelled;
                }

                if (hr < 0)
                {
                    return FileShellOperationResult.Failed;
                }

                return FileShellOperationResult.Succeeded;
            }
            catch (OperationCanceledException)
            {
                return FileShellOperationResult.Cancelled;
            }
            catch (COMException ex) when (_IsCancelHresult(ex.ErrorCode))
            {
                return FileShellOperationResult.Cancelled;
            }
            catch (Exception)
            {
                return FileShellOperationResult.Failed;
            }
            finally
            {
                _ReleaseCom(destination);
                _ReleaseCom(operation);
            }
        }

        /// <summary>
        /// CoCreates the in-proc FileOperation COM object.
        /// </summary>
        private static IFileOperation _CreateOperation()
        {
            var clsid = ClsidFileOperation;
            var iid = IidFileOperation;
            var hr = NativeMethods.CoCreateInstance(ref clsid, IntPtr.Zero, ClsCtxInprocServer, ref iid, out var unk);
            if (hr < 0 || unk is null)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return (IFileOperation)unk!;
        }

        /// <summary>
        /// Builds an <c>IShellItem</c> from a filesystem parsing name.
        /// </summary>
        private static IShellItem _CreateShellItem(string path)
        {
            var iid = IidShellItem;
            NativeMethods.SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out var item);
            return item ?? throw new InvalidOperationException($"Could not create shell item for '{path}'.");
        }

        /// <summary>
        /// True for COPYENGINE_E_USER_CANCELLED / ERROR_CANCELLED family HRESULTs.
        /// </summary>
        private static bool _IsCancelHresult(int hr)
        {
            return unchecked((uint)hr) is CopyEngineUserCancelled or ErrorCancelled;
        }

        /// <summary>
        /// Best-effort <see cref="Marshal.ReleaseComObject"/>; ignores release failures.
        /// </summary>
        private static void _ReleaseCom(object? comObject)
        {
            if (comObject is null)
            {
                return;
            }

            try
            {
                _ = Marshal.ReleaseComObject(comObject);
            }
            catch (Exception)
            {
                // Best-effort COM release.
            }
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem { }

        [ComImport]
        [Guid("947AAB5F-0A5C-4C13-B4D6-4BF7836FC9F8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOperation
        {
            void Advise(IntPtr pfops, out uint cookie);

            void Unadvise(uint cookie);

            void SetOperationFlags(uint operationFlags);

            void SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string message);

            void SetProgressDialog(IntPtr popd);

            void SetProperties(IntPtr pproparray);

            void SetOwnerWindow(IntPtr hwndParent);

            void ApplyPropertiesToItem(IShellItem psiItem);

            void ApplyPropertiesToItems(IntPtr punkItems);

            void RenameItem(IShellItem psiItem, [MarshalAs(UnmanagedType.LPWStr)] string newName, IntPtr pfopsItem);

            void RenameItems(IntPtr punkItems, [MarshalAs(UnmanagedType.LPWStr)] string newName);

            void MoveItem(IShellItem psiItem, IShellItem psiDestinationFolder, IntPtr newName, IntPtr pfopsItem);

            void MoveItems(IntPtr punkItems, IShellItem psiDestinationFolder);

            void CopyItem(IShellItem psiItem, IShellItem psiDestinationFolder, IntPtr copyName, IntPtr pfopsItem);

            void CopyItems(IntPtr punkItems, IShellItem psiDestinationFolder);

            void DeleteItem(IShellItem psiItem, IntPtr pfopsItem);

            void DeleteItems(IntPtr punkItems);

            void NewItem(
                IShellItem psiDestinationFolder,
                uint fileAttributes,
                [MarshalAs(UnmanagedType.LPWStr)] string name,
                [MarshalAs(UnmanagedType.LPWStr)] string templateName,
                IntPtr pfopsItem
            );

            [PreserveSig]
            int PerformOperations();

            void GetAnyOperationsAborted([MarshalAs(UnmanagedType.Bool)] out bool anyOperationsAborted);
        }

        private static class NativeMethods
        {
            [DllImport("ole32.dll")]
            public static extern int CoCreateInstance(
                ref Guid rclsid,
                IntPtr pUnkOuter,
                uint dwClsContext,
                ref Guid riid,
                [MarshalAs(UnmanagedType.Interface)] out object? ppv
            );

            [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
            public static extern void SHCreateItemFromParsingName(
                [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
                IntPtr pbc,
                ref Guid riid,
                [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv
            );
        }
    }
}

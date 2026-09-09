using Avalonia.Input;
using Mfr.App.Ui.Views.DragAndDrop;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Drag payload kind for Rename List field shuttle lists.
    /// </summary>
    internal enum ShuttleDragKind
    {
        /// <summary>Available catalog field dragged from the left list.</summary>
        AvailableField,

        /// <summary>Selected visible column dragged from the right Columns list.</summary>
        SelectedColumn,

        /// <summary>Selected sort key dragged from the right Sort list.</summary>
        SelectedSort,
    }

    /// <summary>
    /// Drag-and-drop payload for shuttle list operations.
    /// </summary>
    /// <param name="Kind">Which shuttle list initiated the drag.</param>
    /// <param name="Keys">Ordered encoded field keys being dragged.</param>
    internal sealed record ShuttleDragPayload(ShuttleDragKind Kind, IReadOnlyList<string> Keys)
    {
        /// <summary>
        /// Avalonia data format for shuttle drag payloads.
        /// </summary>
        public static readonly DataFormat<string> Format = JsonDragPayload.CreateFormat("Mfr.ShuttleDragPayload");

        /// <summary>
        /// Serializes the payload for drag transport.
        /// </summary>
        /// <returns>JSON payload string.</returns>
        public string Serialize()
        {
            return JsonDragPayload.Serialize(this);
        }

        /// <summary>
        /// Builds a transfer containing only this payload.
        /// </summary>
        /// <returns>Transfer ready for <see cref="DragDrop.DoDragDropAsync"/>.</returns>
        public DataTransfer CreateTransfer()
        {
            return JsonDragPayload.CreateTransfer(Format, this);
        }

        /// <summary>
        /// Appends this payload to an existing <paramref name="dataTransfer"/>.
        /// </summary>
        /// <param name="dataTransfer">Transfer to extend.</param>
        public void AddTo(DataTransfer dataTransfer)
        {
            JsonDragPayload.Add(dataTransfer, Format, this);
        }

        /// <summary>
        /// Reads a shuttle drag payload from drag data.
        /// </summary>
        /// <param name="dataTransfer">Drag data, or <see langword="null"/>.</param>
        /// <returns>Payload when present; otherwise <see langword="null"/>.</returns>
        public static ShuttleDragPayload? TryRead(IDataTransfer? dataTransfer)
        {
            return JsonDragPayload.TryRead<ShuttleDragPayload>(dataTransfer, Format);
        }
    }
}

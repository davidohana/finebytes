using System.Text.Json;
using Avalonia.Input;

namespace Mfr.App.Ui.Views.DragAndDrop
{
    /// <summary>
    /// JSON serialize/deserialize helpers for in-process Avalonia drag payloads.
    /// </summary>
    internal static class JsonDragPayload
    {
        private static readonly JsonSerializerOptions _JsonOptions = new() { WriteIndented = false };

        /// <summary>
        /// Creates a string application data format for a payload type.
        /// </summary>
        /// <param name="name">Unique format name (e.g. <c>Mfr.ShuttleDragPayload</c>).</param>
        /// <returns>Avalonia data format for the payload JSON.</returns>
        public static DataFormat<string> CreateFormat(string name)
        {
            return DataFormat.CreateStringApplicationFormat(name);
        }

        /// <summary>
        /// Serializes <paramref name="payload"/> for drag transport.
        /// </summary>
        /// <typeparam name="T">Payload record type.</typeparam>
        /// <param name="payload">Value to serialize.</param>
        /// <returns>JSON payload string.</returns>
        public static string Serialize<T>(T payload)
        {
            return JsonSerializer.Serialize(payload, _JsonOptions);
        }

        /// <summary>
        /// Deserializes a drag payload from transport JSON.
        /// </summary>
        /// <typeparam name="T">Payload record type.</typeparam>
        /// <param name="json">Serialized payload.</param>
        /// <returns>Payload when valid; otherwise <see langword="null"/>.</returns>
        public static T? Deserialize<T>(string? json)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(json, _JsonOptions);
        }

        /// <summary>
        /// Reads the first item matching <paramref name="format"/> from a drag transfer.
        /// </summary>
        /// <typeparam name="T">Payload record type.</typeparam>
        /// <param name="dataTransfer">Drag data, or <see langword="null"/>.</param>
        /// <param name="format">Payload data format.</param>
        /// <returns>Deserialized payload when present; otherwise <see langword="null"/>.</returns>
        public static T? TryRead<T>(IDataTransfer? dataTransfer, DataFormat<string> format)
            where T : class
        {
            if (dataTransfer is null)
            {
                return null;
            }

            foreach (var item in dataTransfer.Items)
            {
                if (item.TryGetRaw(format) is string json)
                {
                    return Deserialize<T>(json);
                }
            }

            return null;
        }

        /// <summary>
        /// Builds a <see cref="DataTransfer"/> containing one serialized payload item.
        /// </summary>
        /// <typeparam name="T">Payload record type.</typeparam>
        /// <param name="format">Payload data format.</param>
        /// <param name="payload">Value to serialize.</param>
        /// <returns>Transfer ready for <see cref="DragDrop.DoDragDropAsync"/>.</returns>
        public static DataTransfer CreateTransfer<T>(DataFormat<string> format, T payload)
        {
            var dataTransfer = new DataTransfer();
            dataTransfer.Add(DataTransferItem.Create(format, Serialize(payload)));
            return dataTransfer;
        }
    }
}

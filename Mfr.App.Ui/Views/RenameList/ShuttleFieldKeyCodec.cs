using System.Text.Json;
using Mfr.Models.RenameList;

namespace Mfr.App.Ui.Views.RenameList
{
    /// <summary>
    /// Encodes <see cref="RenameListFieldKey"/> values for shuttle drag-and-drop payloads.
    /// </summary>
    internal static class ShuttleFieldKeyCodec
    {
        private static readonly JsonSerializerOptions _JsonOptions = new() { WriteIndented = false };

        /// <summary>
        /// Encodes a field key for drag payload transport.
        /// </summary>
        /// <param name="key">Field key to encode.</param>
        /// <returns>Serialized key string.</returns>
        public static string Encode(RenameListFieldKey key)
        {
            return JsonSerializer.Serialize(key, _JsonOptions);
        }

        /// <summary>
        /// Decodes a field key from drag payload transport.
        /// </summary>
        /// <param name="encoded">Serialized key string.</param>
        /// <returns>Decoded field key when valid; otherwise <see langword="null"/>.</returns>
        public static RenameListFieldKey? Decode(string encoded)
        {
            return JsonSerializer.Deserialize<RenameListFieldKey>(encoded, _JsonOptions);
        }
    }
}

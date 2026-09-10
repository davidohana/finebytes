namespace Mfr.Utils
{
    /// <summary>
    /// Shared AppData file helpers used by config, session, and filter-defaults stores.
    /// </summary>
    public static class AppDataFile
    {
        /// <summary>
        /// Deletes <paramref name="path"/> when the file exists.
        /// <para>Missing files are a no-op.</para>
        /// </summary>
        /// <param name="path">Filesystem path to delete.</param>
        /// <param name="fileDescription">
        /// Human-readable file kind used in the failure message (e.g. <c>configuration file</c>).
        /// </param>
        /// <exception cref="IOException">Thrown when the file exists but cannot be deleted.</exception>
        public static void DeleteFileIfExists(string path, string fileDescription)
        {
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                throw new IOException($"Error deleting {fileDescription} '{path}'.", ex);
            }
        }
    }
}

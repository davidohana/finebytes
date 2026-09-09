namespace Mfr.Tests.Architecture
{
    /// <summary>
    /// Shared filesystem helpers for architecture guardrail tests.
    /// </summary>
    internal static class ArchitectureRepoPaths
    {
        /// <summary>
        /// Walks parents of the current directory until <c>finebytes.slnx</c> is found.
        /// </summary>
        /// <returns>Absolute path to the repository root.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the solution file cannot be located.</exception>
        internal static string FindRepoRoot()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (directory is not null)
            {
                var solutionPath = Path.Combine(directory.FullName, "finebytes.slnx");
                if (File.Exists(solutionPath))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Could not locate repository root containing finebytes.slnx.");
        }
    }
}

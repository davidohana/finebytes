namespace Mfr.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="SessionStore"/>.
    /// </summary>
    public sealed class SessionStoreTests
    {
        [Fact]
        public void Load_missing_file_returns_empty_session()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-missing-" + Guid.NewGuid() + ".json");
            var session = SessionStore.Load(path);
            Assert.Equal(1, session.Version);
            Assert.Null(session.LastOpenedDirectory);
            Assert.Null(session.Window);
        }

        [Fact]
        public void Load_corrupt_json_returns_empty_session()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-bad-" + Guid.NewGuid() + ".json");
            try
            {
                File.WriteAllText(path, "{ not-json");
                var session = SessionStore.Load(path);
                Assert.Null(session.LastOpenedDirectory);
                Assert.Null(session.Window);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Save_and_Load_round_trip()
        {
            var path = Path.Combine(Path.GetTempPath(), "mfr-session-round-" + Guid.NewGuid() + ".json");
            try
            {
                var original = new SessionState
                {
                    Version = 1,
                    LastOpenedDirectory = Path.Combine(Path.GetTempPath(), "music"),
                    Window = new SessionWindowState
                    {
                        X = 12,
                        Y = 34,
                        Width = 1100,
                        Height = 720,
                        State = "Maximized",
                    },
                };

                SessionStore.Save(original, path);
                var loaded = SessionStore.Load(path);

                Assert.Equal(1, loaded.Version);
                Assert.Equal(original.LastOpenedDirectory, loaded.LastOpenedDirectory);
                Assert.NotNull(loaded.Window);
                Assert.Equal(12, loaded.Window.X);
                Assert.Equal(34, loaded.Window.Y);
                Assert.Equal(1100, loaded.Window.Width);
                Assert.Equal(720, loaded.Window.Height);
                Assert.Equal("Maximized", loaded.Window.State);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}

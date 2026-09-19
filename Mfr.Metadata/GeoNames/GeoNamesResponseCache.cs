using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mfr.Models;
using Mfr.Utils;

namespace Mfr.Metadata.GeoNames
{
    /// <summary>
    /// Process (L2) and disk (L3) cache for successful GeoNames <c>findNearby</c> results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Failures are never persisted. Soft cap is ~5 000 L3 entries. Circuit-breaker trip state
    /// lives here so Options username changes can clear L2 + breaker together.
    /// </para>
    /// </remarks>
    public sealed class GeoNamesResponseCache
    {
        /// <summary>
        /// Soft maximum number of L3 disk entries retained.
        /// </summary>
        public const int SoftCap = 5000;

        private static readonly JsonSerializerOptions s_JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
        };

        private readonly ConcurrentDictionary<string, GeoNamesInfo> _processCache = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, string> _trippedUsernameToMessage = new(StringComparer.Ordinal);
        private readonly Lock _diskGate = new();
        private readonly string _cacheFilePath;
        private Dictionary<string, DiskEntry>? _diskEntries;

        /// <summary>
        /// Initializes a cache that reads/writes <paramref name="cacheFilePath"/>.
        /// </summary>
        /// <param name="cacheFilePath">Absolute path to <c>geonames-cache.json</c>.</param>
        public GeoNamesResponseCache(string cacheFilePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cacheFilePath);
            _cacheFilePath = cacheFilePath;
        }

        /// <summary>
        /// Default L3 path under <see cref="AppDataPaths.LocalRoot"/>.
        /// </summary>
        public static string DefaultCacheFilePath => AppDataPaths.LocalRoot().CombinePath("geonames-cache.json");

        /// <summary>
        /// Tries to read a successful snapshot from L2 then L3.
        /// </summary>
        /// <param name="cacheKey"><see cref="GeoNamesCacheKey"/> string.</param>
        /// <param name="info">Cached snapshot when found.</param>
        /// <returns><see langword="true"/> when a success entry exists.</returns>
        public bool TryGet(string cacheKey, out GeoNamesInfo info)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

            if (_processCache.TryGetValue(cacheKey, out info!))
            {
                return true;
            }

            lock (_diskGate)
            {
                _EnsureDiskLoaded();
                if (_diskEntries!.TryGetValue(cacheKey, out var entry))
                {
                    entry.LastAccessUtc = DateTime.UtcNow;
                    info = entry.ToInfo();
                    _processCache[cacheKey] = info;
                    _SaveDiskUnlocked();
                    return true;
                }
            }

            info = null!;
            return false;
        }

        /// <summary>
        /// Stores a successful lookup in L2 and L3.
        /// </summary>
        /// <param name="cacheKey"><see cref="GeoNamesCacheKey"/> string.</param>
        /// <param name="info">Successful snapshot (may have blank fields).</param>
        public void Put(string cacheKey, GeoNamesInfo info)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
            ArgumentNullException.ThrowIfNull(info);

            _processCache[cacheKey] = info;

            lock (_diskGate)
            {
                _EnsureDiskLoaded();
                _diskEntries![cacheKey] = DiskEntry.FromInfo(info, DateTime.UtcNow);
                _TrimIfNeededUnlocked();
                _SaveDiskUnlocked();
            }
        }

        /// <summary>
        /// Returns whether the circuit breaker is tripped for <paramref name="effectiveUsername"/>.
        /// </summary>
        /// <param name="effectiveUsername">Resolved GeoNames username.</param>
        /// <param name="message">Stored rate-limit message when tripped.</param>
        /// <returns><see langword="true"/> when further HTTP should be skipped.</returns>
        public bool TryGetTrippedMessage(string effectiveUsername, out string message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(effectiveUsername);
            return _trippedUsernameToMessage.TryGetValue(effectiveUsername.Trim(), out message!);
        }

        /// <summary>
        /// Trips the circuit breaker for <paramref name="effectiveUsername"/>.
        /// </summary>
        /// <param name="effectiveUsername">Resolved GeoNames username.</param>
        /// <param name="message">User-facing rate-limit message reused for later misses.</param>
        public void Trip(string effectiveUsername, string message)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(effectiveUsername);
            ArgumentException.ThrowIfNullOrWhiteSpace(message);
            _trippedUsernameToMessage[effectiveUsername.Trim()] = message;
        }

        /// <summary>
        /// Clears L2 process entries and circuit-breaker trips (not L3 disk).
        /// </summary>
        public void ClearProcessState()
        {
            _processCache.Clear();
            _trippedUsernameToMessage.Clear();
        }

        private void _EnsureDiskLoaded()
        {
            if (_diskEntries is not null)
            {
                return;
            }

            _diskEntries = new Dictionary<string, DiskEntry>(StringComparer.Ordinal);
            if (!File.Exists(_cacheFilePath))
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(_cacheFilePath);
                var file = JsonSerializer.Deserialize<DiskFile>(json, s_JsonOptions);
                if (file?.Entries is null)
                {
                    return;
                }

                foreach (var entry in file.Entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.Key))
                    {
                        continue;
                    }

                    _diskEntries[entry.Key] = entry;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                _diskEntries = new Dictionary<string, DiskEntry>(StringComparer.Ordinal);
            }
        }

        private void _TrimIfNeededUnlocked()
        {
            if (_diskEntries is null || _diskEntries.Count <= SoftCap)
            {
                return;
            }

            var overflow = _diskEntries.Count - SoftCap;
            foreach (
                var key in _diskEntries
                    .OrderBy(pair => pair.Value.LastAccessUtc)
                    .Take(overflow)
                    .Select(pair => pair.Key)
                    .ToList()
            )
            {
                _diskEntries.Remove(key);
            }
        }

        private void _SaveDiskUnlocked()
        {
            if (_diskEntries is null)
            {
                return;
            }

            var directory = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var file = new DiskFile { Entries = [.. _diskEntries.Select(pair => pair.Value with { Key = pair.Key })] };
            var json = JsonSerializer.Serialize(file, s_JsonOptions);
            File.WriteAllText(_cacheFilePath, json);
        }

        private sealed class DiskFile
        {
            public List<DiskEntry> Entries { get; set; } = [];
        }

        private sealed record DiskEntry
        {
            public string Key { get; set; } = string.Empty;

            public string? Place { get; set; }

            public string? Region { get; set; }

            public string? Country { get; set; }

            public DateTime LastAccessUtc { get; set; }

            public GeoNamesInfo ToInfo()
            {
                return new GeoNamesInfo
                {
                    Place = Place,
                    Region = Region,
                    Country = Country,
                };
            }

            public static DiskEntry FromInfo(GeoNamesInfo info, DateTime accessedUtc)
            {
                return new DiskEntry
                {
                    Place = info.Place,
                    Region = info.Region,
                    Country = info.Country,
                    LastAccessUtc = accessedUtc,
                };
            }
        }
    }
}

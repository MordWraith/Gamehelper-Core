namespace Shared.UpdateSecurity
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public sealed class PluginVersionEntry
    {
        [JsonProperty("commitSha")]
        public string CommitSha { get; set; } = string.Empty;

        [JsonProperty("installedAt")]
        public string InstalledAt { get; set; } = string.Empty;
    }

    public static class PluginVersionStore
    {
        private const string FileName = "plugin-versions.json";

        public static Dictionary<string, PluginVersionEntry> Load(string installDir)
        {
            var path = Path.Combine(installDir, "configs", FileName);
            if (!File.Exists(path))
            {
                return new Dictionary<string, PluginVersionEntry>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                var json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<Dictionary<string, PluginVersionEntry>>(json)
                    ?? new Dictionary<string, PluginVersionEntry>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, PluginVersionEntry>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public static void Save(string installDir, Dictionary<string, PluginVersionEntry> entries)
        {
            var dir = Path.Combine(installDir, "configs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, FileName);
            File.WriteAllText(path, JsonConvert.SerializeObject(entries, Formatting.Indented));
        }

        public static void SetPlugin(string installDir, string pluginName, string commitSha)
        {
            var entries = Load(installDir);
            entries[pluginName] = new PluginVersionEntry
            {
                CommitSha = commitSha,
                InstalledAt = DateTime.UtcNow.ToString("o"),
            };
            Save(installDir, entries);
        }

        public static string? GetInstalledSha(string installDir, string pluginName)
        {
            var entries = Load(installDir);
            return entries.TryGetValue(pluginName, out var entry)
                ? entry.CommitSha
                : null;
        }
    }
}

namespace GameHelper.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     Resolves upstream GitHub URLs for bundled plugins (from scripts/plugins-sources.json at build time
    ///     or plugins-sources.json next to the core assembly in dev trees).
    /// </summary>
    internal static class PluginSources
    {
        private static readonly Lazy<IReadOnlyDictionary<string, string>> UrlsLazy = new(LoadUrls);

        internal static string GetSourceUrl(string pluginName)
        {
            return UrlsLazy.Value.TryGetValue(pluginName, out var url) ? url : string.Empty;
        }

        private static IReadOnlyDictionary<string, string> LoadUrls()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in GetCandidatePaths())
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    var json = JObject.Parse(File.ReadAllText(path));
                    MergeSection(result, json["mordWraithForks"]);
                    MergeSection(result, json["upstream"]);
                    return result;
                }
                catch
                {
                    // try next path
                }
            }

            return result;
        }

        private static IEnumerable<string> GetCandidatePaths()
        {
            yield return Path.Combine(AppContext.BaseDirectory, "plugins-sources.json");
            yield return Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "scripts", "plugins-sources.json");
            yield return Path.Combine(AppContext.BaseDirectory, "..", "..", "scripts", "plugins-sources.json");
        }

        private static void MergeSection(Dictionary<string, string> target, JToken? section)
        {
            if (section is not JObject obj)
            {
                return;
            }

            foreach (var prop in obj.Properties())
            {
                var url = prop.Value?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    target[prop.Name] = url;
                }
            }
        }
    }
}

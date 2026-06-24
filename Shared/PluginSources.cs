namespace Shared.UpdateSecurity
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json.Linq;

    public sealed record PluginSource(string Name, string GitHubRepo)
    {
        public string Owner => this.GitHubRepo.Split('/')[0];
        public string RepoName => this.GitHubRepo.Split('/')[1];
        public string CloneUrl => $"https://github.com/{this.GitHubRepo}.git";
    }

    public static class PluginSourcesReader
    {
        public const string FileName = "plugins-sources.json";

        // Reads plugins-sources.json from the given path.
        // Groups (mordWraithForks, upstream, etc.) are flattened into a single list.
        public static IReadOnlyList<PluginSource> Load(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                return Array.Empty<PluginSource>();
            }

            try
            {
                var root = JObject.Parse(File.ReadAllText(jsonPath));
                var result = new List<PluginSource>();
                foreach (var section in root.Properties())
                {
                    if (section.Value is not JObject plugins)
                    {
                        continue;
                    }

                    foreach (var plugin in plugins.Properties())
                    {
                        var url = plugin.Value.ToString().Trim().TrimEnd('/');
                        var repo = ExtractGitHubRepo(url);
                        if (repo != null)
                        {
                            result.Add(new PluginSource(plugin.Name, repo));
                        }
                    }
                }

                return result;
            }
            catch
            {
                return Array.Empty<PluginSource>();
            }
        }

        // Tries to find and load plugins-sources.json from the install directory.
        public static IReadOnlyList<PluginSource> LoadFromInstallDir(string installDir)
        {
            var path = Path.Combine(installDir, FileName);
            return Load(path);
        }

        private static string? ExtractGitHubRepo(string url)
        {
            const string prefix = "https://github.com/";
            if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var repo = url.Substring(prefix.Length).Trim('/');
            var parts = repo.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 2 ? $"{parts[0]}/{parts[1]}" : null;
        }
    }
}

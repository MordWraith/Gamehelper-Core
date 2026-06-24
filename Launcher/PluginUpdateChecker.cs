namespace Launcher
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Shared.UpdateSecurity;

    internal sealed class PluginUpdateInfo
    {
        public required string Name { get; init; }
        public string? InstalledSha { get; init; }
        public string? LatestSha { get; init; }
        public bool IsUpToDate { get; init; }
        public bool IsNotInstalled => this.InstalledSha == null;
    }

    internal static class PluginUpdateChecker
    {
        // Checks all plugins from plugins-sources.json in the install dir.
        // Returns infos for all plugins where an update is available (or not installed).
        internal static async Task<IReadOnlyList<PluginUpdateInfo>> CheckAsync(
            string installDir,
            CancellationToken cancellationToken = default)
        {
            var sources = PluginSourcesReader.LoadFromInstallDir(installDir);
            if (sources.Count == 0)
            {
                return Array.Empty<PluginUpdateInfo>();
            }

            var tasks = sources.Select(async source =>
            {
                var latestSha = await GitHubApiHelper.GetLatestCommitShaAsync(
                    source.Owner, source.RepoName, cancellationToken).ConfigureAwait(false);

                var installedSha = PluginVersionStore.GetInstalledSha(installDir, source.Name);
                var upToDate = latestSha != null && GitHubApiHelper.ShaMatches(installedSha, latestSha);

                return new PluginUpdateInfo
                {
                    Name = source.Name,
                    InstalledSha = installedSha,
                    LatestSha = latestSha,
                    IsUpToDate = upToDate,
                };
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return results.Where(r => !r.IsUpToDate && r.LatestSha != null).ToList();
        }

        // Returns the path to GameHelperDownloader.exe if found next to the GameHelper install.
        internal static string? FindDownloaderExe(string installDir)
        {
            var candidates = new[]
            {
                Path.Combine(installDir, "GameHelperDownloader.exe"),
                Path.Combine(AppContext.BaseDirectory, "GameHelperDownloader.exe"),
            };
            return candidates.FirstOrDefault(File.Exists);
        }
    }
}

namespace Shared.UpdateSecurity
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    public static class GitHubApiHelper
    {
        private static readonly HttpClient HttpClient = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GameHelper-Updater/1.0");
            return client;
        }

        // Returns the latest commit SHA on the default branch, or null on failure.
        public static async Task<string?> GetLatestCommitShaAsync(
            string owner,
            string repo,
            CancellationToken cancellationToken = default)
        {
            // /commits?per_page=1 returns the latest commit on the default branch.
            var url = $"https://api.github.com/repos/{owner}/{repo}/commits?per_page=1";
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var array = JArray.Parse(json);
                return array.Count > 0 ? array[0]["sha"]?.ToString() : null;
            }
            catch
            {
                return null;
            }
        }

        // Returns the short (7-char) form of a commit SHA for display.
        public static string ShortSha(string? sha) =>
            sha is { Length: >= 7 } ? sha[..7] : (sha ?? "unknown");

        // True if the two SHAs refer to the same commit (full or prefix match).
        public static bool ShaMatches(string? installed, string? remote)
        {
            if (string.IsNullOrEmpty(installed) || string.IsNullOrEmpty(remote))
            {
                return false;
            }

            var a = installed.ToLowerInvariant();
            var b = remote.ToLowerInvariant();
            return a == b || a.StartsWith(b, StringComparison.Ordinal) || b.StartsWith(a, StringComparison.Ordinal);
        }
    }
}

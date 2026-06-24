namespace Shared.UpdateSecurity
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class PluginBuildResult
    {
        public bool Success { get; init; }
        public string? CommitSha { get; init; }
        public string? ErrorMessage { get; init; }

        public static PluginBuildResult Ok(string commitSha) =>
            new() { Success = true, CommitSha = commitSha };

        public static PluginBuildResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };
    }

    public static class PluginBuildHelper
    {
        // Checks whether the .NET SDK (dotnet CLI) is available on PATH.
        public static bool IsDotNetSdkAvailable()
        {
            try
            {
                var result = RunProcess("dotnet", "--version", null, timeout: 8000);
                return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output);
            }
            catch
            {
                return false;
            }
        }

        // Checks whether git is available on PATH.
        public static bool IsGitAvailable()
        {
            try
            {
                var result = RunProcess("git", "--version", null, timeout: 8000);
                return result.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        // Clones the plugin repo, builds it, copies output to installDir\Plugins\{name}\,
        // and records the installed commit SHA. Progress lines are reported via IProgress.
        public static async Task<PluginBuildResult> BuildAndInstallAsync(
            PluginSource source,
            string installDir,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "GameHelperPluginBuild");
            var tempDir = Path.Combine(tempRoot, source.Name);

            try
            {
                // Clean any previous temp clone.
                if (Directory.Exists(tempDir))
                {
                    progress?.Report($"  Cleaning temp directory ...");
                    await Task.Run(() => Directory.Delete(tempDir, true), cancellationToken).ConfigureAwait(false);
                }

                Directory.CreateDirectory(tempRoot);

                // Clone.
                progress?.Report($"  Cloning {source.GitHubRepo} ...");
                var cloneResult = await RunProcessAsync(
                    "git", $"clone --depth 1 {source.CloneUrl} \"{tempDir}\"",
                    workDir: null, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (cloneResult.ExitCode != 0)
                {
                    return PluginBuildResult.Fail(
                        $"git clone failed: {cloneResult.ErrorOutput.Trim()}");
                }

                // Get commit SHA.
                var shaResult = await RunProcessAsync(
                    "git", "rev-parse HEAD",
                    workDir: tempDir, cancellationToken: cancellationToken).ConfigureAwait(false);

                var commitSha = shaResult.ExitCode == 0
                    ? shaResult.Output.Trim()
                    : "unknown";

                // Find the .csproj file for this plugin.
                var csproj = FindPluginCsproj(tempDir, source.Name);
                if (csproj == null)
                {
                    return PluginBuildResult.Fail(
                        $"No .csproj found for {source.Name} in cloned repository.");
                }

                var csprojDir = Path.GetDirectoryName(csproj)!;
                progress?.Report($"  Building {source.Name} ...");

                var buildResult = await RunProcessAsync(
                    "dotnet",
                    $"build \"{csproj}\" -c Release --nologo -v quiet",
                    workDir: csprojDir,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (buildResult.ExitCode != 0)
                {
                    var err = buildResult.ErrorOutput.Trim();
                    if (string.IsNullOrWhiteSpace(err))
                    {
                        err = buildResult.Output.Trim();
                    }

                    return PluginBuildResult.Fail($"Build failed:\n{err}");
                }

                // Find the build output folder.
                var outputDir = FindBuildOutput(csprojDir);
                if (outputDir == null)
                {
                    return PluginBuildResult.Fail(
                        $"Build output not found under {csprojDir}\\bin\\Release\\");
                }

                // Copy to install dir.
                var destDir = Path.Combine(installDir, "Plugins", source.Name);
                Directory.CreateDirectory(destDir);
                progress?.Report($"  Installing {source.Name} ...");
                CopyPluginOutput(outputDir, destDir, source.Name);

                // Record installed SHA.
                PluginVersionStore.SetPlugin(installDir, source.Name, commitSha);

                progress?.Report($"  {source.Name} installed (commit {GitHubApiHelper.ShortSha(commitSha)}).");
                return PluginBuildResult.Ok(commitSha);
            }
            catch (OperationCanceledException)
            {
                return PluginBuildResult.Fail("Cancelled.");
            }
            catch (Exception ex)
            {
                return PluginBuildResult.Fail(ex.Message);
            }
            finally
            {
                // Best-effort cleanup.
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch { }
            }
        }

        // Looks for {name}.csproj anywhere under tempDir (handles repos with subdirectories).
        private static string? FindPluginCsproj(string tempDir, string pluginName)
        {
            // Prefer exact name match first.
            var exact = Directory.GetFiles(tempDir, $"{pluginName}.csproj", SearchOption.AllDirectories);
            if (exact.Length > 0)
            {
                return exact[0];
            }

            // Fall back to any .csproj.
            var any = Directory.GetFiles(tempDir, "*.csproj", SearchOption.AllDirectories);
            return any.Length > 0 ? any[0] : null;
        }

        // Finds the Release build output under the csproj directory.
        // Expected: bin\Release\net*\win-x64\ or bin\Release\net*\
        private static string? FindBuildOutput(string csprojDir)
        {
            var releaseDir = Path.Combine(csprojDir, "bin", "Release");
            if (!Directory.Exists(releaseDir))
            {
                return null;
            }

            // Prefer win-x64 subfolder.
            foreach (var tfm in Directory.GetDirectories(releaseDir))
            {
                var winX64 = Path.Combine(tfm, "win-x64");
                if (Directory.Exists(winX64) &&
                    Directory.GetFiles(winX64, "*.dll").Length > 0)
                {
                    return winX64;
                }
            }

            // Fallback: any TFM folder with DLLs directly.
            foreach (var tfm in Directory.GetDirectories(releaseDir))
            {
                if (Directory.GetFiles(tfm, "*.dll").Length > 0)
                {
                    return tfm;
                }
            }

            return null;
        }

        // Copies plugin output to destDir, preserving config/ subfolders (user settings).
        private static void CopyPluginOutput(string sourceDir, string destDir, string pluginName)
        {
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(sourceDir, file);
                var parts = relative.Split(Path.DirectorySeparatorChar);

                // Skip debug symbols.
                if (file.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip user config folders.
                if (parts.Length > 1 &&
                    (parts[0].Equals("config", StringComparison.OrdinalIgnoreCase) ||
                     parts[0].Equals("configs", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                // Skip runtime host executables that belong to the framework, not the plugin.
                if (parts.Length == 1 && file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                    !Path.GetFileNameWithoutExtension(file).Equals(pluginName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var dest = Path.Combine(destDir, relative);
                var destParent = Path.GetDirectoryName(dest)!;
                if (!string.IsNullOrEmpty(destParent))
                {
                    Directory.CreateDirectory(destParent);
                }

                File.Copy(file, dest, overwrite: true);
            }
        }

        private static ProcessRunResult RunProcess(string fileName, string arguments, string? workDir, int timeout)
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workDir ?? string.Empty,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(timeout);
            return new ProcessRunResult(process.ExitCode, output, error);
        }

        private static Task<ProcessRunResult> RunProcessAsync(
            string fileName,
            string arguments,
            string? workDir,
            CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workDir ?? string.Empty,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                while (!process.HasExited)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    process.WaitForExit(200);
                }

                return new ProcessRunResult(process.ExitCode, output, error);
            }, cancellationToken);
        }

        private sealed record ProcessRunResult(int ExitCode, string Output, string ErrorOutput);
    }
}

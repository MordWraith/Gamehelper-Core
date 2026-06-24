namespace Downloader
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Shared.UpdateSecurity;

    internal sealed class DownloaderForm : Form
    {
        // --- Colors (dark theme matching GameHelper launcher) ---
        private static readonly Color Bg = Color.FromArgb(26, 28, 34);
        private static readonly Color PanelBg = Color.FromArgb(34, 36, 46);
        private static readonly Color Accent = Color.FromArgb(92, 140, 240);
        private static readonly Color TextMain = Color.FromArgb(235, 237, 245);
        private static readonly Color TextMuted = Color.FromArgb(165, 170, 185);
        private static readonly Color TextOk = Color.FromArgb(100, 200, 120);
        private static readonly Color TextWarn = Color.FromArgb(255, 200, 80);
        private static readonly Color TextErr = Color.FromArgb(240, 100, 100);
        private static readonly Color BtnNormal = Color.FromArgb(55, 58, 72);

        // --- Fields ---
        private string targetDir;
        private IReadOnlyList<PluginSource> pluginSources = Array.Empty<PluginSource>();
        private readonly Dictionary<string, PluginRowState> pluginRows = new(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource? cts;
        private bool sdkAvailable;
        private bool gitAvailable;

        // --- Controls ---
        private readonly Label titleLabel;
        private readonly Label subtitleLabel;
        private readonly Panel sdkWarningPanel;
        private readonly Label sdkWarningLabel;
        private readonly Panel coreSection;
        private readonly CheckBox coreCheckBox;
        private readonly Label coreShaLabel;
        private readonly Panel pluginListPanel;
        private readonly Panel pluginsSection;
        private readonly RichTextBox logBox;
        private readonly Button updateSelectedBtn;
        private readonly Button selectAllBtn;
        private readonly Button deselectAllBtn;
        private readonly Button closeBtn;
        private readonly ProgressBar progressBar;

        internal DownloaderForm(string initialTargetDir)
        {
            this.targetDir = initialTargetDir;

            this.Text = "GameHelper Downloader & Updater";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(760, 680);
            this.ClientSize = new Size(820, 780);
            this.BackColor = Bg;
            this.ForeColor = TextMain;
            this.Font = new Font("Segoe UI", 9.5f);

            // Title
            this.titleLabel = new Label
            {
                Text = "GameHelper Downloader & Updater",
                Location = new Point(20, 18),
                Size = new Size(780, 30),
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = TextMain,
                BackColor = Color.Transparent,
            };

            this.subtitleLabel = new Label
            {
                Text = "Select components to install or update.",
                Location = new Point(20, 50),
                Size = new Size(780, 20),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
            };

            // SDK warning banner
            this.sdkWarningPanel = new Panel
            {
                Location = new Point(20, 76),
                Size = new Size(780, 36),
                BackColor = Color.FromArgb(60, 50, 20),
                Visible = false,
            };
            this.sdkWarningLabel = new Label
            {
                Text = "⚠  .NET SDK or git not found — plugin compilation unavailable. Install from https://dotnet.microsoft.com and https://git-scm.com",
                Dock = DockStyle.Fill,
                ForeColor = TextWarn,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font("Segoe UI", 9f),
            };
            this.sdkWarningPanel.Controls.Add(this.sdkWarningLabel);

            // Core section
            this.coreSection = new Panel
            {
                Location = new Point(20, 120),
                Size = new Size(780, 56),
                BackColor = PanelBg,
            };

            var coreSectionTitle = new Label
            {
                Text = "CORE",
                Location = new Point(12, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Accent,
            };

            this.coreCheckBox = new CheckBox
            {
                Text = "GameHelper (Core)  —  from MordWraith/Gamehelper-Core",
                Location = new Point(12, 28),
                Size = new Size(500, 20),
                Checked = true,
                ForeColor = TextMain,
                BackColor = PanelBg,
            };

            this.coreShaLabel = new Label
            {
                Text = "Checking...",
                Location = new Point(520, 30),
                Size = new Size(248, 18),
                ForeColor = TextMuted,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false,
            };

            this.coreSection.Controls.AddRange(new Control[]
            {
                coreSectionTitle, this.coreCheckBox, this.coreShaLabel,
            });

            // Plugins section
            this.pluginsSection = new Panel
            {
                Location = new Point(20, 184),
                Size = new Size(780, 330),
                BackColor = PanelBg,
            };

            var pluginSectionTitle = new Label
            {
                Text = "PLUGINS  (requires .NET SDK + git for compilation from source)",
                Location = new Point(12, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Accent,
            };

            this.pluginListPanel = new Panel
            {
                Location = new Point(4, 28),
                Size = new Size(772, 294),
                BackColor = PanelBg,
                AutoScroll = true,
            };

            this.pluginsSection.Controls.Add(pluginSectionTitle);
            this.pluginsSection.Controls.Add(this.pluginListPanel);

            // Action buttons
            this.selectAllBtn = MakeButton("Select All", BtnNormal, new Point(20, 524), new Size(110, 32));
            this.deselectAllBtn = MakeButton("Deselect All", BtnNormal, new Point(138, 524), new Size(110, 32));
            this.updateSelectedBtn = MakeButton("Update Selected", Accent, new Point(600, 524), new Size(200, 32));
            this.updateSelectedBtn.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            this.closeBtn = MakeButton("Close", BtnNormal, new Point(462, 524), new Size(130, 32));

            this.selectAllBtn.Click += (_, _) => this.SetAllChecked(true);
            this.deselectAllBtn.Click += (_, _) => this.SetAllChecked(false);
            this.updateSelectedBtn.Click += this.OnUpdateClick;
            this.closeBtn.Click += (_, _) =>
            {
                this.cts?.Cancel();
                this.Close();
            };

            this.progressBar = new ProgressBar
            {
                Location = new Point(20, 562),
                Size = new Size(780, 10),
                Style = ProgressBarStyle.Continuous,
                Visible = false,
            };

            // Log box
            var logLabel = new Label
            {
                Text = "Log:",
                Location = new Point(20, 578),
                AutoSize = true,
                ForeColor = TextMuted,
            };

            this.logBox = new RichTextBox
            {
                Location = new Point(20, 598),
                Size = new Size(780, 160),
                ReadOnly = true,
                BackColor = Color.FromArgb(18, 20, 26),
                ForeColor = Color.FromArgb(180, 220, 180),
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Font = new Font("Consolas", 9f),
                WordWrap = true,
            };

            this.Controls.AddRange(new Control[]
            {
                this.titleLabel,
                this.subtitleLabel,
                this.sdkWarningPanel,
                this.coreSection,
                this.pluginsSection,
                this.selectAllBtn,
                this.deselectAllBtn,
                this.updateSelectedBtn,
                this.closeBtn,
                this.progressBar,
                logLabel,
                this.logBox,
            });

            this.Resize += this.OnFormResize;
            this.Load += this.OnFormLoad;
        }

        private void OnFormResize(object? sender, EventArgs e)
        {
            var w = this.ClientSize.Width - 40;
            this.titleLabel.Width = w;
            this.subtitleLabel.Width = w;
            this.sdkWarningPanel.Width = w;
            this.coreSection.Width = w;
            this.pluginsSection.Width = w;
            this.pluginListPanel.Width = w - 8;
            this.logBox.Width = w;
            this.progressBar.Width = w;
            this.updateSelectedBtn.Left = this.ClientSize.Width - 220;
            this.closeBtn.Left = this.ClientSize.Width - 358;

            // Reflow plugin rows.
            var y = 0;
            foreach (var row in this.pluginRows.Values)
            {
                row.Panel.Width = this.pluginListPanel.ClientSize.Width - 4;
                row.Panel.Top = y;
                y += row.Panel.Height + 2;
            }

            // Plugins section height: title (28) + rows + padding.
            var rowsH = this.pluginRows.Count * 30 + this.pluginRows.Count * 2;
            this.pluginsSection.Height = Math.Max(60, 28 + rowsH + 8);

            this.RepositionLowerControls();
        }

        private void RepositionLowerControls()
        {
            var pluginsBottom = this.pluginsSection.Bottom;
            const int gap = 8;
            this.selectAllBtn.Top = pluginsBottom + gap;
            this.deselectAllBtn.Top = pluginsBottom + gap;
            this.updateSelectedBtn.Top = pluginsBottom + gap;
            this.closeBtn.Top = pluginsBottom + gap;
            this.progressBar.Top = pluginsBottom + gap + 40;
            var logLabelCtrl = this.Controls.OfType<Label>().FirstOrDefault(l => l.Text == "Log:");
            if (logLabelCtrl != null) logLabelCtrl.Top = pluginsBottom + gap + 56;
            this.logBox.Top = pluginsBottom + gap + 74;
            this.logBox.Height = Math.Max(80, this.ClientSize.Height - this.logBox.Top - 12);
        }

        private async void OnFormLoad(object? sender, EventArgs e)
        {
            this.Log("Checking environment ...");
            this.sdkAvailable = await Task.Run(PluginBuildHelper.IsDotNetSdkAvailable).ConfigureAwait(true);
            this.gitAvailable = await Task.Run(PluginBuildHelper.IsGitAvailable).ConfigureAwait(true);

            if (!this.sdkAvailable || !this.gitAvailable)
            {
                var missing = new List<string>();
                if (!this.gitAvailable) missing.Add("git");
                if (!this.sdkAvailable) missing.Add(".NET SDK");
                this.sdkWarningLabel.Text =
                    $"⚠  {string.Join(" and ", missing)} not found — plugin compilation unavailable. " +
                    "Install from https://git-scm.com and https://dotnet.microsoft.com/download";
                this.sdkWarningPanel.Visible = true;
                this.coreSection.Top = 120;
                this.pluginsSection.Top = 184;
                this.Log($"Warning: {string.Join(", ", missing)} not available. Plugin updates disabled.");
            }
            else
            {
                this.sdkWarningPanel.Visible = false;
                this.coreSection.Top = 84;
                this.pluginsSection.Top = 148;
                this.Log("Environment OK (.NET SDK and git found).");
            }

            // Load plugin sources from target dir or fall back to bundled list.
            this.pluginSources = PluginSourcesReader.LoadFromInstallDir(this.targetDir);
            if (this.pluginSources.Count == 0)
            {
                this.pluginSources = GetBuiltInPluginSources();
                this.Log($"Using built-in plugin list ({this.pluginSources.Count} plugins).");
            }
            else
            {
                this.Log($"Loaded {this.pluginSources.Count} plugins from plugins-sources.json.");
            }

            this.BuildPluginRows();
            this.OnFormResize(null, EventArgs.Empty);
            this.Log("Checking for updates ...");

            await this.CheckAllUpdatesAsync().ConfigureAwait(true);
        }

        private void BuildPluginRows()
        {
            this.pluginListPanel.Controls.Clear();
            this.pluginRows.Clear();

            var y = 0;
            foreach (var source in this.pluginSources.OrderBy(p => p.Name))
            {
                var row = new PluginRowState(source, this.pluginListPanel.ClientSize.Width - 4);
                row.Panel.Top = y;
                this.pluginListPanel.Controls.Add(row.Panel);
                this.pluginRows[source.Name] = row;
                y += row.Panel.Height + 2;
            }

            // Enable plugin rows only if SDK+git available.
            if (!this.sdkAvailable || !this.gitAvailable)
            {
                foreach (var row in this.pluginRows.Values)
                {
                    row.SetEnabled(false);
                }
            }
        }

        private async Task CheckAllUpdatesAsync()
        {
            // Core: check latest manifest version.
            _ = Task.Run(async () =>
            {
                try
                {
                    using var client = new System.Net.Http.HttpClient();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("GameHelper-Downloader/1.0");
                    var url = Shared.UpdateSecurity.UpdateRepositoryConfig.ManifestUrl;
                    var json = await client.GetStringAsync(url).ConfigureAwait(false);
                    var manifest = Newtonsoft.Json.Linq.JObject.Parse(json);
                    var version = manifest["version"]?.ToString() ?? "?";
                    this.Invoke(() =>
                    {
                        this.coreShaLabel.ForeColor = TextOk;
                        this.coreShaLabel.Text = $"Available: v{version}";
                        this.Log($"Core: v{version} available.");
                    });
                }
                catch
                {
                    this.Invoke(() =>
                    {
                        this.coreShaLabel.ForeColor = TextErr;
                        this.coreShaLabel.Text = "Could not reach GitHub";
                    });
                }
            });

            if (!this.sdkAvailable || !this.gitAvailable)
            {
                return;
            }

            // Plugins: check latest commit SHA from GitHub API.
            var tasks = this.pluginSources.Select(async source =>
            {
                var row = this.pluginRows.GetValueOrDefault(source.Name);
                if (row == null) return;

                var latestSha = await GitHubApiHelper.GetLatestCommitShaAsync(
                    source.Owner, source.RepoName).ConfigureAwait(false);

                var installedSha = PluginVersionStore.GetInstalledSha(this.targetDir, source.Name);
                var upToDate = latestSha != null &&
                               GitHubApiHelper.ShaMatches(installedSha, latestSha);

                this.Invoke(() =>
                {
                    row.SetUpdateInfo(
                        installedSha: installedSha,
                        latestSha: latestSha,
                        upToDate: upToDate);

                    if (latestSha == null)
                    {
                        this.Log($"  {source.Name}: could not reach GitHub.");
                    }
                    else if (upToDate)
                    {
                        this.Log($"  {source.Name}: up to date ({GitHubApiHelper.ShortSha(installedSha)}).");
                    }
                    else
                    {
                        var from = installedSha == null ? "not installed" : GitHubApiHelper.ShortSha(installedSha);
                        this.Log($"  {source.Name}: update available ({from} → {GitHubApiHelper.ShortSha(latestSha)}).");
                    }
                });
            });

            await Task.WhenAll(tasks).ConfigureAwait(true);
            this.Log("Update check complete.");
        }

        private async void OnUpdateClick(object? sender, EventArgs e)
        {
            if (this.cts != null)
            {
                this.cts.Cancel();
                this.cts = null;
                this.updateSelectedBtn.Text = "Update Selected";
                this.updateSelectedBtn.BackColor = Accent;
                return;
            }

            // Ensure target dir is set.
            if (string.IsNullOrWhiteSpace(this.targetDir) || !Directory.Exists(this.targetDir))
            {
                using var dialog = new FolderBrowserDialog
                {
                    Description = "Choose GameHelper installation folder",
                    UseDescriptionForTitle = true,
                };
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                this.targetDir = dialog.SelectedPath;
                this.subtitleLabel.Text = $"Target: {this.targetDir}";
            }

            this.cts = new CancellationTokenSource();
            this.updateSelectedBtn.Text = "Cancel";
            this.updateSelectedBtn.BackColor = Color.FromArgb(160, 60, 60);
            this.updateSelectedBtn.Enabled = true;
            this.progressBar.Visible = true;
            this.progressBar.Value = 0;

            var selectedPlugins = this.pluginRows.Values
                .Where(r => r.CheckBox.Checked && r.CheckBox.Enabled)
                .Select(r => r.Source)
                .ToList();

            var totalWork = (this.coreCheckBox.Checked ? 1 : 0) + selectedPlugins.Count;
            var done = 0;

            void ReportProgress()
            {
                done++;
                this.progressBar.Value = totalWork > 0
                    ? (int)(100.0 * done / totalWork)
                    : 0;
            }

            try
            {
                // Core update.
                if (this.coreCheckBox.Checked)
                {
                    this.Log("=== Downloading Core ===");
                    var coreProgress = new Progress<string>(line => this.Log(line));
                    var service = new GameHelperDownloadService();
                    var result = await service.DownloadAsync(
                        this.targetDir,
                        force: true,
                        coreProgress,
                        this.cts.Token).ConfigureAwait(true);

                    if (result.ExitCode == 0)
                    {
                        this.Log($"Core updated to v{result.Version}.");
                        this.coreShaLabel.ForeColor = TextOk;
                        this.coreShaLabel.Text = $"Installed: v{result.Version}";
                    }
                    else
                    {
                        this.Log($"Core update failed: {result.Message}");
                        this.coreShaLabel.ForeColor = TextErr;
                        this.coreShaLabel.Text = "Update failed";
                    }

                    ReportProgress();
                }

                // Plugin updates.
                foreach (var source in selectedPlugins)
                {
                    if (this.cts.Token.IsCancellationRequested) break;

                    this.Log($"=== Building {source.Name} ===");
                    var row = this.pluginRows.GetValueOrDefault(source.Name);
                    row?.SetStatus("Building...", TextWarn);

                    var pluginProgress = new Progress<string>(line => this.Log(line));
                    var buildResult = await PluginBuildHelper.BuildAndInstallAsync(
                        source,
                        this.targetDir,
                        pluginProgress,
                        this.cts.Token).ConfigureAwait(true);

                    if (buildResult.Success)
                    {
                        this.Log($"{source.Name}: installed ({GitHubApiHelper.ShortSha(buildResult.CommitSha)}).");
                        row?.SetInstalledSha(buildResult.CommitSha);
                        row?.SetStatus("Installed ✓", TextOk);
                    }
                    else
                    {
                        this.Log($"{source.Name}: FAILED — {buildResult.ErrorMessage}");
                        row?.SetStatus("Failed ✗", TextErr);
                    }

                    ReportProgress();
                }
            }
            catch (OperationCanceledException)
            {
                this.Log("Update cancelled.");
            }
            catch (Exception ex)
            {
                this.Log($"Error: {ex.Message}");
            }
            finally
            {
                this.cts = null;
                this.updateSelectedBtn.Text = "Update Selected";
                this.updateSelectedBtn.BackColor = Accent;
                this.progressBar.Value = 100;
            }
        }

        private void SetAllChecked(bool value)
        {
            this.coreCheckBox.Checked = value;
            foreach (var row in this.pluginRows.Values)
            {
                if (row.CheckBox.Enabled)
                {
                    row.CheckBox.Checked = value;
                }
            }
        }

        private void Log(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(() => this.Log(message));
                return;
            }

            this.logBox.AppendText(message + Environment.NewLine);
            this.logBox.SelectionStart = this.logBox.Text.Length;
            this.logBox.ScrollToCaret();
        }

        private static Button MakeButton(string text, Color back, Point loc, Size size)
        {
            var btn = new Button
            {
                Text = text,
                Location = loc,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand,
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // Fallback plugin list bundled into the Downloader binary.
        private static IReadOnlyList<PluginSource> GetBuiltInPluginSources() =>
        [
            new PluginSource("AuraTracker",       "MordWraith/AuraTracker"),
            new PluginSource("AmanamuVoidAlert",   "MordWraith/AmanamuVoidAlert"),
            new PluginSource("Autopot",            "MordWraith/Autopot"),
            new PluginSource("Hiveblood",          "MordWraith/Hiveblood"),
            new PluginSource("PlayerBuffBar",      "MordWraith/PlayerBuffBar"),
            new PluginSource("RitualHelper",       "MordWraith/RitualHelper"),
            new PluginSource("SimpleBars",         "MordWraith/SimpleBars"),
            new PluginSource("Atlas",              "yokkenUA/Atlas"),
            new PluginSource("LootTracker",        "yokkenUA/LootTracker"),
            new PluginSource("RunecraftHelper",    "yokkenUA/RunecraftHelper"),
            new PluginSource("SekhemaHelper",      "yokkenUA/SekhemaHelper"),
        ];

        // --- Inner class: one row in the plugin list ---
        private sealed class PluginRowState
        {
            internal readonly PluginSource Source;
            internal readonly Panel Panel;
            internal readonly CheckBox CheckBox;
            private readonly Label shaLabel;
            private readonly Label statusLabel;
            private string? latestSha;

            internal PluginRowState(PluginSource source, int panelWidth)
            {
                this.Source = source;

                this.Panel = new Panel
                {
                    Size = new Size(panelWidth, 28),
                    BackColor = Color.FromArgb(38, 41, 52),
                };

                this.CheckBox = new CheckBox
                {
                    Text = source.Name,
                    Location = new Point(8, 5),
                    Size = new Size(200, 18),
                    Checked = true,
                    ForeColor = TextMain,
                    BackColor = Color.Transparent,
                };

                var repoLabel = new Label
                {
                    Text = source.GitHubRepo,
                    Location = new Point(215, 6),
                    Size = new Size(220, 16),
                    ForeColor = TextMuted,
                    Font = new Font("Segoe UI", 8.5f),
                };

                this.shaLabel = new Label
                {
                    Text = "Checking...",
                    Location = new Point(440, 6),
                    Size = new Size(200, 16),
                    ForeColor = TextMuted,
                    Font = new Font("Consolas", 8.5f),
                    TextAlign = ContentAlignment.MiddleLeft,
                };

                this.statusLabel = new Label
                {
                    Text = string.Empty,
                    Location = new Point(645, 6),
                    Size = new Size(110, 16),
                    ForeColor = TextMuted,
                    Font = new Font("Segoe UI", 8.5f),
                    TextAlign = ContentAlignment.MiddleRight,
                    AutoSize = false,
                };

                this.Panel.Controls.AddRange(new Control[]
                {
                    this.CheckBox, repoLabel, this.shaLabel, this.statusLabel,
                });
            }

            internal void SetUpdateInfo(string? installedSha, string? latestSha, bool upToDate)
            {
                this.latestSha = latestSha;
                var installed = installedSha == null ? "not installed" : GitHubApiHelper.ShortSha(installedSha);
                var latest = latestSha == null ? "?" : GitHubApiHelper.ShortSha(latestSha);
                this.shaLabel.Text = $"{installed} → {latest}";

                if (latestSha == null)
                {
                    this.statusLabel.Text = "Unreachable";
                    this.statusLabel.ForeColor = TextErr;
                    this.CheckBox.Enabled = false;
                }
                else if (upToDate)
                {
                    this.statusLabel.Text = "Up to date";
                    this.statusLabel.ForeColor = TextOk;
                    this.CheckBox.Checked = false;
                }
                else
                {
                    this.statusLabel.Text = "Update available";
                    this.statusLabel.ForeColor = TextWarn;
                }
            }

            internal void SetInstalledSha(string? sha)
            {
                var installed = sha == null ? "?" : GitHubApiHelper.ShortSha(sha);
                var latest = this.latestSha == null ? "?" : GitHubApiHelper.ShortSha(this.latestSha);
                this.shaLabel.Text = $"{installed} → {latest}";
            }

            internal void SetStatus(string text, Color color)
            {
                this.statusLabel.Text = text;
                this.statusLabel.ForeColor = color;
            }

            internal void SetEnabled(bool enabled)
            {
                this.CheckBox.Enabled = enabled;
                if (!enabled)
                {
                    this.statusLabel.Text = "SDK/git required";
                    this.statusLabel.ForeColor = TextMuted;
                    this.shaLabel.Text = string.Empty;
                }
            }
        }
    }
}

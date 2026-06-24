namespace Launcher

{

    using System;

    using System.Collections.Generic;

    using System.Drawing;

    using System.Linq;

    using System.Threading;

    using System.Threading.Tasks;

    using System.Diagnostics;
    using System.Windows.Forms;
    using Shared.UpdateSecurity;



    internal enum UpdateFormPhase

    {

        Checking,

        Prompt,

        MigrationNotice,

        Downloading,

        ReadyToInstall,

        Error,

    }



    internal sealed class UpdateForm : Form

    {

        private static readonly Color Bg = Color.FromArgb(26, 28, 34);

        private static readonly Color PanelBg = Color.FromArgb(34, 36, 46);

        private static readonly Color Accent = Color.FromArgb(92, 140, 240);

        private static readonly Color TextMain = Color.FromArgb(235, 237, 245);

        private static readonly Color TextMuted = Color.FromArgb(165, 170, 185);

        private static readonly Color ChangelogBg = Color.FromArgb(24, 26, 32);

        private static readonly Color TabStripBg = Color.FromArgb(30, 32, 40);

        private static readonly Color ChangelogBorder = Color.FromArgb(48, 52, 66);



        private readonly string installDir;

        private readonly string appExePath;

        private readonly Label titleLabel;

        private readonly Label subtitleLabel;

        private readonly Panel contentPanel;

        private readonly Label statusLabel;

        private readonly ProgressBar progressBar;

        private readonly TabControl changelogTabs;

        private readonly TabPage tabCurrentRelease;

        private readonly TabPage tabAllReleases;

        private readonly RichTextBox currentReleaseBox;

        private readonly RichTextBox historyBox;

        private readonly RichTextBox errorBox;

        private IReadOnlyList<string> currentChangelogRaw = Array.Empty<string>();

        private readonly Button primaryButton;

        private readonly Button secondaryButton;



        private UpdateOffer? currentOffer;

        private UpdateMigrationNotice.Info? currentMigration;

        private IReadOnlyList<ReleaseHistoryEntry> releaseHistory = Array.Empty<ReleaseHistoryEntry>();

        private CancellationTokenSource? downloadCts;

        private UpdateFormPhase phase = UpdateFormPhase.Checking;

        private Button? pluginUpdateButton;



        internal bool ShouldStartGame { get; private set; }



        internal UpdateForm(string installDir, string appExePath)

        {

            this.installDir = installDir;

            this.appExePath = appExePath;



            this.Text = "GameHelper";

            this.StartPosition = FormStartPosition.CenterScreen;

            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            this.MaximizeBox = false;

            this.MinimizeBox = false;

            this.ClientSize = new Size(640, 520);

            this.BackColor = Bg;

            this.ForeColor = TextMain;

            this.Font = new Font("Segoe UI", 10f);



            this.titleLabel = new Label

            {

                AutoSize = false,

                Location = new Point(24, 20),

                Size = new Size(592, 32),

                Font = new Font("Segoe UI", 14f, FontStyle.Bold),

                ForeColor = TextMain,

                Text = "GameHelper",

            };



            this.subtitleLabel = new Label

            {

                AutoSize = false,

                Location = new Point(24, 54),

                Size = new Size(592, 22),

                ForeColor = TextMuted,

                Text = "Checking for updates...",

            };



            this.contentPanel = new Panel

            {

                Location = new Point(24, 88),

                Size = new Size(592, 340),

                BackColor = PanelBg,

            };



            this.statusLabel = new Label

            {

                AutoSize = false,

                Location = new Point(16, 12),

                Size = new Size(560, 22),

                ForeColor = TextMain,

                Text = "Please wait...",

            };



            this.progressBar = new ProgressBar

            {

                Location = new Point(16, 44),

                Size = new Size(560, 24),

                Style = ProgressBarStyle.Continuous,

                Visible = false,

            };



            this.currentReleaseBox = new RichTextBox

            {

                Dock = DockStyle.Fill,

                BackColor = ChangelogBg,

                ForeColor = TextMain,

                BorderStyle = BorderStyle.None,

                ReadOnly = true,

                ScrollBars = RichTextBoxScrollBars.Vertical,

                WordWrap = true,

                Font = new Font("Segoe UI", 9.5f),

            };



            this.historyBox = new RichTextBox

            {

                Dock = DockStyle.Fill,

                BackColor = ChangelogBg,

                ForeColor = TextMain,

                BorderStyle = BorderStyle.None,

                ReadOnly = true,

                ScrollBars = RichTextBoxScrollBars.Vertical,

                WordWrap = true,

                Font = new Font("Segoe UI", 9.5f),

            };



            this.errorBox = new RichTextBox

            {

                Location = new Point(16, 40),

                Size = new Size(560, 280),

                Visible = false,

                ReadOnly = true,

                BorderStyle = BorderStyle.None,

                BackColor = ChangelogBg,

                ForeColor = TextMain,

                ScrollBars = RichTextBoxScrollBars.Vertical,

                WordWrap = true,

                Font = new Font("Segoe UI", 9.5f),

            };



            this.tabCurrentRelease = new TabPage();

            this.tabAllReleases = new TabPage();

            this.tabCurrentRelease.Controls.Add(this.currentReleaseBox);

            this.tabAllReleases.Controls.Add(this.historyBox);



            this.tabCurrentRelease.BackColor = ChangelogBg;

            this.tabCurrentRelease.UseVisualStyleBackColor = false;

            this.tabAllReleases.BackColor = ChangelogBg;

            this.tabAllReleases.UseVisualStyleBackColor = false;

            this.changelogTabs = new TabControl

            {

                Location = new Point(12, 38),

                Size = new Size(568, 290),

                Visible = false,

                DrawMode = TabDrawMode.OwnerDrawFixed,

                Appearance = TabAppearance.FlatButtons,

                SizeMode = TabSizeMode.Fixed,

                ItemSize = new Size(148, 30),

                Padding = new Point(0, 0),

                BackColor = PanelBg,

            };

            this.changelogTabs.TabPages.Add(this.tabCurrentRelease);

            this.changelogTabs.TabPages.Add(this.tabAllReleases);

            this.changelogTabs.DrawItem += this.OnChangelogTabDrawItem;

            this.changelogTabs.Paint += this.OnChangelogTabsPaint;

            this.tabCurrentRelease.Paint += this.OnChangelogTabPagePaint;

            this.tabAllReleases.Paint += this.OnChangelogTabPagePaint;

            this.changelogTabs.SelectedIndexChanged += (_, _) => this.changelogTabs.Invalidate();

            this.UpdateTabTitles();



            this.contentPanel.Controls.Add(this.statusLabel);

            this.contentPanel.Controls.Add(this.progressBar);

            this.contentPanel.Controls.Add(this.errorBox);

            this.contentPanel.Controls.Add(this.changelogTabs);



            this.primaryButton = CreateButton("OK", Accent, new Point(416, 448), new Size(200, 40));

            this.secondaryButton = CreateButton(

                "Cancel",

                Color.FromArgb(55, 58, 72),

                new Point(24, 448),

                new Size(200, 40));

            this.primaryButton.Enabled = false;

            this.secondaryButton.Enabled = false;

            this.primaryButton.Click += this.OnPrimaryClick;

            this.secondaryButton.Click += this.OnSecondaryClick;



            // Plugin update notification button — hidden until plugin check completes.
            this.pluginUpdateButton = CreateButton(
                "Checking plugins...",
                Color.FromArgb(55, 58, 72),
                new Point(24, 496),
                new Size(230, 28));
            this.pluginUpdateButton.Font = new Font("Segoe UI", 8.5f);
            this.pluginUpdateButton.Visible = false;
            this.pluginUpdateButton.Click += this.OnPluginUpdateClick;

            this.Controls.Add(this.titleLabel);

            this.Controls.Add(this.subtitleLabel);

            this.Controls.Add(this.contentPanel);

            this.Controls.Add(this.primaryButton);

            this.Controls.Add(this.secondaryButton);

            this.Controls.Add(this.pluginUpdateButton);

            this.Load += this.OnFormLoad;

            this.FormClosing += this.OnFormClosing;

        }



        private static Button CreateButton(string text, Color back, Point location, Size size)

        {

            var button = new Button

            {

                Text = text,

                Location = location,

                Size = size,

                FlatStyle = FlatStyle.Flat,

                BackColor = back,

                ForeColor = Color.White,

                Font = new Font("Segoe UI", 10f, FontStyle.Bold),

                Cursor = Cursors.Hand,

            };

            button.FlatAppearance.BorderSize = 0;

            return button;

        }



        private void OnChangelogTabDrawItem(object? sender, DrawItemEventArgs e)

        {

            var selected = this.changelogTabs.SelectedIndex == e.Index;

            var back = selected ? Color.FromArgb(48, 52, 66) : TabStripBg;

            var fore = selected ? TextMain : TextMuted;

            using (var bgBrush = new SolidBrush(back))

            {

                e.Graphics.FillRectangle(bgBrush, e.Bounds);

            }

            if (selected)

            {

                using var accentPen = new Pen(Accent, 2);

                e.Graphics.DrawLine(

                    accentPen,

                    e.Bounds.Left + 2,

                    e.Bounds.Bottom - 1,

                    e.Bounds.Right - 2,

                    e.Bounds.Bottom - 1);

            }

            var text = this.changelogTabs.TabPages[e.Index].Text;

            TextRenderer.DrawText(

                e.Graphics,

                text,

                this.Font,

                e.Bounds,

                fore,

                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        }



        private void OnChangelogTabsPaint(object? sender, PaintEventArgs e)

        {

            var tabs = (TabControl)sender!;

            if (tabs.TabCount == 0)

            {

                return;

            }



            using (var stripBrush = new SolidBrush(TabStripBg))

            {

                var firstTab = tabs.GetTabRect(0);

                var lastTab = tabs.GetTabRect(tabs.TabCount - 1);

                e.Graphics.FillRectangle(stripBrush, 0, 0, tabs.Width, firstTab.Bottom);

                if (lastTab.Right < tabs.Width)

                {

                    e.Graphics.FillRectangle(

                        stripBrush,

                        lastTab.Right,

                        0,

                        tabs.Width - lastTab.Right,

                        firstTab.Height);

                }

            }



            var display = tabs.DisplayRectangle;

            var outer = new Rectangle(

                display.X - 2,

                display.Y - 2,

                display.Width + 3,

                display.Height + 3);

            using (var fillBrush = new SolidBrush(ChangelogBg))

            {

                e.Graphics.FillRectangle(fillBrush, outer);

            }



            using (var borderPen = new Pen(ChangelogBorder, 1))

            {

                e.Graphics.DrawRectangle(

                    borderPen,

                    display.X - 1,

                    display.Y - 1,

                    display.Width + 1,

                    display.Height + 1);

            }

        }



        private void OnChangelogTabPagePaint(object? sender, PaintEventArgs e)

        {

            var page = (TabPage)sender!;

            using var brush = new SolidBrush(ChangelogBg);

            e.Graphics.FillRectangle(brush, page.ClientRectangle);

        }



        private void RefreshLocalizedUi()

        {

            this.UpdateTabTitles();



            switch (this.phase)

            {

                case UpdateFormPhase.Checking:

                    this.ShowChecking();

                    break;

                case UpdateFormPhase.Prompt when this.currentOffer != null:

                    this.ShowPrompt(this.currentOffer);

                    break;



                case UpdateFormPhase.MigrationNotice when this.currentMigration != null:

                    this.ShowMigrationNotice(this.currentMigration);

                    break;

                case UpdateFormPhase.Downloading:

                    this.titleLabel.Text = "Downloading update";

                    this.statusLabel.Text = "Download in progress...";

                    break;

                case UpdateFormPhase.ReadyToInstall when this.currentOffer != null:

                    this.ShowReadyToRestart(this.currentOffer);

                    break;

                case UpdateFormPhase.Error:

                    this.primaryButton.Text = "Start without update";

                    this.secondaryButton.Text = "Exit";

                    break;

            }



            if (this.changelogTabs.Visible && this.currentOffer != null)
            {
                this.FillChangelogList(this.currentOffer.Changelog);
            }

            this.FillHistoryBox();

        }



        private void UpdateTabTitles()

        {

            this.tabCurrentRelease.Text = "This update";

            this.tabAllReleases.Text = "All releases";

        }



        private async void OnFormLoad(object? sender, EventArgs e)

        {

            await this.RunUpdateFlowAsync();

        }



        private void OnFormClosing(object? sender, FormClosingEventArgs e)

        {

            this.downloadCts?.Cancel();

        }



        private async Task RunUpdateFlowAsync()

        {

            // Start plugin update check in background (doesn't block game startup).
            _ = this.CheckPluginsInBackgroundAsync();

            try

            {

                var current = UpdateService.GetCurrentVersion(this.appExePath);

                this.subtitleLabel.Text = $"Installed: v{current}";

                this.ShowChecking();



                var historyTask = ChangelogHistoryService.LoadMergedAsync(this.installDir);

                var checkResult = await UpdateService.CheckForUpdateAsync(this.appExePath, this.installDir);

                this.releaseHistory = await historyTask;

                this.FillHistoryBox();



                if (checkResult.Offer == null)

                {

                    if (checkResult.MigrationNotice != null &&
                        UpdateMigrationNotice.ShouldShow(this.installDir, checkResult.MigrationNotice, current))

                    {

                        this.currentMigration = checkResult.MigrationNotice;

                        this.ShowMigrationNotice(checkResult.MigrationNotice);

                        return;

                    }

                    this.ShouldStartGame = true;

                    this.Close();

                    return;

                }



                this.currentOffer = checkResult.Offer;

                this.ShowPrompt(checkResult.Offer);

            }

            catch (Exception ex)

            {

                LauncherLog.Write($"Update-Flow: {ex}");

                this.ShowError(UpdateErrors.Format(ex));

            }

        }



        private void HideErrorDetails()

        {

            this.errorBox.Visible = false;

            this.errorBox.Clear();

        }



        private void ShowChecking()

        {

            this.phase = UpdateFormPhase.Checking;

            this.HideErrorDetails();

            this.titleLabel.Text = "GameHelper";

            this.statusLabel.Text = "Checking for updates on GitHub...";

            this.progressBar.Visible = false;

            this.changelogTabs.Visible = false;

            this.primaryButton.Enabled = false;

            this.secondaryButton.Enabled = false;

        }



        private IReadOnlyList<string> BuildChangelogLines(UpdateOffer offer)
        {
            if (offer.MigrationNotice == null)
            {
                return offer.Changelog;
            }

            var warning = offer.MigrationNotice.MessageEn;
            var maxAuto = offer.MigrationNotice.MaxAutoUpdateVersion;
            var header = string.IsNullOrWhiteSpace(maxAuto)
                ? $"IMPORTANT: v{offer.MigrationNotice.ManualInstallVersion} must be installed manually after this update."
                : $"IMPORTANT: Auto-update stops at v{maxAuto}. From v{offer.MigrationNotice.ManualInstallVersion} install manually.";
            return new[] { header, warning }.Concat(offer.Changelog).ToList();
        }

        private void ShowMigrationNotice(UpdateMigrationNotice.Info notice)
        {
            this.phase = UpdateFormPhase.MigrationNotice;
            this.HideErrorDetails();

            var maxAuto = notice.MaxAutoUpdateVersion;
            this.titleLabel.Text = string.IsNullOrWhiteSpace(maxAuto)
                ? $"Manual update required for v{notice.ManualInstallVersion}"
                : $"Install v{notice.ManualInstallVersion} manually";

            this.subtitleLabel.Text = string.IsNullOrWhiteSpace(maxAuto)
                ? $"Your installed version will not auto-update to v{notice.ManualInstallVersion}."
                : $"Auto-update works up to v{maxAuto}. Use GameHelperDownloader.exe or the ZIP for v{notice.ManualInstallVersion}+.";

            this.statusLabel.Text = notice.MessageEn;

            this.progressBar.Visible = false;
            this.changelogTabs.Visible = false;

            this.primaryButton.Text = "Continue";
            this.secondaryButton.Text = "Open download page";
            this.primaryButton.Enabled = true;
            this.secondaryButton.Enabled = true;
        }

        private void ShowPrompt(UpdateOffer offer)

        {

            this.phase = UpdateFormPhase.Prompt;

            this.HideErrorDetails();

            this.titleLabel.Text = offer.MigrationNotice != null
                ? $"Update v{offer.RemoteVersion} (read notice)"
                : $"Update v{offer.RemoteVersion} available";

            this.subtitleLabel.Text = offer.IsZipUpdate
                    ? $"Current: v{offer.CurrentVersion}  ->  New: v{offer.RemoteVersion} (full package)"
                    : $"Current: v{offer.CurrentVersion}  ->  New: v{offer.RemoteVersion} ({offer.FileCount} files)";

            this.statusLabel.Text = "Release notes:";

            this.progressBar.Visible = false;

            this.changelogTabs.Visible = true;

            this.changelogTabs.SelectedTab = this.tabCurrentRelease;

            this.FillChangelogList(this.BuildChangelogLines(offer));



            this.primaryButton.Text = $"Update now (v{offer.RemoteVersion})";
            this.secondaryButton.Text = "Start without update";

            this.primaryButton.Enabled = true;

            this.secondaryButton.Enabled = true;

        }



        private async void BeginDownload()

        {

            await this.ShowDownloadAsync();

        }



        private async Task ShowDownloadAsync()

        {

            if (this.currentOffer == null)

            {

                return;

            }



            this.phase = UpdateFormPhase.Downloading;

            this.HideErrorDetails();

            this.titleLabel.Text = "Downloading update";

            this.subtitleLabel.Text = $"v{this.currentOffer.RemoteVersion}";

            this.statusLabel.Text = "Download in progress...";

            this.progressBar.Visible = true;

            this.progressBar.Value = 0;

            this.changelogTabs.Visible = false;

            this.primaryButton.Enabled = false;

            this.secondaryButton.Enabled = false;



            this.downloadCts = new CancellationTokenSource();

            var progress = new Progress<DownloadProgress>(p =>

            {

                this.progressBar.Value = Math.Clamp(p.Percent, 0, 100);

                this.statusLabel.Text = string.IsNullOrEmpty(p.CurrentFile)
                    ? "Download complete."
                    : $"Downloading ({p.CompletedFiles + 1}/{p.TotalFiles}):{Environment.NewLine}{p.CurrentFile}";

            });



            try

            {

                await UpdateService.DownloadUpdateAsync(

                    this.currentOffer,

                    this.installDir,

                    progress,

                    this.downloadCts.Token);

                this.ShowReadyToRestart(this.currentOffer);

            }

            catch (OperationCanceledException)

            {

                this.ShowError("Download cancelled.");

            }

            catch (Exception ex)

            {

                LauncherLog.Write($"Download: {ex}");

                var detail = UpdateErrors.Format(ex);

                this.ShowError($"Download failed:{Environment.NewLine}{detail}");

            }

        }



        private void ShowReadyToRestart(UpdateOffer offer)

        {

            this.phase = UpdateFormPhase.ReadyToInstall;

            this.HideErrorDetails();

            this.titleLabel.Text = "Update ready";

            this.subtitleLabel.Text = $"Version {offer.RemoteVersion} has been downloaded.";

            this.statusLabel.Text = "Release notes:";

            this.progressBar.Visible = false;

            this.changelogTabs.Visible = true;

            this.changelogTabs.SelectedTab = this.tabCurrentRelease;

            this.FillChangelogList(offer.Changelog);



            this.primaryButton.Text = "Restart and install";

            this.secondaryButton.Text = "Later";

            this.primaryButton.Enabled = true;

            this.secondaryButton.Enabled = true;

        }



        private static string NormalizeVersion(string version) =>

            version.Trim().TrimStart('v', 'V');



        private IReadOnlyList<string> GetDisplayChangelogRaw(IReadOnlyList<string> manifestLines)

        {

            var version = this.currentOffer?.RemoteVersion;

            if (string.IsNullOrEmpty(version))

            {

                return manifestLines;

            }

            var entry = this.releaseHistory.FirstOrDefault(r =>

                string.Equals(NormalizeVersion(r.Version), NormalizeVersion(version), StringComparison.OrdinalIgnoreCase));

            if (entry?.Changelog == null || entry.Changelog.Count == 0)

            {

                return manifestLines;

            }

            var manifestBilingual = manifestLines.Any(ChangelogLocalization.LooksBilingual);

            var historyBilingual = entry.Changelog.Any(ChangelogLocalization.LooksBilingual);

            return historyBilingual || !manifestBilingual ? entry.Changelog : manifestLines;

        }



        private void FillChangelogList(IReadOnlyList<string> rawLines)

        {

            this.currentChangelogRaw = this.GetDisplayChangelogRaw(rawLines);

            var lines = ChangelogLocalization.ResolveLines(this.currentChangelogRaw).ToList();

            if (lines.Count == 0)

            {

                lines.Add("Improvements and bug fixes.");

            }

            this.currentReleaseBox.Text = string.Join(

                Environment.NewLine,

                lines.Select(line => $"• {line}"));

            this.currentReleaseBox.SelectionStart = 0;

            this.currentReleaseBox.ScrollToCaret();

        }



        private void FillHistoryBox()

        {

            this.historyBox.Text = ChangelogHistoryService.FormatForDisplay(this.releaseHistory);

            this.historyBox.SelectionStart = 0;

            this.historyBox.ScrollToCaret();

        }



        private void ShowError(string message)

        {

            this.phase = UpdateFormPhase.Error;

            this.titleLabel.Text = "Update unavailable";

            this.subtitleLabel.Text = string.Empty;

            this.statusLabel.Text = "Details:";

            this.errorBox.Visible = true;

            this.errorBox.Text = message;

            this.errorBox.SelectionStart = 0;

            this.errorBox.ScrollToCaret();

            this.progressBar.Visible = false;

            this.changelogTabs.Visible = false;

            this.primaryButton.Text = "Start without update";

            this.secondaryButton.Text = "Exit";

            this.primaryButton.Enabled = true;

            this.secondaryButton.Enabled = true;

        }



        private void OnPrimaryClick(object? sender, EventArgs e)

        {

            switch (this.phase)

            {

                case UpdateFormPhase.MigrationNotice:

                    this.ShouldStartGame = true;

                    this.Close();

                    return;



                case UpdateFormPhase.Prompt:

                    this.primaryButton.Enabled = false;

                    this.secondaryButton.Enabled = false;

                    this.BeginDownload();

                    return;



                case UpdateFormPhase.ReadyToInstall:

                    this.InstallAndExit();

                    return;



                case UpdateFormPhase.Error:

                default:

                    this.ShouldStartGame = true;

                    this.Close();

                    return;

            }

        }



        private void InstallAndExit()

        {

            try

            {

                if (!UpdateService.HasStagedUpdate)

                {

                    throw new InvalidOperationException(
                        "Downloaded files are missing. Please run the update again.");

                }



                this.primaryButton.Enabled = false;

                this.secondaryButton.Enabled = false;

                this.statusLabel.Text = "Installing update and restarting...";

                UpdateService.ApplyUpdateAndRestart();

                Environment.Exit(0);

            }

            catch (Exception ex)

            {

                LauncherLog.Write($"Install: {ex}");

                var message = ex.Message.Contains("Zugriff verweigert", StringComparison.OrdinalIgnoreCase) ||

                              ex.Message.Contains("Access is denied", StringComparison.OrdinalIgnoreCase)

                    ? "Could not start the update helper (access denied). Run GameHelper as administrator, or install to a folder such as Documents\\GameHelper."

                    : ex.Message;

                MessageBox.Show(

                    message,

                    "GameHelper",

                    MessageBoxButtons.OK,

                    MessageBoxIcon.Error);

                this.primaryButton.Enabled = true;

                this.secondaryButton.Enabled = true;

            }

        }



        private void OnSecondaryClick(object? sender, EventArgs e)

        {

            switch (this.phase)

            {

                case UpdateFormPhase.MigrationNotice:

                    try

                    {

                        Process.Start(new ProcessStartInfo

                        {

                            FileName = $"{UpdateRepositoryConfig.GitHubHost}/{UpdateRepositoryConfig.Repository}/releases/latest",

                            UseShellExecute = true,

                        });

                    }

                    catch (Exception ex)

                    {

                        LauncherLog.Write($"Open releases: {ex}");

                    }

                    return;



                case UpdateFormPhase.Prompt:

                case UpdateFormPhase.ReadyToInstall:

                    this.ShouldStartGame = true;

                    this.Close();

                    return;



                case UpdateFormPhase.Error:

                    Environment.Exit(0);

                    return;



                default:

                    this.ShouldStartGame = true;

                    this.Close();

                    return;

            }

        }

        private async Task CheckPluginsInBackgroundAsync()
        {
            try
            {
                var updates = await PluginUpdateChecker.CheckAsync(this.installDir).ConfigureAwait(true);
                if (this.IsDisposed || !this.IsHandleCreated)
                {
                    return;
                }

                if (updates.Count == 0)
                {
                    // No plugin updates — keep button hidden.
                    return;
                }

                var label = updates.Count == 1
                    ? $"Plugin update: {updates[0].Name}"
                    : $"Plugin updates available ({updates.Count})";

                if (this.pluginUpdateButton != null)
                {
                    this.pluginUpdateButton.Text = label;
                    this.pluginUpdateButton.BackColor = Color.FromArgb(92, 140, 240);
                    this.pluginUpdateButton.Visible = true;
                }
            }
            catch (Exception ex)
            {
                LauncherLog.Write($"Plugin update check: {ex.Message}");
            }
        }

        private void OnPluginUpdateClick(object? sender, EventArgs e)
        {
            var downloaderExe = PluginUpdateChecker.FindDownloaderExe(this.installDir);
            if (downloaderExe == null)
            {
                MessageBox.Show(
                    "GameHelperDownloader.exe not found.\n" +
                    "Download it from the GitHub releases page to manage plugin updates.",
                    "GameHelper",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = downloaderExe,
                    Arguments = $"\"{this.installDir}\"",
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                LauncherLog.Write($"Open downloader: {ex.Message}");
                MessageBox.Show(
                    $"Could not open GameHelperDownloader.exe:\n{ex.Message}",
                    "GameHelper",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

    }

}



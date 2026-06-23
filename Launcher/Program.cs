namespace Launcher
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public static class Program
    {
        [STAThread]
        private static async Task Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ApplicationConfiguration.Initialize();

            LauncherLog.Write($"Start (PID {Environment.ProcessId})");

            if (!GameHelperFinder.TryFindGameHelperExe(out var installDir, out var appExePath))
            {
                LauncherDialogs.ShowError(
                    $"GameHelper.App.exe was not found in:{Environment.NewLine}{installDir}");
                return;
            }

            LauncherLog.Write($"InstallDir={installDir}");

            using var updateForm = new UpdateForm(installDir, appExePath);
            updateForm.ShowDialog();

            if (!updateForm.ShouldStartGame)
            {
                return;
            }

            if (GameStarter.TryStart(installDir, appExePath))
            {
                Application.Exit();
            }
        }
    }
}

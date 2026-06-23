namespace Launcher
{
    using System;
    using System.Diagnostics;
    using System.Windows.Forms;

    internal static class GameStarter
    {
        internal static bool TryStart(string installDir, string appExePath)
        {
            try
            {
                var newName = MiscHelper.GenerateRandomString();
                TemporaryFileManager.Purge();

                var gameHelperPath = GameHelperTransformer.TransformGameHelperExecutable(installDir, appExePath, newName);
                Process.Start(new ProcessStartInfo
                {
                    FileName = gameHelperPath,
                    WorkingDirectory = installDir,
                    UseShellExecute = true,
                });
                LauncherLog.Write($"Overlay started: {gameHelperPath}");
                return true;
            }
            catch (Exception ex)
            {
                LauncherLog.Write($"Error: {ex}");
                var incompleteHint = ex.Message.Contains("AsmResolver", StringComparison.OrdinalIgnoreCase)
                    ? Environment.NewLine + Environment.NewLine +
                      "Installation incomplete. Delete the folder and run GameHelperDownloader again into an empty folder."
                    : string.Empty;
                LauncherDialogs.ShowError(
                    $"Start failed:{Environment.NewLine}{ex.Message}{incompleteHint}");
                return false;
            }
        }
    }
}

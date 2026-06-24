namespace Downloader
{
    using System;
    using System.IO;
    using System.Windows.Forms;

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Parse target dir from args (positional or --target).
            string? targetDir = null;
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg is "--help" or "-h" or "/?")
                {
                    PrintHelp();
                    return;
                }

                if ((arg is "--target" or "-t") && i + 1 < args.Length)
                {
                    targetDir = args[++i];
                    continue;
                }

                if (!arg.StartsWith('-'))
                {
                    targetDir = arg;
                }
            }

            // If no target provided, suggest default path next to the exe.
            if (string.IsNullOrWhiteSpace(targetDir))
            {
                targetDir = Path.Combine(AppContext.BaseDirectory, "GameHelper");
            }

            Application.Run(new DownloaderForm(targetDir));
        }

        private static void PrintHelp()
        {
            Console.WriteLine("GameHelperDownloader");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  GameHelperDownloader.exe [target folder]");
            Console.WriteLine("  GameHelperDownloader.exe --target \"D:\\Games\\GameHelper\"");
            Console.WriteLine();
            Console.WriteLine("Opens a GUI to install or update GameHelper Core and plugins.");
            Console.WriteLine("Plugin compilation requires .NET SDK and git to be installed.");
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace LostArkMarketWatcherV2.modules
{
    public class FileWatcher
    {
        FileSystemWatcher? fileSystemWatcher;
        readonly LamoLogger logger;
        SemaphoreSlim screenshotSemaphore;
        public FileWatcher()
        {
            this.logger = (System.Windows.Application.Current as LamoWatcherApp)?.logger!;
            this.screenshotSemaphore =  new SemaphoreSlim(LamoConfig.Instance.screenshot_threads);
            SetPath();
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            logger?.Debug($"Screenshot queued: {e.Name}");
            Task.Run(async () =>
            {
                this.screenshotSemaphore.Wait();
                LamoScan scan = new(e.FullPath);
                await scan.Scan();
                this.screenshotSemaphore.Release();
            });
        }

        public void SetPath()
        {

            fileSystemWatcher?.Dispose();
            fileSystemWatcher = new FileSystemWatcher();
            string? screenshots_path = LamoConfig.Instance.screenshot_folder;
            screenshots_path ??= Path.Join(LamoConfig.Instance.game_directory, "EFGame", "Screenshots");
            logger?.Debug($"Watching: {screenshots_path}");
            fileSystemWatcher.Path = screenshots_path;
            fileSystemWatcher.Created += OnFileCreated;
            fileSystemWatcher.EnableRaisingEvents = true;
        }

    }
}

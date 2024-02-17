using LostArkMarketWatcherV2.modules;
using LostArkMarketWatcherV2.windows.login;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Application = System.Windows.Application;


namespace LostArkMarketWatcherV2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class LamoWatcherApp : Application
    {
        private NotifyIcon? _notifyIcon;
        private LogWindow? _logWindow;
        private ConfigWindow? _configWindow;
        private LoginWindow? _loginWindow;
        public LamoLogger? logger;
        public FileWatcher? fileWatcher;
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                InitializeWindows();
                InitializeSystemTrayIcon();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void InitializeWindows()
        {
            _logWindow = new LogWindow();
            logger = new LamoLogger(_logWindow);
            _configWindow = new ConfigWindow();
            _loginWindow = new LoginWindow();
            if (LamoConfig.Instance.cookie == null)
            {
                _loginWindow.Show();
            }
            else
            {
                _ = CheckSession();
            }
        }
        private void InitializeSystemTrayIcon()
        {
            this._notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = new System.Drawing.Icon("Assets/Icons/favicon.ico"),
                Visible = true,
                ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip()
            };
            this._notifyIcon.ContextMenuStrip.Items.Add("Show Log", null, (sender, args) => ShowLog());
            this._notifyIcon.ContextMenuStrip.Items.Add("Show Config", null, (sender, args) => ShowConfig());
            this._notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (sender, args) => CloseApp());
        }
        private void CloseApp()
        {
            _notifyIcon!.Visible = false;
            Shutdown();
        }
        public void ShowLog()
        {
            if (_logWindow != null)
            {
                _logWindow.Show();
                _logWindow.WindowState = WindowState.Normal;
            }
        }
        private void ShowConfig()
        {
            if (_configWindow != null)
            {
                _configWindow.Show();
                _configWindow.WindowState = WindowState.Normal;
            }
        }
        public async Task CheckSession()
        {
            try
            {
                await LamoApi.session();
                logger?.Debug("Logged in");
                if (LamoConfig.Instance.open_log_on_start)
                {
                    _logWindow!.Show();
                }
                if (LamoConfig.Instance.game_directory == null)
                {
                    _configWindow!.Show();
                }
                else
                {
                    fileWatcher = new FileWatcher();
                }
            }
            catch (Exception ex)
            {
                logger?.Error(ex.Message.ToString());
                _loginWindow!.Show();
            }            
        }
    }
    public class LamoLogger(LogWindow logWindow)
    {
        private readonly LogWindow _logWindow = logWindow;

        public void Debug(string text)
        {
            System.Windows.Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                _logWindow?.debug(text);
            }));
        }
        public void Info(string text)
        {
            System.Windows.Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                _logWindow?.info(text);
            }));
        }
        public void Error(string text)
        {
            System.Windows.Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                _logWindow?.error(text);
            }));
        }
    }
}

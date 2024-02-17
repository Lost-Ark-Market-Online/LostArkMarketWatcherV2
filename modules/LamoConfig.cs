using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace LostArkMarketWatcherV2.modules
{
    public class LamoConfig
    {
        private void _save_into_config(string key, string value)
        {
            Configuration config_file = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config_file.AppSettings.Settings.Remove(key);
            config_file.AppSettings.Settings.Add(key, value);
            config_file.Save(ConfigurationSaveMode.Modified);
        }

        public string baseUrl = "https://beta.lostarkmarket.online";
        
        public readonly string version = "1.0.0.0";

        private string? _region;
        public string? region
        {
            set
            {
                _region = value;
            }
            get
            {
                return _region;
            }
        }
        private Cookie? _cookie;
        public Cookie? cookie
        {
            set
            {
                _cookie = value;
                if (_cookie != null)
                {
                    _save_into_config("cookie_name", _cookie.Name);
                    _save_into_config("cookie_value", _cookie.Value);
                }
            }
            get
            {
                return _cookie;
            }
        }
        private bool _debug;
        public bool debug
        {
            set
            {
                _debug = value;
                _save_into_config("debug", _debug.ToString());
            }
            get
            {
                return _debug;
            }
        }
        private int _screenshot_threads;
        public int screenshot_threads
        {
            set
            {
                _screenshot_threads = value;
                _save_into_config("screenshot_threads", _screenshot_threads.ToString());
            }
            get
            {
                return _screenshot_threads;
            }
        }
        private int _scan_threads;
        public int scan_threads
        {
            set
            {
                _scan_threads = value;
                _save_into_config("scan_threads", _scan_threads.ToString());
            }
            get
            {
                return _scan_threads;
            }
        }
        private int _upload_threads;
        public int upload_threads
        {
            set
            {
                _upload_threads = value;
                _save_into_config("upload_threads", _upload_threads.ToString());
            }
            get
            {
                return _upload_threads;
            }
        }
        private int _volume;
        public int volume
        {
            set
            {
                _volume = value;
                _save_into_config("volume", _volume.ToString());
            }
            get
            {
                return _volume;
            }
        }
        private bool _play_audio;
        public bool play_audio
        {
            set
            {
                _play_audio = value;
                _save_into_config("play_audio", _play_audio.ToString());
            }
            get
            {
                return _play_audio;
            }
        }
        private bool _delete_screenshots;
        public bool delete_screenshots
        {
            set
            {
                _delete_screenshots = value;
                _save_into_config("delete_screenshots", _delete_screenshots.ToString());
            }
            get
            {
                return _delete_screenshots;
            }
        }
        private bool _save_log;
        public bool save_log
        {
            set
            {
                _save_log = value;
                _save_into_config("save_log", _save_log.ToString());
            }
            get
            {
                return _save_log;
            }
        }
        private bool _open_log_on_start;
        public bool open_log_on_start
        {
            set
            {
                _open_log_on_start = value;
                _save_into_config("open_log_on_start", _open_log_on_start.ToString());
            }
            get
            {
                return _open_log_on_start;
            }
        }
        private bool _jumpstart;
        public bool jumpstart
        {
            set
            {
                _jumpstart = value;
                _save_into_config("jumpstart", _jumpstart.ToString());
            }
            get
            {
                return _jumpstart;
            }
        }
        private string? _game_directory;
        public string? game_directory
        {
            set
            {
                _game_directory = value;
                if (_game_directory != null)
                {
                    _save_into_config("game_directory", _game_directory);
                    GetRegion();
                }
            }
            get
            {
                return _game_directory;
            }
        }
        private string? _screenshot_folder;
        public string? screenshot_folder
        {
            set
            {
                _screenshot_folder = value;
                if (_screenshot_folder != null)
                {
                    _save_into_config("screenshot_folder", _screenshot_folder);
                }
            }
            get
            {
                return _screenshot_folder;
            }
        }

        private readonly static LamoConfig instance = new LamoConfig();
        private LamoConfig()
        {
            if (ConfigurationManager.AppSettings["cookie_name"] != null && ConfigurationManager.AppSettings["cookie_value"] != null)
            {
                cookie = new Cookie(
                    ConfigurationManager.AppSettings["cookie_name"]!,
                    ConfigurationManager.AppSettings["cookie_value"]!
                );
            }

            if (ConfigurationManager.AppSettings["debug"] != null)
            {
                debug = bool.Parse(ConfigurationManager.AppSettings["debug"]!);
            }
            else
            {
                debug = false;
            }

            if (ConfigurationManager.AppSettings["screenshot_threads"] != null)
            {
                screenshot_threads = int.Parse(ConfigurationManager.AppSettings["screenshot_threads"]!);
            }
            else
            {
                screenshot_threads = 1;
            }

            if (ConfigurationManager.AppSettings["scan_threads"] != null)
            {
                scan_threads = int.Parse(ConfigurationManager.AppSettings["scan_threads"]!);
            }
            else
            {
                scan_threads = 2;
            }

            if (ConfigurationManager.AppSettings["upload_threads"] != null)
            {
                upload_threads = int.Parse(ConfigurationManager.AppSettings["upload_threads"]!);
            }
            else
            {
                upload_threads = 5;
            }

            if (ConfigurationManager.AppSettings["volume"] != null)
            {
                volume = int.Parse(ConfigurationManager.AppSettings["volume"]!);
            }
            else
            {
                volume = 5;
            }

            if (ConfigurationManager.AppSettings["play_audio"] != null)
            {
                play_audio = bool.Parse(ConfigurationManager.AppSettings["play_audio"]!);
            }
            else
            {
                play_audio = true;
            }

            if (ConfigurationManager.AppSettings["delete_screenshots"] != null)
            {
                delete_screenshots = bool.Parse(ConfigurationManager.AppSettings["delete_screenshots"]!);
            }
            else
            {
                delete_screenshots = true;
            }

            if (ConfigurationManager.AppSettings["save_log"] != null)
            {
                save_log = bool.Parse(ConfigurationManager.AppSettings["save_log"]!);
            }
            else
            {
                save_log = true;
            }

            if (ConfigurationManager.AppSettings["open_log_on_start"] != null)
            {
                open_log_on_start = bool.Parse(ConfigurationManager.AppSettings["open_log_on_start"]!);
            }
            else
            {
                open_log_on_start = true;
            }

            if (ConfigurationManager.AppSettings["jumpstart"] != null)
            {
                jumpstart = bool.Parse(ConfigurationManager.AppSettings["jumpstart"]!);
            }
            else
            {
                jumpstart = false;
            }

            if (ConfigurationManager.AppSettings["game_directory"] != null)
            {
                game_directory = ConfigurationManager.AppSettings["game_directory"]!;
            }

            if (ConfigurationManager.AppSettings["screenshot_folder"] != null)
            {
                screenshot_folder = ConfigurationManager.AppSettings["screenshot_folder"]!;
            }
            
            GetRegion();
        }
        void GetRegion()
        {
            if (this.game_directory == null) {
                return;
            }
            string gameOptionsPath = Path.Join(this.game_directory, "EFGame", "Config", "UserOption.xml");
            XmlDocument gameOptionsXml = new();
            gameOptionsXml.Load(gameOptionsPath);
            XmlNode? regionNode = gameOptionsXml.SelectSingleNode("//SaveAccountOptionData/RegionID");
            if (regionNode != null)
            {
                this._region = regionNode.InnerText;
                System.Diagnostics.Debug.WriteLine($"Region Detected: {this.region}");
            }
        }
        public static LamoConfig Instance
        { get { return instance; } }
    }
}

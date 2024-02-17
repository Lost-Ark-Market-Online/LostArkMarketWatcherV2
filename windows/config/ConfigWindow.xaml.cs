using LostArkMarketWatcherV2.modules;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LostArkMarketWatcherV2
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        private readonly LamoLogger logger = (System.Windows.Application.Current as LamoWatcherApp)!.logger!;
        public ConfigWindow()
        {
            InitializeComponent();
            loadConfig();
        }

        private void loadConfig()
        {
            if (LamoConfig.Instance.game_directory != null)
            {
                game_txt.Text = LamoConfig.Instance.game_directory;
            }
            if (LamoConfig.Instance.screenshot_folder != null)
            {
                custom_folder_cb.IsChecked = true;
                custom_folder_container.Visibility = Visibility.Visible;
                custom_folder_txt.Text = LamoConfig.Instance.screenshot_folder;
            }
            del_screenshots_cb.IsChecked = LamoConfig.Instance.delete_screenshots;
            save_logs_cb.IsChecked = LamoConfig.Instance.save_log;
            open_log_cb.IsChecked = LamoConfig.Instance.open_log_on_start;
            screenshots_ud.Value = LamoConfig.Instance.screenshot_threads;
            scanning_ud.Value = LamoConfig.Instance.scan_threads;
            upload_ud.Value = LamoConfig.Instance.upload_threads;
            sound_cb.IsChecked = LamoConfig.Instance.play_audio;
            if (LamoConfig.Instance.play_audio)
            {
                volume_container.Visibility = Visibility.Visible;
                volume_sl.Value = LamoConfig.Instance.volume;
            }
        }

        private void saveConfig()
        {
            if (game_txt.Text.Length > 0)
            {
                LamoConfig.Instance.game_directory = game_txt.Text;
            }
            else
            {
                LamoConfig.Instance.game_directory = null;
            }
            if (custom_folder_cb.IsChecked == true && (custom_folder_txt.Text.Length > 0))
            {
                LamoConfig.Instance.screenshot_folder = custom_folder_txt.Text;
            }
            LamoConfig.Instance.delete_screenshots = del_screenshots_cb.IsChecked.GetValueOrDefault(false);
            LamoConfig.Instance.save_log = save_logs_cb.IsChecked.GetValueOrDefault(false);
            LamoConfig.Instance.open_log_on_start = open_log_cb.IsChecked.GetValueOrDefault(false);
            LamoConfig.Instance.screenshot_threads = screenshots_ud.Value.GetValueOrDefault(1);
            LamoConfig.Instance.scan_threads = scanning_ud.Value.GetValueOrDefault(2);
            LamoConfig.Instance.upload_threads = upload_ud.Value.GetValueOrDefault(5);
            LamoConfig.Instance.play_audio = sound_cb.IsChecked.GetValueOrDefault(false);
            if (sound_cb.IsChecked == true)
            {
                LamoConfig.Instance.volume = (int)volume_sl.Value;
            }
            (System.Windows.Application.Current as LamoWatcherApp)!.fileWatcher!.SetPath();

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            base.OnClosing(e);
            Hide();
        }

        private void customFolderChecked(object sender, RoutedEventArgs e)
        {
            custom_folder_container.Visibility = Visibility.Visible;
        }

        private void customFolderUnchecked(object sender, RoutedEventArgs e)
        {
            custom_folder_container.Visibility = Visibility.Collapsed;
        }

        private void gameFolderClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string selectedFolder = dialog.SelectedPath;
                game_txt.Text = selectedFolder;
            }
        }

        private void playSoundChecked(object sender, RoutedEventArgs e)
        {
            volume_container.Visibility = Visibility.Visible;
        }

        private void playSoundUnchecked(object sender, RoutedEventArgs e)
        {
            volume_container.Visibility = Visibility.Collapsed;
        }

        private void customFolderClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string selectedFolder = dialog.SelectedPath;
                custom_folder_txt.Text = selectedFolder;
            }
        }

        private void saveClick(object sender, RoutedEventArgs e)
        {
            saveConfig();
            Hide();
        }

        private void cancelClick(object sender, RoutedEventArgs e)
        {
            loadConfig();
            Hide();
        }
    }
}

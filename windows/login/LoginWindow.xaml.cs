using LostArkMarketWatcherV2.modules;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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

namespace LostArkMarketWatcherV2.windows.login
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly LamoLogger logger = (System.Windows.Application.Current as LamoWatcherApp)!.logger!;
        public LoginWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            base.OnClosing(e);
            System.Windows.Application.Current.Shutdown();
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void LoginClick(object sender, RoutedEventArgs e)
        {
            String password = password_txt.Password;
            String email = email_txt.Text;
            password_txt.IsEnabled = false;
            email_txt.IsEnabled = false;
            login_btn.IsEnabled = false;
            cancel_btn.IsEnabled = false;
            login(email, password);
    }
    private async void login(String email, String password)
        {
            logger.Debug("Login Action");
            try {
                await LamoApi.login(email, password);
                (System.Windows.Application.Current as LamoWatcherApp)!.ShowLog();
                Hide();
            }
            catch (Exception ex)
            {
                logger.Error("POST request failed: " + ex.Message);
                password_txt.IsEnabled = true;
                email_txt.IsEnabled = true;
                login_btn.IsEnabled = true;
                cancel_btn.IsEnabled = true;
            }
        }
    }

}

using courseselect.Example;
using Npgsql;
using System;
using System.Data;
using System.Windows;

namespace courseselect
{
    public partial class App : Application
    {
        private static NpgsqlConnection _appConnection;

        public static NpgsqlConnection AppConnection
        {
            get
            {
                if (_appConnection == null || _appConnection.State != ConnectionState.Open)
                {
                    string connString = "Host=192.168.56.118;Port=26000;Database=worksys;Username=yxy;Password=Yxy050104";
                    _appConnection = new NpgsqlConnection(connString);
                    _appConnection.Open();
                }
                return _appConnection;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShowLoginWindow();
        }

        private void ShowLoginWindow()
        {
            var loginWindow = new LoginWindow(AppConnection);
            loginWindow.Closed += (s, e) => Shutdown();
            loginWindow.Show();
        }
    }
}
using Npgsql;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace courseselect.Example
{
    public abstract class MainWindow : Window
    {
        protected NpgsqlConnection Connection { get; }
        protected string UserId { get; }

        protected MainWindow(NpgsqlConnection connection, string userId)
        {
            Connection = connection;
            UserId = userId;
            InitializeSecurity();
            this.Closed += (s, e) => Application.Current.Shutdown();
        }

        private void InitializeSecurity()
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
           // this.Icon = new BitmapImage(new Uri("pack://application:,,,/Icons/app.ico"));
        }
    }
}
using courseselect.Example;
using courseselect.Example.Manager.courseselect;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;  // 使用 Microsoft.Data.SqlClient
using System.Windows;

namespace courseselect
{
    public partial class App : Application
    {
        private static SqlConnection _appConnection;  // 更改为 SqlConnection
        public ObservableCollection<TeacherInfo> TeacherList { get; set; } = new ObservableCollection<TeacherInfo>();
        public static SqlConnection AppConnection
        {
            get
            {
                if (_appConnection == null || _appConnection.State != ConnectionState.Open)
                {
                    // 默认情况下，LocalDB 使用以下连接字符串
                   
                    string connString= @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\VSS\courseselect\Example\Database1.mdf";
                    _appConnection = new SqlConnection(connString);
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
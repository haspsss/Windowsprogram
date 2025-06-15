using courseselect.Example;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace courseselect
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
        public partial class MainWindow : Window
        {
            private NpgsqlConnection _connection;

        public MainWindow()
            {
                InitializeComponent();
                this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                this.SourceInitialized += MainWindow_SourceInitialized;
                string connectionString = "Host=192.168.56.118;Port=26000;Database=worksys;username=yxy;Password=Yxy050104;SearchPath=SInformation";
                _connection = new NpgsqlConnection(connectionString);
            try
            {
                if (_connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                    MessageBox.Show("数据加载成功！");
                }

                // var personInfoPage = new Example.PersonInformation.我的信息(_connection, login.userid);
                //frame1.Navigate(personInfoPage);
            }
            catch (Exception ex)
            {
                MessageBox.Show("数据加载失败: " + ex.Message);
            }

        }
        public NpgsqlConnection GetConnection()
        {
            return _connection;
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
            {
                LoginWindow login = new LoginWindow(_connection);
                login.ShowDialog();
                this.Title = "欢迎您，" + login.Username;
            

        }

        private void TreeView_Selected(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = e.Source as TreeViewItem;
            TreeViewItem parent = item.Parent as TreeViewItem;
            if (parent == null) return;
            string path = string.Format("/Example/{0}/{1}.xaml", parent.Tag, item.Header);
            Uri source = new Uri(path, UriKind.Relative);
            Object obj = null;
            try
            {
                obj = System.Windows.Application.LoadComponent(source);

            }
            catch
            {
                System.Windows.MessageBox.Show("未找到" + source.OriginalString);
            }
            Page p = obj as Page;
            if (p != null)
            {
                frame1.NavigationService.RemoveBackEntry();

                frame1.Source = source;
                return;

            }
            Window w = obj as Window;
            if (w != null)
            {
                w.Owner = this;
                w.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                w.ShowDialog();
            }
            else System.Windows.MessageBox.Show("无法将加载的对象转换为Windoww类型");
        }
    }

}



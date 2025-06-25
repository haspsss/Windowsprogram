
using courseselect.Example.Manager.PersonalInformation;

using courseselect.Example.Manager.courseselect;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using courseselect.Example.Manager.stuinfo;
using courseselect.Example.Manager.teachinfo;

namespace courseselect.Example
{
    /// <summary>
    /// AdminMainWindow.xaml 的交互逻辑
    /// </summary>
   
    public partial class AdminMainWindow : Window
    {
        private readonly SqlConnection _connection; // 替换 NpgsqlConnection
        private readonly string _userId;
        private readonly string _userName;
        public AdminMainWindow(SqlConnection connection, string userId,string userName)
        {
            InitializeComponent();
            this.Title = $"教师主界面 - {userId}";
            _connection = connection;
            _userId = userId;
            _userName = userName;

        }
        private void TreeView_Selected(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = e.Source as TreeViewItem;
            TreeViewItem parent = item?.Parent as TreeViewItem;
            if (parent == null) return;

            string header = item.Header.ToString();
            switch (header)
            {
                case "我的信息":
                    // Lazy loading of the MyInformation page

                    var _myInformationPage = new 我的信息(_connection, _userId);

                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(_myInformationPage);
                    break;
                case "选课页面":
                    // Lazy loading of the MyInformation page

                    var courseselect = new 选课页面(_connection, _userId, _userName);

                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(courseselect);
                    break;
                case "课程管理":
                    // Lazy loading of the MyInformation page

                    var coursemanage = new 课程管理(_connection, _userId, _userName);

                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(coursemanage);
                    break;
                case "授课安排":
                    // Lazy loading of the MyInformation page

                    var courseteach = new 授课安排(_connection );

                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(courseteach);
                    break;
                case "学生信息管理":
                    // Lazy loading of the MyInformation page

                    var stuinfo = new 学生信息管理(_connection);

                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(stuinfo);
                    break;
                case "选课信息管理":
                    // Lazy loading of the MyInformation page

                    var selectinfo = new 选课信息管理(_connection);

                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(selectinfo);
                    break;
                case "学生成绩管理":
                    // Lazy loading of the MyInformation page

                    var selectresult = new 学生成绩管理(_connection);

                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(selectresult);
                    break;
                case "教师信息管理":
                    // Lazy loading of the MyInformation page

                    var teachinfo = new 教师信息管理(_connection);

                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(teachinfo);
                    break;
                case "授课信息管理":
                    // Lazy loading of the MyInformation page

                    var teachinfoman = new 授课信息管理(_connection);

                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(teachinfoman);
                    break;
                case "授课成绩信息":
                    // Lazy loading of the MyInformation page

                    var teachresult = new 授课成绩信息(_connection);

                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(teachresult);
                    break;


                default:
                    string path = string.Format("/Example/Teacher/{0}/{1}.xaml", parent.Tag, item.Header);
                    Uri source = new Uri(path, UriKind.Relative);
                    try
                    {
                        object obj = System.Windows.Application.LoadComponent(source);

                        if (obj is Page p)
                        {
                            frame1.NavigationService.RemoveBackEntry();
                            frame1.Source = source;
                        }
                        else if (obj is Window w)
                        {
                            w.Owner = this;
                            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            w.ShowDialog();
                        }
                        else
                        {
                            MessageBox.Show("无法将加载的对象转换为 Page 或 Window 类型");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"加载页面时出错: {ex.Message}");
                    }
                    break;
            }
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // 关闭当前窗口（学生主界面）
            this.Close();

            // 显示登录窗口
            var loginWindow = new LoginWindow(_connection);
            loginWindow.Show();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 实现你自己的文本改变逻辑
        }
    }
}


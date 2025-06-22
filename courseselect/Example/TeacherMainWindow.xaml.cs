
using courseselect.Example.Teacher.courseselect;
using courseselect.Example.Teacher.PersonalInformation;
using courseselect.Example.Teacher.queryresult;
using courseselect.Example.Teacher.examarrangement;
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

namespace courseselect.Example
{
    /// <summary>
    /// TeacherMainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TeacherMainWindow : Window
    {

        private readonly SqlConnection _connection; // 替换 NpgsqlConnection
        private readonly string _userId;
        private readonly string _userName;
        
        public TeacherMainWindow(SqlConnection connection, string userId, string userName)
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

                    var courseselect = new 选课页面(_connection, _userId,_userName);

                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(courseselect);
                    break;
                case "选课信息":
                    // Lazy loading of the MyInformation page

                    var courseselectinformation = new 选课信息(_connection, _userId);

                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(courseselectinformation);
                    break;
                case "成绩录入":
                    // Lazy loading of the MyInformation page

                    var result = new 成绩录入(_connection, _userId);
                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(result);
                    break;
                case "考试信息":
                    // Lazy loading of the MyInformation page

                    var examinfo = new 考试信息(_connection, _userId);
                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(examinfo);
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

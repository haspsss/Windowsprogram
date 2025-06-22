
using System.Data.SqlClient;
using System;

using System.Windows;
using System.Windows.Controls;
using courseselect.Example.Student.PersonalInformation;
using courseselect.Example.Student.courseselect;
using courseselect.Example.Student.queryresult;
using courseselect.Example.Student.考试安排;

namespace courseselect.Example
{
    public partial class StudentMainWindow : Window
    {
        private readonly SqlConnection _connection; // 替换 NpgsqlConnection
        private readonly string _userId;
        private readonly string _userName;
        private MyInformation _myInformationPage; // Store the MyInformation page instance
        private 选课页面 _xuanKePage;

        public StudentMainWindow(SqlConnection connection, string userId, string userName)
        {
            InitializeComponent();
            _connection = connection;
            _userId = userId;
            this.Title = $"学生主界面 - {userId}: 您好！";
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
                    if (_myInformationPage == null)
                    {
                        _myInformationPage = new MyInformation(_connection, _userId);
                    }
                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(_myInformationPage);
                    break;

                case "选课页面":
                    // Lazy loading of the 选课页面
                    if (_xuanKePage == null)
                    {
                        _xuanKePage = new 选课页面(_connection, _userId, _userName); // Pass userId as well
                    }
                    welcombox.IsReadOnly = true;
                    frame1.Navigate(_xuanKePage);
                    break;
                case "选课信息":
                    // Lazy loading of the 选课信息 page
                    var courseInfoPage = new 选课信息(_connection, _userId);
                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(courseInfoPage);
                    break;
                case "选课课表":
                    var classSchedulePage = new 选课课表(_connection, _userId);
                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(classSchedulePage);
                    break;
                case "成绩信息":
                    var resultinformation = new 成绩信息(_connection, _userId);
                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(resultinformation);
                    break;
                case "成绩单":
                    var resulttable = new 成绩单(_connection, _userId);
                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(resulttable);
                    break;
                case "考试信息":
                    var testinformation = new 考试信息(_connection, _userId);
                    welcombox.Visibility = Visibility.Hidden;
                    frame1.Navigate(testinformation);
                    break;
                default:
                    string path = string.Format("/Example/{0}/{1}.xaml", parent.Tag, item.Header);
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
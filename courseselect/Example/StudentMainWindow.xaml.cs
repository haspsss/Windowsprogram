using courseselect.Example.courseselect;
using courseselect.Example.PersonalInformation;
using Npgsql;
using System;
using System.Windows;
using System.Windows.Controls;

namespace courseselect.Example
{
    public partial class StudentMainWindow : Window
    {
        private readonly NpgsqlConnection _connection;
        private readonly string _userId;
        private readonly string _userName;
        private MyInformation _myInformationPage; // Store the MyInformation page instance
        private 选课页面 _xuanKePage;

        public StudentMainWindow(NpgsqlConnection connection, string userId,string userName)
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
                    welcombox.IsReadOnly = true;
                    frame1.Navigate(_myInformationPage);
                    break;

                case "选课页面":
                    // Lazy loading of the 选课页面
                    if (_xuanKePage == null)
                    {
                        _xuanKePage = new 选课页面(_connection, _userId,_userName); // Pass userId as well
                    }
                    welcombox.IsReadOnly = true;
                    frame1.Navigate(_xuanKePage);
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
        

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}

    

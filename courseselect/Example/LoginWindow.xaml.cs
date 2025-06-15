using Npgsql; // 使用 Npgsql 命名空间
using System;
using System.Windows;
using System.Windows.Controls;

namespace courseselect.Example
{
    public partial class LoginWindow : Window
    {
        public string Username { get; set; }
        private NpgsqlConnection _connection;

        public LoginWindow(NpgsqlConnection connection)
        {
            InitializeComponent();
            _connection = connection;
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            UserIdBox.Text = string.Empty;
            Passwordbox.Password = string.Empty;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            switch (btn.Content.ToString())
            {
                case "登录":
                    ValidateUser();
                    break;

                case "忘记密码":
                    HandleForgetPassword();
                    break;
            }
        }

        private void ValidateUser()
        {
            string userid = UserIdBox.Text;
            string password = Passwordbox.Password;

            // 查询用户的名称和密码是否正确
            string queryUser = @"
        SELECT name FROM ""SInformation"".""student"" WHERE sid = @userid AND spassword = @password
        UNION ALL
        SELECT name FROM ""SInformation"".""manager"" WHERE mid = @userid AND mpassword = @password
        UNION ALL
        SELECT name FROM ""SInformation"".""teacher"" WHERE ttid = @userid AND tpassword = @password";

            using (var cmd = new NpgsqlCommand(queryUser, _connection))
            {
                cmd.Parameters.AddWithValue("@userid", userid);
                cmd.Parameters.AddWithValue("@password", password);

                // 执行查询并获取名称
                object result = cmd.ExecuteScalar();

                if (result == null)
                {
                    MessageBox.Show("用户不存在或密码错误！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 设置 Username 为查询到的名称
                this.Username = result.ToString();
                this.Close();
            }
        }
            
        

        private void HandleForgetPassword()
        {
            string userid = UserIdBox.Text;

            // 查询是否修改过密码
            using (var cmd = new NpgsqlCommand("SELECT ifchangepw FROM \"SInformation\".\"student\" WHERE sid = @userid " +
                                                "UNION ALL " +
                                                "SELECT ifchangepw FROM \"SInformation\".\"manager\" WHERE mid = @userid " +
                                                "UNION ALL " +
                                                "SELECT ifchangepw FROM \"SInformation\".\"teacher\" WHERE ttid = @userid", _connection))
            {
                cmd.Parameters.AddWithValue("@userid", userid);
                object result = cmd.ExecuteScalar();

                if (result == null)
                {
                    MessageBox.Show("该用户不存在！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (result.ToString() == "F")
                {
                    MessageBox.Show("您未修改过密码！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (result.ToString() == "T")
                {
                    // 调用Forgetpw页的逻辑
                    Forgetpw forgetPassword = new Forgetpw(_connection);
                    forgetPassword.Show();
                }
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (showpw.IsChecked == true)
            {
                PassTextbox.Text = Passwordbox.Password;
                PassTextbox.Visibility = Visibility.Visible;
            }
        }

        private void showpw_Unchecked(object sender, RoutedEventArgs e)
        {
            if (showpw.IsChecked == false)
            {
                PassTextbox.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (string.IsNullOrEmpty(this.Username))
            {
                App.Current.Shutdown();
            }
        }
    }
}
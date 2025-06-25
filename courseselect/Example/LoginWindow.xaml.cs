using courseselect.Example;

using System.Data.SqlClient; // 使用 Microsoft.Data.SqlClient
using System;
using System.Data;

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace courseselect.Example
{
    public partial class LoginWindow : Window
    {
        private readonly SqlConnection _connection; // 替换 NpgsqlConnection 为 SqlConnection
        public string userName;

        public LoginWindow(SqlConnection connection)
        {
            InitializeComponent();
            _connection = connection;

            if (_connection.State != ConnectionState.Open)
            {
                try
                {
                    _connection.Open();
                    MessageBox.Show("Data loaded successfully!");
                }
                catch (SqlException ex) // 修改为 SqlException
                {
                    HandleDbException(ex);
                    MessageBox.Show("Failed to load data!");
                    return;
                }
            }

            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeUI();
            Application.Current.MainWindow = this; // LoginWindow 构造函数中

        }

        private void InitializeUI()
        {
            showpw.IsChecked = false;
            PassTextbox.Visibility = Visibility.Collapsed;
            this.Closed += (s, e) => Application.Current.Shutdown();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;

            if (clickedButton != null)
            {
                if (clickedButton.Content.ToString() == "登录")
                {
                    if (ValidateInput())
                    {
                        await HandleLoginAsync();
                    }
                }
                else if (clickedButton.Content.ToString() == "忘记密码")
                {
                    await HandleForgotPasswordAsync();
                }
            }
        }

        private async Task HandleLoginAsync()
        {
            string userid = UserIdBox.Text;
            string password = Passwordbox.Password;

            string role = await GetUserRoleAndValidatePasswordAsync(userid, password);
            if (string.IsNullOrEmpty(role))
            {
                ShowErrorMessage("User does not exist or password is incorrect.");
                Passwordbox.Clear(); // 清空密码框
            }
            else
            {
                this.Hide();
                Application.Current.MainWindow = CreateRoleWindow(role, userid);
                Application.Current.MainWindow.Show();
            }
        }
        private async Task HandleForgotPasswordAsync()
        {
            string userid = UserIdBox.Text;

            if (string.IsNullOrWhiteSpace(userid))
            {
                ShowErrorMessage("User ID cannot be empty for password reset.");
                return;
            }

            string role = await GetUserRoleAsync(userid);
            if (string.IsNullOrEmpty(role))
            {
                ShowErrorMessage("This user does not exist.");
                return;
            }

            bool ifChangePw = await CheckIfChangePwAsync(role, userid);
            if (!ifChangePw)
            {
                ShowErrorMessage("You have not changed your password before.");
                return;
            }

            // 获取验证问题和答案
            string question = await GetSecurityQuestionAsync(role, userid);
            if (question == null)
            {
                ShowErrorMessage("Failed to retrieve security question."); // 处理数据库错误
                return;
            }

            // 显示 Forgetpw 窗口，并传入问题
            Forgetpw forgetPwWindow = new Forgetpw(_connection, userid, question);
            bool? result = forgetPwWindow.ShowDialog();

            if (result == true) // 用户在 Forgetpw 窗口点击了确认
            {
                // Forgetpw 窗口已经验证了答案，这里可以直接显示密码
                string password = await GetPasswordAsync(role, userid);
                if (password == null)
                {
                    ShowErrorMessage("Failed to retrieve password."); // 处理数据库错误
                }
                else
                {
                    MessageBox.Show($"Your password is: {password}");
                }
            }
        }

        private async Task<string> GetSecurityQuestionAsync(string role, string userid)
        {
            string query = "";

            if (role == "student")
            {
                query = "SELECT question FROM   student WHERE sid = @userid;";
            }
            else if (role == "teacher")
            {
                query = "SELECT question FROM   teacher WHERE ttid = @userid;";
            }
            else if (role == "admin")
            {
                query = "SELECT question FROM   manager WHERE mid = @userid;";
            }

            using (var cmd = new SqlCommand(query, _connection)) // 使用 SqlCommand
            {
                cmd.Parameters.AddWithValue("@userid", userid);
                return await cmd.ExecuteScalarAsync() as string;
            }
        }

        private async Task<string> GetPasswordAsync(string role, string userid)
        {
            string query = "";

            if (role == "student")
            {
                query = "SELECT password FROM   student WHERE sid = @userid;";
            }
            else if (role == "teacher")
            {
                query = "SELECT password FROM   teacher WHERE ttid = @userid;";
            }
            else if (role == "admin")
            {
                query = "SELECT password FROM   manager WHERE mid = @userid;";
            }

            using (var cmd = new SqlCommand(query, _connection)) // 使用 SqlCommand
            {
                cmd.Parameters.AddWithValue("@userid", userid);
                return await cmd.ExecuteScalarAsync() as string;
            }
        }

        private async Task<string> GetUserRoleAsync(string userid)
        {
            string query = @"
            SELECT 
                CASE 
                    WHEN EXISTS (SELECT 1 FROM   student WHERE sid = @userid) THEN 'student'
                    WHEN EXISTS (SELECT 1 FROM   teacher WHERE ttid = @userid) THEN 'teacher'
                    WHEN EXISTS (SELECT 1 FROM   manager WHERE mid = @userid) THEN 'admin'
                    ELSE NULL 
                END AS role;";

            using (var cmd = new SqlCommand(query, _connection)) // 使用 SqlCommand
            {
                cmd.Parameters.AddWithValue("@userid", userid);
                return await cmd.ExecuteScalarAsync() as string;
            }
        }

        private async Task<string> GetUserRoleAndValidatePasswordAsync(string userid, string password)
        {
            string query = @"
            SELECT 
                CASE 
                    WHEN EXISTS (SELECT 1 FROM   student WHERE sid = @userid AND password = @password) THEN 'student'
                    WHEN EXISTS (SELECT 1 FROM   teacher WHERE ttid = @userid AND password = @password) THEN 'teacher'
                    WHEN EXISTS (SELECT 1 FROM   manager WHERE mid = @userid AND password = @password) THEN 'admin'
                    ELSE NULL 
                END AS role;";

            using (var cmd = new SqlCommand(query, _connection)) // 使用 SqlCommand
            {
                cmd.Parameters.AddWithValue("@userid", userid);
                cmd.Parameters.AddWithValue("@password", password); // 使用明文密码

                object result = await cmd.ExecuteScalarAsync();
                // return result != null ? result.ToString() : null;
                return result?.ToString();
            }
        }

        private async Task<bool> CheckIfChangePwAsync(string role, string userid)
        {
            string query = "";

            if (role == "student")
            {
                query = "SELECT ifchangepw FROM   student WHERE sid = @userid;";
            }
            else if (role == "teacher")
            {
                query = "SELECT ifchangepw FROM   teacher WHERE ttid = @userid;";
            }
            else if (role == "admin")
            {
                query = "SELECT ifchangepw FROM   manager WHERE mid = @userid;";
            }

            using (var cmd = new SqlCommand(query, _connection)) // 使用 SqlCommand
            {
                cmd.Parameters.AddWithValue("@userid", userid);
                object result = await cmd.ExecuteScalarAsync();
                return result != null && result.ToString() == "T";
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(UserIdBox.Text))
            {
                ShowErrorMessage("User ID cannot be empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Passwordbox.Password))
            {
                ShowErrorMessage("Password cannot be empty.");
                return false;
            }

            return true;
        }

        private Window CreateRoleWindow(string role, string userId)
        {
            string query = "SELECT name FROM   student WHERE sid = @userid;";
            userName = query;
            using (var command = new SqlCommand(query, _connection)) // 使用 SqlCommand
            {
                command.Parameters.AddWithValue("@userid", userId);

                // 打开连接，如果尚未打开
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Open();
                }

                using (var reader = command.ExecuteReader()) // 使用 ExecuteReader
                {
                    if (reader.Read()) // 读取结果
                    {
                        userName = reader.GetString(0); // 获取姓名
                    }
                }
            }
            if (role == "student")
            {
                return new StudentMainWindow(_connection, userId, userName);
            }
            else if (role == "teacher")
            {
                return new TeacherMainWindow(_connection, userId,userName);
            }
            else if (role == "admin")
            {
                return new AdminMainWindow(_connection, userId, userName);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(role));
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (showpw.IsChecked == true)
            {
                PassTextbox.Text = Passwordbox.Password;
                PassTextbox.Visibility = Visibility.Visible;
                Passwordbox.Visibility = Visibility.Collapsed;
            }
        }

        private void showpw_Unchecked(object sender, RoutedEventArgs e)
        {
            if (showpw.IsChecked == false)
            {
                Passwordbox.Password = PassTextbox.Text;
                Passwordbox.Visibility = Visibility.Visible;
                PassTextbox.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void HandleDbException(SqlException ex) // 修改为 SqlException
        {
            string errorMsg = ex.Message.Contains("connection failed") ?
                "Failed to connect to the database. Please check the network and server status." :
                "SQL query error. Please contact the administrator.";

            LogError($"Database error: {ex.Message}");
            ShowErrorMessage(errorMsg);
        }

        private void LogError(string message)
        {
            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
            System.IO.File.AppendAllText(logPath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
    }
}
using courseselect.Example;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient; // 使用 Microsoft.Data.SqlClient
using System.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;
using System.Text;
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
                    MessageBox.Show("数据加载成功！");
                }
                catch (SqlException ex) // 修改为 SqlException
                {
                    HandleDbException(ex);
                    MessageBox.Show("数据加载失败！");
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
                
            }
        }

        private async Task HandleLoginAsync()
        {
            string userid = UserIdBox.Text;
            string password = showpw.IsChecked == true ? PassTextbox.Text : Passwordbox.Password;


            string role = await GetUserRoleAndValidatePasswordAsync(userid, password);
            if (string.IsNullOrEmpty(role))
            {
                ShowErrorMessage("用户不存在或密码错误！");
                Passwordbox.Clear(); // 清空密码框
            }
            else
            {
                this.Hide();
                Application.Current.MainWindow = CreateRoleWindow(role, userid);
                Application.Current.MainWindow.Show();
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
        SELECT role, ifchangepw, password FROM (
            SELECT 'student' AS role, ifchangepw, password FROM student WHERE sid = @userid
            UNION ALL
            SELECT 'teacher' AS role, ifchangepw, password FROM teacher WHERE ttid = @userid
            UNION ALL
            SELECT 'admin' AS role, ifchangepw, password FROM manager WHERE mid = @userid
        ) AS CombinedUser
    ";

            using (var cmd = new SqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@userid", userid);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        string role = reader.GetString(0);
                        string ifchangepw = reader.GetString(1);
                        string storedPassword = reader.GetString(2);

                        if (ifchangepw == "T")
                        {
                            // 加密后比对
                            string hashedInput = PasswordHelper.ComputeSha256Hash(password);
                            if (hashedInput == storedPassword)
                                return role;
                        }
                        else
                        {
                            // 明文直接比对
                            if (password == storedPassword)
                                return role;
                        }
                    }
                }
            }

            return null;
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
                ShowErrorMessage("用户账号不能为空！");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Passwordbox.Password))
            {
                ShowErrorMessage("密码不能为空！");
                return false;
            }

            return true;
        }

        private Window CreateRoleWindow(string role, string userId)
        {
            string query = "";
            string idColumn = "";
            string name = "";

            if (role == "student")
            {
                query = "SELECT name FROM student WHERE sid = @userid;";
                idColumn = "sid";
            }
            else if (role == "teacher")
            {
                query = "SELECT name FROM teacher WHERE ttid = @userid;";
                idColumn = "ttid";
            }
            else if (role == "admin")
            {
                query = "SELECT name FROM manager WHERE mid = @userid;";
                idColumn = "mid";
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(role));
            }

            using (var command = new SqlCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@userid", userId);

                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Open();
                }

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        name = reader.GetString(0); // 获取用户姓名
                    }
                }
            }

            userName = name;

            if (role == "student")
            {
                return new StudentMainWindow(_connection, userId, userName);
            }
            else if (role == "teacher")
            {
                return new TeacherMainWindow(_connection, userId, userName);
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
            MessageBox.Show(message, "错误提示", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void HandleDbException(SqlException ex) // 修改为 SqlException
        {
            string errorMsg = ex.Message.Contains("connection failed") ?
                "无法连接到数据库， 请检查网络和服务器状况。" :
                "SQL 查询错误，请联系管理员。";

            LogError($"数据库错误： {ex.Message}");
            ShowErrorMessage(errorMsg);
        }

        private void LogError(string message)
        {
            string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
            System.IO.File.AppendAllText(logPath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
     

public static class PasswordHelper
    {
        public static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(rawData);
                byte[] hash = sha256.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder();
                foreach (var b in hash)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }
    }
        public async Task EncryptAllPlainPasswordsAsync(SqlConnection connection)
        {
            await EncryptTablePasswordsAsync(connection, "student", "sid");
            await EncryptTablePasswordsAsync(connection, "teacher", "ttid");
            await EncryptTablePasswordsAsync(connection, "manager", "mid");

            MessageBox.Show("所有明文密码已加密并更新！");
        }

        private async Task EncryptTablePasswordsAsync(SqlConnection connection, string tableName, string idColumn)
        {
            string selectQuery = $"SELECT {idColumn}, password FROM {tableName}";

            using (SqlCommand selectCmd = new SqlCommand(selectQuery, connection))
            using (SqlDataReader reader = await selectCmd.ExecuteReaderAsync())
            {
                var updates = new List<(string id, string encryptedPassword)>();

                while (await reader.ReadAsync())
                {
                    string id = reader[idColumn].ToString();
                    string plainPassword = reader["password"].ToString();

                    // 如果已经是加密的，就跳过（假设长度>=60判断）
                    if (plainPassword.Length >= 60 && !plainPassword.Contains(" ")) continue;

                    string encrypted = PasswordHelper.ComputeSha256Hash(plainPassword);
                    updates.Add((id, encrypted));
                }

                reader.Close();

                // 开始更新
                foreach (var item in updates)
                {
                    string updateQuery = $"UPDATE {tableName} SET password = @password WHERE {idColumn} = @id";
                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, connection))
                    {
                        updateCmd.Parameters.AddWithValue("@password", item.encryptedPassword);
                        updateCmd.Parameters.AddWithValue("@id", item.id);
                        await updateCmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }


    }
}
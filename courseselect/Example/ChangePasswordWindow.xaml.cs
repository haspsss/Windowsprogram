using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace courseselect.Example
{
    public partial class ChangePasswordWindow : Window
    {
        private readonly SqlConnection _connection;
        private readonly string _userId;

        public ChangePasswordWindow(SqlConnection connection, string userId)
        {
            InitializeComponent();
            _connection = connection;
            _userId = userId;
           
        }
      

        private void ConfirmChange_Click(object sender, RoutedEventArgs e)
        {
            string oldPassword = txtOldPassword.Text;
            string newPassword = txtNewPassword.Text;
           

            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword))
            {
                MessageBox.Show("所有字段都必须填写！","错误提示",MessageBoxButton.OK ,MessageBoxImage.Error);
                return;
            }

            string dbPassword = null;

            // 读取数据库中原密码
            using (var cmd = new SqlCommand("SELECT password FROM student WHERE sid = @userId", _connection))
            {
                cmd.Parameters.AddWithValue("@userId", _userId);
                dbPassword = cmd.ExecuteScalar()?.ToString();
            }

            if (dbPassword == null)
            {
                MessageBox.Show("用户不存在！", "错误提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 判断旧密码是否匹配（支持明文或已加密的对比）
            bool isMatch = false;
            if (dbPassword.Length < 60)
            {
                // 明文
                isMatch = (dbPassword == oldPassword);
            }
            else
            {
                // 加密
                string hashedInput = ComputeSha256Hash(oldPassword);
                isMatch = (dbPassword == hashedInput);
            }

            if (!isMatch)
            {
                MessageBox.Show("原始密码错误！", "错误提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 将新密码加密后存入数据库
            string encryptedNewPassword = ComputeSha256Hash(newPassword);

            using (var updateCmd = new SqlCommand(
                "UPDATE student SET password = @newPassword, ifchangepw = 'T' WHERE sid = @userId",
                _connection))
            {
                updateCmd.Parameters.AddWithValue("@newPassword", encryptedNewPassword);
              
                updateCmd.Parameters.AddWithValue("@userId", _userId);
                updateCmd.ExecuteNonQuery();
            }

            MessageBox.Show("密码修改成功！","修改成功",MessageBoxButton.OK,MessageBoxImage.Asterisk);
            DialogResult = true;
            this.Close();
        }

        // 加密函数：SHA-256
        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(rawData);
                byte[] hash = sha256.ComputeHash(bytes);

                StringBuilder builder = new StringBuilder();
                foreach (var b in hash)
                    builder.Append(b.ToString("x2")); // 转为16进制
                return builder.ToString();
            }
        }
    }
}

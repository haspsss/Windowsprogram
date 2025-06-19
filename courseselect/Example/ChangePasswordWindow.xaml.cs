using System.Data.SqlClient;
using System.Windows;

namespace courseselect.Example // 注意命名空间
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
            string question = txtQuestion.Text;
            string answer = txtAnswer.Text;

            // 验证输入（可以根据需要添加更多验证）
            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(question) || string.IsNullOrEmpty(answer))
            {
                MessageBox.Show("所有字段都必须填写！");
                return;
            }


            using (var cmd = new SqlCommand("SELECT password FROM student WHERE sid = @userId", _connection)) // 注意双引号转义
            {
                cmd.Parameters.AddWithValue("userId", _userId); // 添加参数
                string dbPassword = cmd.ExecuteScalar()?.ToString();

                if (dbPassword != oldPassword)
                {
                    MessageBox.Show("原始密码错误！");
                    return;
                }

                using (var updateCmd = new SqlCommand("UPDATE student SET password = @newPassword, ifchangepw = 'T', question = @question, answer = @answer WHERE sid = @userId", _connection)) // 注意双引号转义
                {
                    updateCmd.Parameters.AddWithValue("newPassword", newPassword);
                    updateCmd.Parameters.AddWithValue("question", question);
                    updateCmd.Parameters.AddWithValue("answer", answer);
                    updateCmd.Parameters.AddWithValue("userId", _userId);
                    updateCmd.ExecuteNonQuery();
                }

                DialogResult = true;
                MessageBox.Show("密码修改成功！");
                this.Close();
            }
        }
    }
}

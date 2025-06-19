
using System.Threading.Tasks;
using System.Windows;
using System.Data.SqlClient;

namespace courseselect.Example
{
    public partial class Forgetpw : Window
    {
        private readonly SqlConnection _connection;
        private readonly string _userId;
        private readonly string _question;


        public Forgetpw(SqlConnection connection, string userId, string question)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _connection = connection;
            _userId = userId;
            _question = question;

            // 显示问题
            Question.Text = _question; // 假设你有一个名为 lblQuestion 的 Label 控件
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e) // 确认按钮点击事件
        {
            string answer = Answer.Text; // 获取用户输入的答案

            string correctAnswer = await GetCorrectAnswerAsync(_userId); // 从数据库获取正确答案

            if (answer == correctAnswer)
            {
                DialogResult = true; // 设置 DialogResult 为 true，表示验证成功
                this.Close();
            }
            else
            {
                MessageBox.Show("答案错误！");
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
                END AS role";

            using (var cmd = new SqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@userid", userid);
                return await cmd.ExecuteScalarAsync() as string;
            }
        }
        private async Task<string> GetCorrectAnswerAsync(string userId)
        {
            string role = await GetUserRoleAsync(userId); // 获取用户角色

            if (string.IsNullOrEmpty(role))
            {
                // 处理用户不存在的情况，例如返回 null 或抛出异常
                return null;
            }

            string query = "";

            if (role == "student")
            {
                query = "SELECT answer FROM student WHERE sid = @userid";
            }
            else if (role == "teacher")
            {
                query = "SELECT answer FROM teacher WHERE ttid = @userid";
            }
            else if (role == "admin")
            {
                query = "SELECT answer FROM manager WHERE mid = @userid";
            }
            else
            {
                // 处理未知角色的情况，例如返回 null 或抛出异常
                return null;
            }

            using (var cmd = new SqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@userid", userId);
                return await cmd.ExecuteScalarAsync() as string;
            }
        }
        
    }
}
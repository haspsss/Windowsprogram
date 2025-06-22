
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace courseselect.Example.Student.PersonalInformation
{
    public partial class MyInformation : Page
    {
        private SqlConnection _connection;
        private string _userId;

        public MyInformation(SqlConnection connection, string userId)
        {
            InitializeComponent();
            _connection = connection;
            _userId = userId;
            LoadData(); // 初始化连接和 userId 后加载数据
        }

        private void LoadData()
        {
            string query = "SELECT sid, name, sex, birth, college, major, political, phone FROM student WHERE sid = @userId";

            using (var cmd = new SqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@userId", _userId);

                try
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // 从数据库读取数据并填充到 TextBox 中
                            SidTextBox.Text = reader["sid"].ToString();
                            NameTextBox.Text = reader["name"].ToString();
                            string sexCode = reader["sex"].ToString();
                            SexTextBox.Text = sexCode == "M" ? "男" : sexCode == "F" ? "女" : "未知";

                            BirthTextBox.Text = Convert.ToDateTime(reader["birth"]).ToString("yyyy-MM-dd"); // 格式化日期
                            CollegeTextBox.Text = reader["college"].ToString();
                            MajorTextBox.Text = reader["major"].ToString();
                            PoliticalTextBox.Text = reader["political"].ToString();
                            PhoneTextBox.Text = reader["phone"].ToString();
                        }
                        else
                        {
                            System.Windows.MessageBox.Show("未找到用户信息。");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"加载数据时出错: {ex.Message}");
                }
            }
        }

     
             private void Changepwbtn_Click(object sender, RoutedEventArgs e)
        {
            ChangePasswordWindow changePasswordWindow = new ChangePasswordWindow(_connection, _userId);
            changePasswordWindow.Owner = Window.GetWindow(this); // 设置拥有者窗口
            changePasswordWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner; // 设置窗口位置
            bool? result = changePasswordWindow.ShowDialog();

            if (result == true) // 修改成功
            {
                // 可选：刷新用户信息显示
                LoadData();
            }
        }
    

      
    }
}
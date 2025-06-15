using System;
using System.Windows;
using Npgsql;

namespace courseselect.Example
{
    public partial class Forgetpw : Window
    {
        private NpgsqlConnection _connection;

        public Forgetpw(NpgsqlConnection connection)
        {
            InitializeComponent();
            _connection = connection;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            string accountId = AccountIdTextBox.Text;

            // 使用共享连接进行查询
            string queryUser = "SELECT ifchangepw FROM \"SInformation\".\"student\" WHERE sid = @accountId " +
                               "UNION ALL " +
                               "SELECT ifchangepw FROM \"SInformation\".\"manager\" WHERE mid = @accountId " +
                               "UNION ALL " +
                               "SELECT ifchangepw FROM \"SInformation\".\"teacher\" WHERE tid = @accountId";

            using (var cmd = new NpgsqlCommand(queryUser, _connection))
            {
                cmd.Parameters.AddWithValue("@accountId", accountId);
                object result = cmd.ExecuteScalar();

                if (result == null)
                {
                    MessageBox.Show("该账号不存在！");
                }
                else
                {
                    string ifChangePw = result.ToString();
                    if (ifChangePw == "F")
                    {
                        MessageBox.Show("您未修改过密码！");
                    }
                    else if (ifChangePw == "T")
                    {
                        // 刷新 Forgetpw 窗口内容
                        MessageTextBlock.Text = "仍需编写";
                    }
                }
            }
        }
    }
}
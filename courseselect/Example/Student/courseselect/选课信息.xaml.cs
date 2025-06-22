using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace courseselect.Example.Student.courseselect
{
    public partial class 选课信息 : Page
    {
        private readonly SqlConnection _connection;
        private readonly string _userId;

        public 选课信息(SqlConnection connection, string userId)
        {
            InitializeComponent();
            _connection = connection;
            _userId = userId;
            LoadCourseInformation();
        }

        private void LoadCourseInformation()
        {
            try
            {
                string query = @"
                SELECT c.cid, c.cname, c.ctime, c.cplace, c.ccredit,c.cweek,c.cday,c.ccollege,c.ctest,c.ckind,c.cyear, 
                       t.name AS TeacherName, 
                       t.college AS TeacherCollege, 
                       t.department AS TeacherDepartment
                FROM scclass sc
                JOIN course c ON sc.sc_cid = c.cid
                JOIN teaching_course tc ON c.cid = tc.cid
                JOIN teacher t ON tc.ttid = t.ttid
                WHERE sc.sc_sid = @UserId";

                using (SqlCommand command = new SqlCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("@UserId", _userId);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    CourseInfoDataGrid.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载选课信息时出错: {ex.Message}");
            }
        }
    }
}
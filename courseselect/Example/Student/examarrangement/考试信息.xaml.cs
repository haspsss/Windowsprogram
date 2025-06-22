using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace courseselect.Example.Student.考试安排
{
    /// <summary>
    /// 考试信息.xaml 的交互逻辑
    /// </summary>
    public class ExamCourse : INotifyPropertyChanged
    {
        public string CID { get; set; }
        public string CName { get; set; }
        public DateTime? CTestTime { get; set; }
        public DateTime? CTestTimeEnd { get; set; }
        public string TeacherName { get; set; }
        public string ExamStatus { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public partial class 考试信息 : Page, INotifyPropertyChanged
    {
        public ObservableCollection<ExamCourse> ExamCourses { get; set; } = new ObservableCollection<ExamCourse>();

        public 考试信息(SqlConnection connection, string studentId)
        {
            InitializeComponent();
            LoadExamCourses(connection, studentId);
            DataContext = this;
        }

        private void LoadExamCourses(SqlConnection conn, string studentId)
        {
            string sql = @"
                SELECT c.cid, c.cname, c.ctesttime, c.ctesttimeend, tc.tname
                FROM scclass sc
                JOIN course c ON sc.sc_cid = c.cid
                JOIN teaching_course tc ON tc.cid = c.cid
                WHERE sc.sc_sid = @StudentId AND c.ctest = N'考试'";

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@StudentId", studentId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var course = new ExamCourse
                        {
                            CID = reader["cid"].ToString(),
                            CName = reader["cname"].ToString(),
                            TeacherName = reader["tname"].ToString(),
                            CTestTime = reader["ctesttime"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ctesttime"]) : null,
                            CTestTimeEnd = reader["ctesttimeend"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ctesttimeend"]) : null
                        };

                        course.ExamStatus = GetExamStatus(course.CTestTime, course.CTestTimeEnd);
                        ExamCourses.Add(course);
                    }
                }
            }

            OnPropertyChanged(nameof(ExamCourses));
        }

        private string GetExamStatus(DateTime? start, DateTime? end)
        {
            DateTime now = DateTime.Now;
            if (!start.HasValue || !end.HasValue)
                return "未知";

            if (now < start.Value)
                return "未开始";
            else if (now >= start.Value && now <= end.Value)
                return "进行中";
            else
                return "已完成";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

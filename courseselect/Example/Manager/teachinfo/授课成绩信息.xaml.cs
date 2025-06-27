using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Globalization;
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

namespace courseselect.Example.Manager.teachinfo
{
    /// <summary>
    /// 授课成绩信息.xaml 的交互逻辑
    /// </summary>
    public class StudentInfo : INotifyPropertyChanged
    {
        public string Sid { get; set; }
        public string Name { get; set; }

        private double usualScore;
        public double UsualScore
        {
            get => usualScore;
            set
            {
                if (usualScore != value)
                {
                    usualScore = value;
                    OnPropertyChanged(nameof(UsualScore));
                    CalculateFinalScore();
                }
            }
        }

        private double examScore;
        public double ExamScore
        {
            get => examScore;
            set
            {
                if (examScore != value)
                {
                    examScore = value;
                    OnPropertyChanged(nameof(ExamScore));
                    CalculateFinalScore();
                }
            }
        }

        private double finalScore;
        public double FinalScore
        {
            get => finalScore;
            set
            {
                if (finalScore != value)
                {
                    finalScore = value;
                    OnPropertyChanged(nameof(FinalScore));
                    CalculateFinalScore(); 
                }
            }
        }

        public double UsualRatio { get; set; } // 来自课程表
        public double ExamRatio { get; set; }  // 来自课程表

        public void CalculateFinalScore()
        {
            FinalScore = Math.Round(UsualScore * UsualRatio + ExamScore * ExamRatio, 1);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }


    public class NCourseInfo : INotifyPropertyChanged
    {
        public string Cid { get; set; }
        public string Cname { get; set; }
        public string Ckind { get; set; }
        public string Ccollege { get; set; }
        public double UsualRatio { get; set; }
        public double ExamRatio { get; set; }
        public bool HasNoStudents => Students == null || Students.Count == 0;
        public ObservableCollection<StudentInfo> Students { get; set; } = new ObservableCollection<StudentInfo>();

        public double UsualAvg => Students.Count > 0 ? Students.Average(s => s.UsualScore) : 0;
        public double ExamAvg => Students.Count > 0 ? Students.Average(s => s.ExamScore) : 0;
        public double FinalAvg => Students.Count > 0 ? Students.Average(s => s.FinalScore) : 0;

        public double UsualMax => Students.Count > 0 ? Students.Max(s => s.UsualScore) : 0;
        public double ExamMax => Students.Count > 0 ? Students.Max(s => s.ExamScore) : 0;
        public double FinalMax => Students.Count > 0 ? Students.Max(s => s.FinalScore) : 0;

        public double UsualMin => Students.Count > 0 ? Students.Min(s => s.UsualScore) : 0;
        public double ExamMin => Students.Count > 0 ? Students.Min(s => s.ExamScore) : 0;
        public double FinalMin => Students.Count > 0 ? Students.Min(s => s.FinalScore) : 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


    public class NTeacherInfo
    {
        public string Ttid { get; set; }
        public string Name { get; set; }
        public string SexDisplay { get; set; }
        public string College { get; set; }
        public string Department { get; set; }
        public bool HasNoCourses => Courses == null || Courses.Count == 0;
        // 课程备份（用于恢复）
        public List<NCourseInfo> AllCourses { get; set; } = new List<NCourseInfo>();
        public ObservableCollection<NCourseInfo> Courses { get; set; } = new ObservableCollection<NCourseInfo>();
    }


    public partial class 授课成绩信息 : Page
    {
        private SqlConnection _connection;
        public ObservableCollection<NTeacherInfo> AllTeachers { get; set; } = new ObservableCollection<NTeacherInfo>();

        public 授课成绩信息(SqlConnection connection)
        {
            InitializeComponent();
            _connection = connection;
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open(); // ✅ 添加这个
            }

            this.DataContext = this;

            LoadTeacherData();
        }

        private void LoadTeacherData()
        {
            AllTeachers.Clear();

            string teacherQuery = @"SELECT ttid, name, sex, college, department FROM teacher";
            using (SqlCommand cmd = new SqlCommand(teacherQuery, _connection))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string sexRaw = reader["sex"].ToString();
                    string sexDisplay = sexRaw == "M" ? "男" : sexRaw == "F" ? "女" : sexRaw;

                    NTeacherInfo teacher = new NTeacherInfo
                    {
                        Ttid = reader["ttid"].ToString(),
                        Name = reader["name"].ToString(),
                        SexDisplay = sexDisplay,
                        College = reader["college"].ToString(),
                        Department = reader["department"].ToString()
                    };

                    AllTeachers.Add(teacher);
                }
            }

            foreach (var teacher in AllTeachers)
            {
                string courseQuery = @"
                    SELECT c.cid, c.cname, c.ckind, c.ccollege
                    FROM teaching_course tc
                    JOIN course c ON tc.cid = c.cid
                    WHERE tc.ttid = @ttid";
                using (SqlCommand cmd = new SqlCommand(courseQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@ttid", teacher.Ttid);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            NCourseInfo course = new NCourseInfo
                            {
                                Cid = reader["cid"].ToString(),
                                Cname = reader["cname"].ToString(),
                                Ckind = reader["ckind"].ToString(),
                                Ccollege = reader["ccollege"].ToString()
                            };

                            teacher.Courses.Add(course);
                            teacher.AllCourses.Add(course); // ✅ 备份
                        }
                    }
                }
            


            foreach (var course in teacher.Courses)
                {
                    // 查询课程比例（course 表）
                    string ratioQuery = "SELECT usual_ratio, exam_ratio FROM course WHERE cid = @cid";
                    using (SqlCommand ratioCmd = new SqlCommand(ratioQuery, _connection))
                    {
                        ratioCmd.Parameters.AddWithValue("@cid", course.Cid);
                        using (SqlDataReader r = ratioCmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                course.UsualRatio = r["usual_ratio"] != DBNull.Value ? Convert.ToDouble(r["usual_ratio"]) : 0;
                                course.ExamRatio = r["exam_ratio"] != DBNull.Value ? Convert.ToDouble(r["exam_ratio"]) : 0;

                            }
                        }
                    }

                    // 查询学生（scclass 和 student 联表）
                    string stuQuery = @"
SELECT s.sid, s.name, sc.usual_score, sc.exam_score
FROM scclass sc
JOIN student s ON s.sid = sc.sc_sid
WHERE sc.sc_cid = @cid";
                    using (SqlCommand stuCmd = new SqlCommand(stuQuery, _connection))
                    {
                        stuCmd.Parameters.AddWithValue("@cid", course.Cid);
                        using (SqlDataReader reader = stuCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var stu = new StudentInfo
                                {
                                    Sid = reader["sid"].ToString(),
                                    Name = reader["name"].ToString(),
                                    UsualScore = reader["usual_score"] == DBNull.Value ? 0 : Convert.ToDouble(reader["usual_score"]),
                                    ExamScore = reader["exam_score"] == DBNull.Value ? 0 : Convert.ToDouble(reader["exam_score"]),
                                    UsualRatio = course.UsualRatio,
                                    ExamRatio = course.ExamRatio
                                };

                                stu.CalculateFinalScore(); // 初始化
                                course.Students.Add(stu);
                            }
                        }
                    }

                }
            }

            TeacherList.ItemsSource = AllTeachers;
        }





        private void FilterTeacher_Click(object sender, RoutedEventArgs e)
        {
            string keyword = TeacherSearchBox.Text.Trim().ToLower();  // 小写处理做统一匹配

            if (string.IsNullOrEmpty(keyword))
            {
                TeacherList.ItemsSource = AllTeachers;
            }
            else
            {
                var filtered = new ObservableCollection<NTeacherInfo>();

                foreach (var t in AllTeachers)
                {
                    string matchTarget = (t.Ttid + " " + t.Name + " " + t.SexDisplay + " " + t.College + " " + t.Department).ToLower();

                    if (matchTarget.Contains(keyword))
                    {
                        // ✅ 恢复所有课程列表
                        t.Courses.Clear();
                        foreach (var c in t.AllCourses)
                        {
                            t.Courses.Add(c);
                        }

                        // ✅ 重载每门课程的学生列表
                        foreach (var course in t.Courses)
                        {
                            course.Students.Clear();

                            string query = @"
                        SELECT s.sid, s.name, sc.usual_score, sc.exam_score
                        FROM scclass sc
                        JOIN student s ON sc.sc_sid = s.sid
                        WHERE sc.sc_cid = @cid";

                            using (SqlCommand cmd = new SqlCommand(query, _connection))
                            {
                                cmd.Parameters.AddWithValue("@cid", course.Cid);
                                using (SqlDataReader reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var stu = new StudentInfo
                                        {
                                            Sid = reader["sid"].ToString(),
                                            Name = reader["name"].ToString(),
                                            UsualScore = reader["usual_score"] == DBNull.Value ? 0 : Convert.ToDouble(reader["usual_score"]),
                                            ExamScore = reader["exam_score"] == DBNull.Value ? 0 : Convert.ToDouble(reader["exam_score"]),
                                            UsualRatio = course.UsualRatio,
                                            ExamRatio = course.ExamRatio
                                        };
                                        stu.CalculateFinalScore();
                                        course.Students.Add(stu);
                                    }
                                }
                            }
                        }

                        filtered.Add(t);
                    }
                }

                TeacherList.ItemsSource = filtered;
            }
        }



        private void FilterCourse_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var teacher = button.Tag as NTeacherInfo;
            if (teacher == null) return;

            var stackPanel = VisualTreeHelper.GetParent(button) as Panel;
            if (stackPanel == null) return;

            TextBox searchBox = null;
            foreach (var child in stackPanel.Children)
            {
                if (child is TextBox tb)
                {
                    searchBox = tb;
                    break;
                }
            }

            if (searchBox == null) return;

            string keyword = searchBox.Text.Trim().ToLower();

            var allCourses = teacher.AllCourses.ToList();

            var filtered = new ObservableCollection<NCourseInfo>();
            foreach (var course in allCourses)
            {
                string matchTarget = $"{course.Cid} {course.Cname} {course.Ckind} {course.Ccollege}".ToLower();

                if (matchTarget.Contains(keyword))
                {
                    // ✅ 重置学生列表（恢复全部）
                    course.Students.Clear();

                    string query = @"
                SELECT s.sid, s.name, sc.usual_score, sc.exam_score
                FROM scclass sc
                JOIN student s ON sc.sc_sid = s.sid
                WHERE sc.sc_cid = @cid";

                    using (SqlCommand cmd = new SqlCommand(query, _connection))
                    {
                        cmd.Parameters.AddWithValue("@cid", course.Cid);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var stu = new StudentInfo
                                {
                                    Sid = reader["sid"].ToString(),
                                    Name = reader["name"].ToString(),
                                    UsualScore = reader["usual_score"] == DBNull.Value ? 0 : Convert.ToDouble(reader["usual_score"]),
                                    ExamScore = reader["exam_score"] == DBNull.Value ? 0 : Convert.ToDouble(reader["exam_score"]),
                                    UsualRatio = course.UsualRatio,
                                    ExamRatio = course.ExamRatio
                                };
                                stu.CalculateFinalScore();
                                course.Students.Add(stu);
                            }
                        }
                    }

                    filtered.Add(course);
                }
            }

            teacher.Courses.Clear();
            foreach (var item in filtered)
            {
                teacher.Courses.Add(item);
            }
        }





        private void FilterStudent_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
                return;

            var course = button.Tag as NCourseInfo;
            if (course == null)
                return;

            // 查找学生搜索框
            var stackPanel = VisualTreeHelper.GetParent(button) as Panel;
            if (stackPanel == null)
                return;

            TextBox searchBox = null;
            foreach (var child in stackPanel.Children)
            {
                if (child is TextBox tb)
                {
                    searchBox = tb;
                    break;
                }
            }

            if (searchBox == null)
                return;

            string keyword = searchBox.Text.Trim();

            // 原始学生列表重新读取（或用缓存更优）
            ObservableCollection<StudentInfo> allStudents = new ObservableCollection<StudentInfo>();

            string query = @"
        SELECT s.sid, s.name, sc.usual_score, sc.exam_score
        FROM scclass sc
        JOIN student s ON sc.sc_sid = s.sid
        WHERE sc.sc_cid = @cid";

            using (SqlCommand cmd = new SqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@cid", course.Cid);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        StudentInfo stu = new StudentInfo
                        {
                            Sid = reader["sid"].ToString(),
                            Name = reader["name"].ToString(),
                            UsualScore = reader["usual_score"] == DBNull.Value ? 0 : Convert.ToDouble(reader["usual_score"]),
                            ExamScore = reader["exam_score"] == DBNull.Value ? 0 : Convert.ToDouble(reader["exam_score"]),
                            UsualRatio = course.UsualRatio,
                            ExamRatio = course.ExamRatio
                        };
                        stu.CalculateFinalScore();
                        allStudents.Add(stu);
                    }
                }
            }

            // 执行筛选
            var filtered = allStudents.Where(s =>
                s.Sid.Contains(keyword) || s.Name.Contains(keyword)).ToList();

            // 更新现有 Students 列表内容，而不是替换引用
            course.Students.Clear();
            foreach (var s in filtered)
            {
                course.Students.Add(s);
            }

            // ✅ 不刷新 TeacherList.ItemsSource，避免教师或课程筛选丢失
        }

        private void SaveCourseScores_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
                return;

            var course = button.Tag as NCourseInfo;
            if (course == null)
                return;

            foreach (var stu in course.Students)
            {
                stu.UsualRatio = course.UsualRatio;
                stu.ExamRatio = course.ExamRatio;
                stu.CalculateFinalScore();

                string updateQuery = @"
            UPDATE scclass
            SET usual_score = @usual, exam_score = @exam, score = @final
            WHERE sc_sid = @sid AND sc_cid = @cid";

                using (SqlCommand cmd = new SqlCommand(updateQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@usual", stu.UsualScore);
                    cmd.Parameters.AddWithValue("@exam", stu.ExamScore);
                    cmd.Parameters.AddWithValue("@final", stu.FinalScore);
                    cmd.Parameters.AddWithValue("@sid", stu.Sid);
                    cmd.Parameters.AddWithValue("@cid", course.Cid);

                    cmd.ExecuteNonQuery();
                }
            }



            MessageBox.Show($"课程 {course.Cname} 的成绩已保存并更新成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void ClearTeacherFilter_Click(object sender, RoutedEventArgs e)
        {
            TeacherSearchBox.Text = string.Empty;
            TeacherList.ItemsSource = AllTeachers;

            foreach (var t in AllTeachers)
            {
                // 恢复课程
                t.Courses.Clear();
                foreach (var c in t.AllCourses)
                {
                    t.Courses.Add(c);
                }

                // 恢复每门课程的学生
                foreach (var course in t.Courses)
                {
                    course.Students.Clear();

                    string query = @"
                SELECT s.sid, s.name, sc.usual_score, sc.exam_score
                FROM scclass sc
                JOIN student s ON sc.sc_sid = s.sid
                WHERE sc.sc_cid = @cid";

                    using (SqlCommand cmd = new SqlCommand(query, _connection))
                    {
                        cmd.Parameters.AddWithValue("@cid", course.Cid);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var stu = new StudentInfo
                                {
                                    Sid = reader["sid"].ToString(),
                                    Name = reader["name"].ToString(),
                                    UsualScore = reader["usual_score"] == DBNull.Value ? 0 : Convert.ToDouble(reader["usual_score"]),
                                    ExamScore = reader["exam_score"] == DBNull.Value ? 0 : Convert.ToDouble(reader["exam_score"]),
                                    UsualRatio = course.UsualRatio,
                                    ExamRatio = course.ExamRatio
                                };
                                stu.CalculateFinalScore();
                                course.Students.Add(stu);
                            }
                        }
                    }
                }
            }

            // 刷新显示
            TeacherList.ItemsSource = null;
            TeacherList.ItemsSource = AllTeachers;
        }
        private void ClearStudentFilter_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var course = button?.Tag as NCourseInfo;
            if (course == null)
                return;

            // 找到 TextBox 并清空
            var panel = VisualTreeHelper.GetParent(button) as Panel;
            if (panel != null)
            {
                foreach (var child in panel.Children)
                {
                    if (child is TextBox tb)
                    {
                        tb.Text = "";
                        break;
                    }
                }
            }

            // 重新加载所有学生
            course.Students.Clear();
            string query = @"
        SELECT s.sid, s.name, sc.usual_score, sc.exam_score
        FROM scclass sc
        JOIN student s ON sc.sc_sid = s.sid
        WHERE sc.sc_cid = @cid";

            using (SqlCommand cmd = new SqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@cid", course.Cid);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var stu = new StudentInfo
                        {
                            Sid = reader["sid"].ToString(),
                            Name = reader["name"].ToString(),
                            UsualScore = reader["usual_score"] == DBNull.Value ? 0 : Convert.ToDouble(reader["usual_score"]),
                            ExamScore = reader["exam_score"] == DBNull.Value ? 0 : Convert.ToDouble(reader["exam_score"]),
                            UsualRatio = course.UsualRatio,
                            ExamRatio = course.ExamRatio
                        };
                        stu.CalculateFinalScore();
                        course.Students.Add(stu);
                    }
                }
            }

            TeacherList.ItemsSource = null;
            TeacherList.ItemsSource = AllTeachers;
        }

        private void ClearCourseFilter_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
                return;

            var teacher = button.Tag as NTeacherInfo;
            if (teacher == null)
                return;

            // 找到 TextBox 并清空
            var stackPanel = VisualTreeHelper.GetParent(button) as Panel;
            if (stackPanel == null)
                return;

            foreach (var child in stackPanel.Children)
            {
                if (child is TextBox tb)
                {
                    tb.Text = ""; // ✅ 清空课程搜索框
                    break;
                }
            }

            // ✅ 恢复课程列表（从 AllCourses 复制到 Courses，不替换引用）
            teacher.Courses.Clear();
            foreach (var c in teacher.AllCourses)
            {
                teacher.Courses.Add(c);
            }

        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is Visibility v) && v == Visibility.Visible;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace courseselect.Example.Manager.stuinfo
{
    public partial class 学生成绩管理 : Page
    {
        private SqlConnection _connection;
        public ObservableCollection<StudentCourseGroup> GroupedList { get; set; } = new ObservableCollection<StudentCourseGroup>();

        public 学生成绩管理(SqlConnection connection)
        {
            InitializeComponent();
            _connection = connection;
            this.DataContext = this;
            LoadGroupedData();
            LoadFilterOptions();
        }

        private void LoadGroupedData()
        {
            GroupedList.Clear();

            string sql = @"
                SELECT s.sid AS sc_sid, s.name AS sc_sname, s.sex, s.college, s.major,
                       sc.sc_cid, sc.sc_cname, sc.usual_score, sc.exam_score,sc.score, c.ccredit
                FROM scclass sc
                JOIN student s ON s.sid = sc.sc_sid
                JOIN course c ON c.cid = sc.sc_cid
                ORDER BY s.sid";

            Dictionary<string, StudentCourseGroup> map = new Dictionary<string, StudentCourseGroup>();

            using (SqlCommand cmd = new SqlCommand(sql, _connection))
            {
                if (_connection.State != System.Data.ConnectionState.Open)
                    _connection.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string sid = reader["sc_sid"].ToString();

                        if (!map.ContainsKey(sid))
                        {
                            map[sid] = new StudentCourseGroup
                            {
                                Sid = sid,
                                Name = reader["sc_sname"].ToString(),
                                Sex = reader["sex"].ToString(),
                                College = reader["college"].ToString(),
                                Major = reader["major"].ToString()
                            };
                        }
                        map[sid].Courses.Add(new StudentCourse
                        {
                            Cid = reader["sc_cid"].ToString(),
                            Cname = reader["sc_cname"].ToString(),
                            Credit = reader["ccredit"].ToString(),

                            UsualScore = reader["usual_score"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["usual_score"]),
                            ExamScore = reader["exam_score"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["exam_score"]),
                            Score = reader["score"] == DBNull.Value ? null : (double?)Convert.ToDouble(reader["score"])
                        });
                    }
                    }
                }

            foreach (var item in map.Values)
            {
                GroupedList.Add(item);
            }

            StudentGroupList.ItemsSource = GroupedList;
        }

        private void LoadFilterOptions()
        {
            List<string> colleges = new List<string> { "全部" };
            List<string> majors = new List<string> { "全部" };
            List<string> sexes = new List<string> { "全部", "男", "女" };

            using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT college, major FROM student", _connection))
            {
                if (_connection.State != System.Data.ConnectionState.Open)
                    _connection.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string college = reader["college"].ToString();
                        string major = reader["major"].ToString();
                        if (!colleges.Contains(college)) colleges.Add(college);
                        if (!majors.Contains(major)) majors.Add(major);
                    }
                }
            }

            CollegeComboBox.ItemsSource = colleges;
            MajorComboBox.ItemsSource = majors;
            SexComboBox.ItemsSource = sexes;

            CollegeComboBox.SelectedIndex = 0;
            MajorComboBox.SelectedIndex = 0;
            SexComboBox.SelectedIndex = 0;
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            string keyword = SearchBox.Text.Trim().ToLower();
            string selectedCollege = CollegeComboBox.SelectedItem as string;
            string selectedMajor = MajorComboBox.SelectedItem as string;
            string selectedSex = SexComboBox.SelectedItem as string;

            var filtered = GroupedList.Where(g =>
                // 模糊搜索学生基本信息（不包含课程信息）
                (string.IsNullOrWhiteSpace(keyword) ||
                    g.Sid.ToLower().Contains(keyword) ||
                    g.Name.ToLower().Contains(keyword) ||
                    g.SexDisplay.ToLower().Contains(keyword) ||
                    g.College.ToLower().Contains(keyword) ||
                    g.Major.ToLower().Contains(keyword)
                ) &&

                // 下拉框筛选
                (string.IsNullOrWhiteSpace(selectedCollege) || selectedCollege == "全部" || g.College == selectedCollege) &&
                (string.IsNullOrWhiteSpace(selectedMajor) || selectedMajor == "全部" || g.Major == selectedMajor) &&
                (string.IsNullOrWhiteSpace(selectedSex) || selectedSex == "全部" || g.SexDisplay == selectedSex)
            ).ToList();

            StudentGroupList.ItemsSource = filtered;
        }


        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            CollegeComboBox.SelectedIndex = 0;
            MajorComboBox.SelectedIndex = 0;
            SexComboBox.SelectedIndex = 0;
            StudentGroupList.ItemsSource = GroupedList;
        }
    }

    public class StudentCourseGroup
    {
        public string Sid { get; set; }
        public string Name { get; set; }
        public string Sex { get; set; }
        public string SexDisplay => Sex == "F" ? "女" : (Sex == "M" ? "男" : Sex);
        public string College { get; set; }
        public string Major { get; set; }

        public ObservableCollection<StudentCourse> Courses { get; set; } = new ObservableCollection<StudentCourse>();

        public double AvgGPA
        {
            get
            {
                var validScores = Courses
                    .Select(c => c.Score)
                    .Where(s => s.HasValue)
                    .Select(s => s.Value)
                    .ToList();

                if (validScores.Count == 0) return 0;

                double avg = validScores.Average();

                if (avg < 60)
                    return 0; // 低于60分，GPA为0

                double gpa = (avg - 50) * 0.1;

                return Math.Round(gpa, 2);
            }
        }

    }

    public class StudentCourse
    {
        public string Cid { get; set; }
        public string Cname { get; set; }
        public string Credit { get; set; }

        public int? UsualScore { get; set; }
        public int? ExamScore { get; set; }
        public double? Score { get; set; } // float 类型从数据库读取到 double 是安全的
    }


}
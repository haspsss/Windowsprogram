using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace courseselect.Example.Teacher.courseselect
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
                return v != Visibility.Visible;

            return false;
        }
    }
    public partial class 选课信息 : Page, INotifyPropertyChanged
    {
        private readonly SqlConnection _connection;
        private readonly string _userId;

        public ObservableCollection<CourseWithStudents> CourseList { get; set; } = new ObservableCollection<CourseWithStudents>();

        private bool _hasCourses;
        public bool HasCourses
        {
            get => _hasCourses;
            set
            {
                if (_hasCourses != value)
                {
                    _hasCourses = value;
                    OnPropertyChanged();
                }
            }
        }

        public 选课信息(SqlConnection connection, string userId)
        {
            InitializeComponent();
            _connection = connection;
            _userId = userId;
            this.DataContext = this;

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                string sql = @"
                SELECT c.cid, c.cname, c.cyear, c.ccredit, c.ckind, c.ccollege,
                       s.sid, s.name, s.sex, s.college, s.major
                FROM teaching_course tc
                JOIN course c ON tc.cid = c.cid
                LEFT JOIN scclass sc ON sc.sc_cid = c.cid
                LEFT JOIN student s ON s.sid = sc.sc_sid
                WHERE tc.ttid = @ttid
                ORDER BY c.cid";

                using (var cmd = new SqlCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("@ttid", _userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        var courseDict = new Dictionary<string, CourseWithStudents>();

                        while (reader.Read())
                        {
                            string cid = reader["cid"].ToString();

                            if (!courseDict.ContainsKey(cid))
                            {
                                courseDict[cid] = new CourseWithStudents
                                {
                                    Cid = cid,
                                    CName = reader["cname"].ToString(),
                                    CYear = reader["cyear"].ToString(),
                                    CCredit = Convert.ToInt32(reader["ccredit"]),
                                    CKind = reader["ckind"].ToString(),
                                    CCollege = reader["ccollege"].ToString(),
                                    Students = new ObservableCollection<StudentInfo>()
                                };
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("sid")))
                            {
                                courseDict[cid].Students.Add(new StudentInfo
                                {
                                    Sid = reader["sid"].ToString(),
                                    Name = reader["name"].ToString(),
                                    Sex = reader["sex"].ToString() == "M" ? "男" :
                                          reader["sex"].ToString() == "F" ? "女" : "未知",
                                    College = reader["college"].ToString(),
                                    Major = reader["major"].ToString()
                                });
                            }
                        }

                        CourseList.Clear();
                        foreach (var course in courseDict.Values)
                            CourseList.Add(course);

                        HasCourses = CourseList.Count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载选课信息时出错: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public class InverseBooleanToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is bool b)
                    return b ? Visibility.Collapsed : Visibility.Visible;

                return Visibility.Visible;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is Visibility v)
                    return v != Visibility.Visible;

                return false;
            }
        }
        public class StudentInfo
        {
            public string Sid { get; set; }
            public string Name { get; set; }
            public string Sex { get; set; }
            public string College { get; set; }
            public string Major { get; set; }
        }

        public class CourseWithStudents
        {
            public string Cid { get; set; }
            public string CName { get; set; }
            public string CYear { get; set; }
            public int CCredit { get; set; }
            public string CKind { get; set; }
            public string CCollege { get; set; }
            public ObservableCollection<StudentInfo> Students { get; set; }
            public int StudentCount => Students?.Count ?? 0;
            public bool IsExpanded { get; set; }
        }
    }
}

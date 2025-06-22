using System;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing; // For INotifyPropertyChanged
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace courseselect.Example.Teacher.courseselect
{
    public partial class 选课页面 : Page
    {
        private SqlConnection _connection;
        private List<CourseInfo> _allCourses; // 使用 List<CourseInfo>
        private string userId;
        private string userName;


        public 选课页面(SqlConnection connection, string userId, string userName)
        {
            InitializeComponent();
            _connection = connection;
            this.userId = userId;
            this.userName = userName;
            PopulateFilterDropdowns();
            LoadAndDisplayCourses();
        }




        // 计算已选课程的学分

        private void PopulateFilterDropdowns()
        {
            CTimeComboBox.Items.Add("3-4节");
            CTimeComboBox.Items.Add("Afternoon");
            CTimeComboBox.Items.Add("Evening");
            CCollegeComboBox.Items.Add("College of Engineering");
            CCollegeComboBox.Items.Add("College of Science");
            CKindComboBox.Items.Add("Required");
            CKindComboBox.Items.Add("Elective");
            TestComboBox.Items.Add("Exam");
            TestComboBox.Items.Add("Project");
        }

        private void LoadAndDisplayCourses(string searchText = null, string selectedCTime = null, string selectedCCollege = null, string selectedCKind = null, string selectedTest = null, string selectedCWeek = null)
        {
            try
            {
                _allCourses = new List<CourseInfo>();
                string query = @"
                    SELECT
                        c.cid,
                        c.cname,
                        cday,
                        caccomdate,
                        ccredit,
                        cplace,
                      
                        ckind,
                        cintro,
                        ccollege,
                        cdepartment,
                        cmajor,
                        cyear,
                        cweek,
                        ctime,
                        ctestplace,
                        ctest,
                        t.name
                    FROM teaching_course tc
                    JOIN course c ON tc.cid = c.cid
                    JOIN teacher t ON tc.ttid = t.ttid
                    WHERE 1=1";


                List<SqlParameter> parameters = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(searchText))
                {
                    query += " AND c.cname LIKE @SearchText";
                    parameters.Add(new SqlParameter("@SearchText", "%" + searchText + "%"));
                }
                if (!string.IsNullOrEmpty(selectedCTime))
                {
                    query += " AND c.ctime = @CTime";
                    parameters.Add(new SqlParameter("@CTime", selectedCTime));
                }
                if (!string.IsNullOrEmpty(selectedCCollege))
                {
                    query += " AND c.ccollege = @CCollege";
                    parameters.Add(new SqlParameter("@CCollege", selectedCCollege));
                }
                if (!string.IsNullOrEmpty(selectedCKind))
                {
                    query += " AND c.ckind = @CKind";
                    parameters.Add(new SqlParameter("@CKind", selectedCKind));
                }
                if (!string.IsNullOrEmpty(selectedTest))
                {
                    query += " AND c.ctest = @CTest";
                    parameters.Add(new SqlParameter("@CTest", selectedTest));
                }
                if (!string.IsNullOrEmpty(selectedCWeek))
                {
                    query += " AND c.cweek = @CWeek";
                    parameters.Add(new SqlParameter("@CWeek", selectedCWeek));
                }

                using (SqlCommand command = new SqlCommand(query, _connection))
                {
                    command.Parameters.AddRange(parameters.ToArray());

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        _allCourses = dataTable.AsEnumerable()
                            .Where(row => !string.IsNullOrEmpty(row.Field<string>("cid"))).Select(row => new CourseInfo
                            {
                                Cid = row.Field<string>("cid"),
                                CName = row.Field<string>("cname"),
                                CDay = row.Field<string>("cday"),
                                CAccomdate = row.Field<int>("caccomdate"), // 总容量
                                CCredit = row.Field<int>("ccredit"),
                                CPlace = row.Field<string>("cplace"),

                                CKind = row.Field<string>("ckind"),
                                CIntro = row.Field<string>("cintro"),
                                CCollege = row.Field<string>("ccollege"),
                                CDepartment = row.Field<string>("cdepartment"),
                                CMajor = row.Field<string>("cmajor"),
                                CYear = row.Field<string>("cyear"),
                                CWeek = row.Field<string>("cweek"),
                                CTime = row.Field<string>("ctime"),
                                CTestPlace = row.Field<string>("ctestplace"),
                                CTest = row.Field<string>("ctest"),
                                TeacherName = row.Field<string>("name"),
                                RemainingSeats = 0 // 初始化
                            }).ToList();

                        // 计算剩余座位数
                        foreach (var course in _allCourses)
                        {
                            course.RemainingSeats = course.CAccomdate - GetEnrolledCount(course.Cid);
                        }
                        CourseDataGrid.ItemsSource = _allCourses;
                        // 设置所有课程的按钮状态和选中状态

                        CourseDataGrid.Items.Refresh(); // 确保 UI 更新


                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载课程时出错: " + ex.Message);
            }
        }

        private int GetEnrolledCount(string courseId)
        {
            string countQuery = "SELECT COUNT(*) FROM scclass WHERE sc_cid = @CourseId";
            using (var command = new SqlCommand(countQuery, _connection))
            {
                command.Parameters.AddWithValue("@CourseId", courseId);
                return (int)command.ExecuteScalar(); // 获取已选人数
            }
        }

        /// <summary>
        /// 初始化所有课程的 IsSelected 状态，并调用 UpdateAllCourseButtonStates 更新按钮状态。
        /// </summary>


        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text;
            LoadAndDisplayCourses(searchText: searchText);
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 获取所有筛选条件的值
            string selectedCTime = CTimeComboBox.SelectedItem as string;
            string selectedCCollege = CCollegeComboBox.SelectedItem as string;
            string selectedCKind = CKindComboBox.SelectedItem as string;
            string selectedTest = TestComboBox.SelectedItem as string;
            string selectedCWeek = CWeekComboBox.SelectedItem as string; // 假设您有 CWeekComboBox

            // 重新加载并显示课程
            LoadAndDisplayCourses(
                searchText: SearchTextBox.Text, // 保持搜索框内容
                selectedCTime: selectedCTime,
                selectedCCollege: selectedCCollege,
                selectedCKind: selectedCKind,
                selectedTest: selectedTest,
                selectedCWeek: selectedCWeek
            );
        }

        // CourseInfo 类
        public class CourseInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            // 现有属性
            public string Cid { get; set; }
            public string CName { get; set; }
            public string CDay { get; set; }
            public int CAccomdate { get; set; } // 课程总容量
            public int CCredit { get; set; }
            public string CPlace { get; set; }
            public string CTestTime { get; set; }
            public string CKind { get; set; }
            public string CIntro { get; set; }
            public string CCollege { get; set; }
            public string CDepartment { get; set; }
            public string CMajor { get; set; }
            public string CYear { get; set; }
            public string CWeek { get; set; } // 用于冲突判断
            public string CTime { get; set; } // 用于冲突判断
            public string CTestPlace { get; set; }
            public string CTest { get; set; }
            public string TeacherName { get; set; }

            // 实现了 INotifyPropertyChanged 的 RemainingSeats 属性
            private int _remainingSeats;


            public int RemainingSeats
            {
                get => _remainingSeats;
                set
                {
                    if (_remainingSeats != value)
                    {
                        _remainingSeats = value;
                        OnPropertyChanged(nameof(RemainingSeats));

                    }

                }
            }

            // 实现了 INotifyPropertyChanged 的 ButtonEnabled 属性
            private bool _buttonEnabled;
            public bool ButtonEnabled
            {
                get => _buttonEnabled;
                set
                {
                    if (_buttonEnabled != value)
                    {
                        _buttonEnabled = value;
                        OnPropertyChanged(nameof(ButtonEnabled));

                    }
                }
            }

            // 实现了 INotifyPropertyChanged 的 IsDeselectButtonEnabled 属性
            private bool _isDeselectButtonEnabled;
            public bool IsDeselectButtonEnabled
            {
                get => _isDeselectButtonEnabled;
                set
                {
                    if (_isDeselectButtonEnabled != value)
                    {
                        _isDeselectButtonEnabled = value;
                        OnPropertyChanged(nameof(IsDeselectButtonEnabled));
                    }
                }
            }
        }
    }
}

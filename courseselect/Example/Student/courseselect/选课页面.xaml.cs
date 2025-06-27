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

namespace courseselect.Example.Student.courseselect
{
    public partial class 选课页面 : Page
    {
        private SqlConnection _connection;
        private List<CourseInfo> _allCourses; // 使用 List<CourseInfo>
        private string userId;
        private string userName;
        private int totalCredits; // 存储总学分
        private int selectedCredits; // 存储已选课程学分

        public 选课页面(SqlConnection connection, string userId, string userName)
        {
            InitializeComponent();
            _connection = connection;
            this.userId = userId;
            this.userName = userName;
            PopulateFilterDropdowns();
            LoadAndDisplayCourses();
        }
        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            CTimeComboBox.SelectedIndex = -1;
            CCollegeComboBox.SelectedIndex = -1;
            CKindComboBox.SelectedIndex = -1;
            TestComboBox.SelectedIndex = -1;
            CWeekComboBox.SelectedIndex = -1;

            LoadAndDisplayCourses();
        }
        private void LoadUserCredits()
        {
            try
            {
                // 获取当前用户的总学分
                string query = @"SELECT credit FROM student WHERE sid = @UserId";

                using (var command = new SqlCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    object result = command.ExecuteScalar();

                    totalCredits = result != null ? Convert.ToInt32(result) : 0;
                    selectedCredits = CalculateSelectedCredits(); // 计算已选学分
                    UpdateCreditsSummary();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取学分信息时出错: " + ex.Message);
            }
        }

        // 计算已选课程的学分
        private int CalculateSelectedCredits()
        {
            return _allCourses.Where(course => course.IsSelected).Sum(course => course.CCredit);
        }

        // 更新学分信息的显示
        private void UpdateCreditsSummary()
        {
            int remainingCredits = totalCredits - selectedCredits;
            CreditsSummaryTextBlock.Text = $"总学分: {totalCredits} | 已选学分: {selectedCredits} | 剩余学分: {remainingCredits}";
        }

        private void PopulateFilterDropdowns()
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                string[] fields = { "ctime", "ccollege", "ckind", "ctest", "cweek" };
                ComboBox[] combos = { CTimeComboBox, CCollegeComboBox, CKindComboBox, TestComboBox, CWeekComboBox };

                for (int i = 0; i < fields.Length; i++)
                {
                    string field = fields[i];
                    ComboBox comboBox = combos[i];

                    string query = $"SELECT DISTINCT {field} FROM course WHERE {field} IS NOT NULL AND {field} != ''";
                    using (SqlCommand cmd = new SqlCommand(query, _connection))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            comboBox.Items.Clear();
                            comboBox.Items.Add("全部"); // 添加“全部”选项

                            while (reader.Read())
                            {
                                string value = reader[field].ToString();
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    comboBox.Items.Add(value);
                                }
                            }
                        }
                    }

                    comboBox.SelectedIndex = 0; // 默认选中“全部”
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载下拉框数据失败: " + ex.Message);
            }
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


                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query += " AND (c.cid LIKE @SearchText OR c.cname LIKE @SearchText OR c.cday LIKE @SearchText OR c.ckind LIKE @SearchText OR c.ccollege LIKE @SearchText OR c.cdepartment LIKE @SearchText OR c.cmajor LIKE @SearchText OR c.cyear LIKE @SearchText OR c.cweek LIKE @SearchText OR c.ctime LIKE @SearchText OR c.ctest LIKE @SearchText OR t.name LIKE @SearchText)";
                    parameters.Add(new SqlParameter("@SearchText", "%" + searchText + "%"));
                }


                if (!string.IsNullOrWhiteSpace(selectedCTime) && selectedCTime != "全部")
                {
                    query += " AND c.ctime = @CTime";
                    parameters.Add(new SqlParameter("@CTime", selectedCTime));
                }

                if (!string.IsNullOrWhiteSpace(selectedCCollege) && selectedCCollege != "全部")
                {
                    query += " AND c.ccollege = @CCollege";
                    parameters.Add(new SqlParameter("@CCollege", selectedCCollege));
                }

                if (!string.IsNullOrWhiteSpace(selectedCKind) && selectedCKind != "全部")
                {
                    query += " AND c.ckind = @CKind";
                    parameters.Add(new SqlParameter("@CKind", selectedCKind));
                }

                if (!string.IsNullOrWhiteSpace(selectedTest) && selectedTest != "全部")
                {
                    query += " AND c.ctest = @CTest";
                    parameters.Add(new SqlParameter("@CTest", selectedTest));
                }

                if (!string.IsNullOrWhiteSpace(selectedCWeek) && selectedCWeek != "全部")
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
                        SetCourseButtonStates();
                        LoadUserCredits(); // 在加载课程后调用以获取和更新学分信息
                        UpdateAllCourseButtonStates();
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
        private void SetCourseButtonStates()
        {
            // 确保连接已打开
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            // 首先，将所有课程的 IsSelected 状态重置为 false
            foreach (var course in _allCourses)
            {
                course.IsSelected = false;
            }

            // 查询用户的选课记录
            string query = @"
                SELECT sc_cid
                FROM scclass
                WHERE sc_sid = @UserId";

            using (var command = new SqlCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                using (var reader = command.ExecuteReader())
                {
                    var selectedCourseCids = new HashSet<string>();
                    while (reader.Read())
                    {
                        selectedCourseCids.Add(reader.GetString(0)); // 添加已选课程ID
                    }

                    // 根据数据库记录设置课程的 IsSelected 状态
                    foreach (var course in _allCourses)
                    {
                        if (selectedCourseCids.Contains(course.Cid))
                        {
                            course.IsSelected = true;
                        }
                    }
                }
            }
            // 根据 IsSelected 状态和冲突情况更新所有课程的按钮状态
            UpdateAllCourseButtonStates();
        }

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

        private void SelectCourse_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var courseToSelect = button.DataContext as CourseInfo;
            if (courseToSelect == null) return;

            if (courseToSelect.RemainingSeats <= 0)
            {
                MessageBox.Show("课程已满，无法选课。");
                return;
            }

            // 在进行数据库操作前，先进行客户端冲突检查
            if (IsCourseConflict(courseToSelect))
            {
                MessageBox.Show("该课程与您已选的其他课程时间冲突，无法选课。");
                return;
            }

            try
            {
                // 确保连接已打开
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Open();
                }

                string insertQuery = @"
                    INSERT INTO scclass (sc_sid, sc_sname, sc_cid, sc_cname)
                    VALUES (@UserId, @UserName, @CourseId, @CourseName)";

                using (SqlCommand command = new SqlCommand(insertQuery, _connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@UserName", userName);
                    command.Parameters.AddWithValue("@CourseId", courseToSelect.Cid);
                    command.Parameters.AddWithValue("@CourseName", courseToSelect.CName);

                    command.ExecuteNonQuery();
                    courseToSelect.RemainingSeats -= 1; // 更新可选人数
                    MessageBox.Show("选课成功！");

                    // 标记课程为已选中
                    courseToSelect.IsSelected = true;

                    // 重新评估所有课程的按钮状态
                    UpdateAllCourseButtonStates();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("选课时出错: " + ex.Message);
            }
        }

       

        /// <summary>
        /// 判断给定课程是否与当前任何已选课程（IsSelected = true）冲突。
        /// 此方法在客户端执行，不查询数据库。
        /// </summary>
        private bool IsCourseConflict(CourseInfo courseToCheck)
        {
            // 遍历所有当前已选课程 (IsSelected 为 true 且不是 courseToCheck 本身)
            foreach (var selectedCourse in _allCourses.Where(c => c.IsSelected && c.Cid != courseToCheck.Cid))
            {
                // 假设冲突判断基于 CWeek, CDay, CTime 完全相同
                if (courseToCheck.CWeek == selectedCourse.CWeek &&
                    courseToCheck.CDay == selectedCourse.CDay &&
                    courseToCheck.CTime == selectedCourse.CTime)
                {
                    return true; // 发现冲突
                }
            }
            return false; // 没有发现冲突
        }

        private void DeselectCourse_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            var courseToDeselect = button.DataContext as CourseInfo;
            if (courseToDeselect == null) return;

            try
            {
                // 确保连接已打开
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Open();
                }

                string deleteQuery = @"
                    DELETE FROM scclass
                    WHERE sc_sid = @UserId AND sc_cid = @CourseId";

                using (SqlCommand command = new SqlCommand(deleteQuery, _connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@CourseId", courseToDeselect.Cid);

                    int affectedRows = command.ExecuteNonQuery();
                    if (affectedRows > 0)
                    {
                        courseToDeselect.RemainingSeats += 1; // 更新可选人数
                        MessageBox.Show("退选成功！");

                        // 标记课程为未选中
                        courseToDeselect.IsSelected = false;

                        // 重新评估所有课程的按钮状态
                        UpdateAllCourseButtonStates();
                    }
                    else
                    {
                        MessageBox.Show("未找到选课记录，退选失败。");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("退选时出错: " + ex.Message);
            }
        }

        /// <summary>
        /// 遍历所有课程，并根据其 IsSelected 状态和冲突状态更新其选课/退课按钮的可用性。
        /// </summary>
        

        private void UpdateAllCourseButtonStates()
        {
            selectedCredits = CalculateSelectedCredits();
            UpdateCreditsSummary();
            foreach (var course in _allCourses)
            {
                if (course.IsSelected) // 如果课程已被选中
                {
                    course.ButtonEnabled = false; // 选课按钮禁用
                    course.IsDeselectButtonEnabled = true; // 退课按钮启用
                }
                else // 如果课程未被选中
                {
                    // 检查该课程是否与任何当前已选课程冲突
                    if (IsCourseConflict(course))
                    {
                        course.ButtonEnabled = false; // 选课按钮禁用 (因冲突)
                        course.IsDeselectButtonEnabled = false; // 退课按钮禁用 (未选中)
                    }
                    else
                    {
                        // 无冲突且未选中，检查是否有剩余座位
                        if (course.RemainingSeats > 0)
                        {
                            // 根据课程的学分与剩余学分的约束来决定按钮的可用性
                            if (course.CCredit > (totalCredits - selectedCredits))
                            {
                                course.ButtonEnabled = false; // 选课按钮禁用 (学分约束)
                            }
                            else
                            {
                                course.ButtonEnabled = true; // 选课按钮启用 (无冲突，可选择)
                            }
                        }
                        else
                        {
                            course.ButtonEnabled = false; // 课程已满，禁用选课按钮
                        }
                        course.IsDeselectButtonEnabled = false; // 退课按钮禁用 (未选中)
                    }
                }
            }

            // 刷新 DataGrid 以反映所有更改
            CourseDataGrid.Items.Refresh();
        }
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

        // 新增的 IsSelected 属性，标记课程是否已被选择
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        // 构造函数，设置默认值
        public CourseInfo()
        {
            // 默认情况下，选课按钮可用，退课按钮不可用，未选中
            ButtonEnabled = true;
            IsDeselectButtonEnabled = false;
            IsSelected = false;
        }
    }
}
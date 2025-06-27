using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace courseselect.Example.Manager.courseselect
{
    public partial class 课程管理 : Page
    {
        public ObservableCollection<CourseInfo> CourseList { get; set; } = new ObservableCollection<CourseInfo>();
        public ObservableCollection<TeacherInfo> TeacherList { get; set; } = new ObservableCollection<TeacherInfo>();
        private readonly SqlConnection _connection;
        private readonly string userId;
        private readonly string userName;
        private List<CourseInfo> OriginalCourseList = new List<CourseInfo>();


        public 课程管理(SqlConnection connection, string userId, string userName)
        {
            InitializeComponent();
            _connection = connection;
            this.userId = userId;
            this.userName = userName;
            this.DataContext = this;
            PopulateFilterDropdowns();
            LoadTeachers();
            LoadCourses();
        }
        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            CTimeComboBox.SelectedItem = null;
            CCollegeComboBox.SelectedItem = null;
            CKindComboBox.SelectedItem = null;
            TestComboBox.SelectedItem = null;
            CWeekComboBox.SelectedItem = null;

            FilterCourses();  // 重新加载所有课程
        }

        private void PopulateFilterDropdowns()
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            PopulateComboBoxFromDatabase("ctime", CTimeComboBox, "上课时间");
            PopulateComboBoxFromDatabase("ccollege", CCollegeComboBox, "开课学院");
            PopulateComboBoxFromDatabase("ckind", CKindComboBox, "课程类型");
            PopulateComboBoxFromDatabase("ctest", TestComboBox, "考核方式");
            PopulateComboBoxFromDatabase("cweek", CWeekComboBox, "上课周次");
        }
        private void PopulateComboBoxFromDatabase(string columnName, ComboBox comboBox, string displayName)
        {
            try
            {
                string query = $"SELECT DISTINCT {columnName} FROM course WHERE {columnName} IS NOT NULL AND {columnName} <> ''";
                using (SqlCommand cmd = new SqlCommand(query, _connection))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    comboBox.Items.Clear();
                    comboBox.Items.Add("全部");

                    while (reader.Read())
                    {
                        string value = reader[columnName].ToString();
                        if (!string.IsNullOrWhiteSpace(value) && !comboBox.Items.Contains(value))
                        {
                            comboBox.Items.Add(value);
                        }
                    }
                }

                comboBox.SelectedIndex = 0; // 默认选中“全部”
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载 {displayName} 下拉框失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void FilterCourses()
        {
            string keyword = SearchTextBox.Text.Trim().ToLower();
            string selectedTime = CTimeComboBox.SelectedItem?.ToString();
            string selectedCollege = CCollegeComboBox.SelectedItem?.ToString();
            string selectedKind = CKindComboBox.SelectedItem?.ToString();
            string selectedTest = TestComboBox.SelectedItem?.ToString();
            string selectedWeek = CWeekComboBox.SelectedItem?.ToString();

            var filtered = OriginalCourseList.Where(c =>
     (string.IsNullOrWhiteSpace(keyword) ||
         (c.Cid?.ToLower().Contains(keyword) ?? false) ||
         (c.CName?.ToLower().Contains(keyword) ?? false) ||
         (c.TName?.ToLower().Contains(keyword) ?? false)) &&
     (string.IsNullOrEmpty(selectedTime) || selectedTime == "全部" ||
         string.Equals(c.CTime?.Trim(), selectedTime.Trim(), StringComparison.OrdinalIgnoreCase)) &&
     (string.IsNullOrEmpty(selectedCollege) || selectedCollege == "全部" || c.CCollege == selectedCollege) &&
     (string.IsNullOrEmpty(selectedKind) || selectedKind == "全部" || c.CKind == selectedKind) &&
     (string.IsNullOrEmpty(selectedTest) || selectedTest == "全部" || c.CTest == selectedTest) &&
     (string.IsNullOrEmpty(selectedWeek) || selectedWeek == "全部" || c.CWeek == selectedWeek)
 ).ToList();


            CourseList.Clear();
            foreach (var course in filtered)
            {
                CourseList.Add(course);
            }
        }


        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterCourses();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FilterCourses();
        }

        private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                FilterCourses();
            }
        }

        private void LoadTeachers()
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            ((App)Application.Current).TeacherList.Clear();
            TeacherList.Clear();

            SqlCommand cmd = new SqlCommand("SELECT ttid, name FROM teacher", _connection);
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                TeacherInfo teacher = new TeacherInfo
                {
                    TTid = reader["ttid"].ToString(),
                    TName = reader["name"].ToString()
                };
                ((App)Application.Current).TeacherList.Add(teacher);
                TeacherList.Add(teacher); // 本地 TeacherList 用于绑定 ComboBox
            }
            reader.Close();
        }


        private void LoadCourses()
        {
            CourseList.Clear();

            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            string sql = @"
                SELECT c.*, tc.ttid, t.name AS tname
                FROM course c
                JOIN teaching_course tc ON c.cid = tc.cid
                JOIN teacher t ON tc.ttid = t.ttid";

            using (SqlCommand cmd = new SqlCommand(sql, _connection))
            {
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        CourseInfo course = new CourseInfo();
                        course.Cid = reader["cid"].ToString();
                        course.CName = reader["cname"].ToString();
                        course.CCollege = reader["ccollege"].ToString();
                        course.CDepartment = reader["cdepartment"].ToString();
                        course.CMajor = reader["cmajor"].ToString();

                        course.CCredit = Convert.ToInt32(reader["ccredit"]);
                        course.CKind = reader["ckind"].ToString();
                        course.CAccomDate = Convert.ToInt32(reader["caccomdate"]);

                        course.CPlace = reader["cplace"].ToString();
                        course.CYear = reader["cyear"].ToString();
                        course.CWeek = reader["cweek"].ToString();
                        course.CDay = reader["cday"].ToString();
                        course.CTime = reader["ctime"].ToString();
                        course.CNum = Convert.ToInt32(reader["cnum"]);
                        course.CLast = Convert.ToInt32(reader["clast"]);
                        course.CTest = reader["ctest"].ToString();
                        course.CTestTime = reader["ctesttime"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ctesttime"]);
                        course.CTestTimeEnd = reader["ctesttimeend"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ctesttimeend"]);
                        course.CTestPlace = reader["ctestplace"].ToString();
                        course.TTid = reader["ttid"].ToString();
                        course.TName = reader["tname"].ToString();

                        CourseList.Add(course);
                    }
                }
            }
            OriginalCourseList = CourseList.ToList();


        }
      

        private void AddCourse_Click(object sender, RoutedEventArgs e)
        {
            CourseList.Add(new CourseInfo());
        }

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            // 1. 统一校验所有课程
            foreach (var course in CourseList)
            {
                if (string.IsNullOrWhiteSpace(course.Cid) ||
      string.IsNullOrWhiteSpace(course.CName) ||
      string.IsNullOrWhiteSpace(course.CCollege) ||
      string.IsNullOrWhiteSpace(course.CDepartment) ||
      string.IsNullOrWhiteSpace(course.CKind) ||
      string.IsNullOrWhiteSpace(course.CPlace) ||
      string.IsNullOrWhiteSpace(course.CYear) ||
      string.IsNullOrWhiteSpace(course.CWeek) ||
      string.IsNullOrWhiteSpace(course.CDay) ||
      course.CNum <= 0 || course.CLast <= 0 ||
      
      string.IsNullOrWhiteSpace(course.CMajor) ||
      course.CAccomDate <= 0 ||
      (course.CTest != "论文" &&
          (!course.CTestTime.HasValue ||
           !course.CTestTimeEnd.HasValue ||
           string.IsNullOrWhiteSpace(course.CTestPlace))))
                {
                    MessageBox.Show("所有字段不能为空，请检查输入！（非论文课程需填写考试时间与地点）","提示",MessageBoxButton.OK,MessageBoxImage.Exclamation);
                    return;
                }


                if (course.CTest != "论文")
                {
                    if (!course.CTestTime.HasValue ||
                        !course.CTestTimeEnd.HasValue ||
                        string.IsNullOrWhiteSpace(course.CTestPlace))
                    {
                        MessageBox.Show("非论文课程必须填写考试时间和考试地点！", "提示", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                }
                if (course.CAccomDate <= 0)
                {
                    MessageBox.Show("课程容纳人数必须为正整数！", "提示", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                if (string.IsNullOrWhiteSpace(course.CMajor))
                {
                    MessageBox.Show("所属专业不能为空！", "提示", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }


            }

            // 2. 全部合法，开始保存
            foreach (var course in CourseList)
            {
                // 如果考试方式是论文，则清空考试时间地点
                if (course.CTest == "论文")
                {
                    course.CTestTime = null;
                    course.CTestTimeEnd = null;
                    course.CTestPlace = null;
                }

                // 保存 course 表
                using (SqlCommand cmd1 = new SqlCommand(@"
MERGE INTO course AS target
USING (SELECT @cid AS cid) AS source
ON target.cid = source.cid
WHEN MATCHED THEN 
    UPDATE SET cname=@cname, ccollege=@ccollege, cdepartment=@cdepartment,cmajor=@cmajor ,ccredit=@ccredit, 
               ckind=@ckind, caccomdate=@caccomdate, cplace=@cplace, cyear=@cyear, cweek=@cweek, 
               cday=@cday,ctime=@ctime, cnum=@cnum, clast=@clast, ctest=@ctest, ctesttime=@ctesttime, 
               ctesttimeend=@ctesttimeend, ctestplace=@ctestplace
WHEN NOT MATCHED THEN 
    INSERT (cid, cname, ccollege, cdepartment, cmajor,ccredit, ckind, caccomdate, cplace, cyear, 
            cweek, cday, ctime,cnum, clast, ctest, ctesttime, ctesttimeend, ctestplace)
    VALUES (@cid, @cname, @ccollege, @cdepartment,@cmajor, @ccredit, @ckind, @caccomdate, @cplace, 
            @cyear, @cweek, @cday, @ctime,@cnum, @clast, @ctest, @ctesttime, @ctesttimeend, @ctestplace);", _connection))
                {
                    cmd1.Parameters.AddWithValue("@cid", course.Cid);
                    cmd1.Parameters.AddWithValue("@cname", course.CName);
                    cmd1.Parameters.AddWithValue("@ccollege", course.CCollege);
                    cmd1.Parameters.AddWithValue("@cdepartment", course.CDepartment);
                    cmd1.Parameters.AddWithValue("@cmajor", course.CMajor);

                    cmd1.Parameters.AddWithValue("@ccredit", course.CCredit);
                    cmd1.Parameters.AddWithValue("@ckind", course.CKind);

                    cmd1.Parameters.AddWithValue("@caccomdate", course.CAccomDate);
                    cmd1.Parameters.AddWithValue("@cplace", course.CPlace);
                    cmd1.Parameters.AddWithValue("@cyear", course.CYear);
                    cmd1.Parameters.AddWithValue("@cweek", course.CWeek);
                    cmd1.Parameters.AddWithValue("@cday", course.CDay);
                    cmd1.Parameters.AddWithValue("@ctime", course.CTime);
                    cmd1.Parameters.AddWithValue("@cnum", course.CNum);
                    cmd1.Parameters.AddWithValue("@clast", course.CLast);

                    SqlParameter p1 = new SqlParameter("@ctest", SqlDbType.NVarChar);
                    p1.Value = string.IsNullOrEmpty(course.CTest) ? (object)DBNull.Value : (object)course.CTest;
                    cmd1.Parameters.Add(p1);

                    SqlParameter p2 = new SqlParameter("@ctesttime", SqlDbType.DateTime);
                    p2.Value = course.CTestTime.HasValue ? (object)course.CTestTime.Value : (object)DBNull.Value;
                    cmd1.Parameters.Add(p2);

                    SqlParameter p3 = new SqlParameter("@ctesttimeend", SqlDbType.DateTime);
                    p3.Value = course.CTestTimeEnd.HasValue ? (object)course.CTestTimeEnd.Value : (object)DBNull.Value;
                    cmd1.Parameters.Add(p3);

                    SqlParameter p4 = new SqlParameter("@ctestplace", SqlDbType.NVarChar);
                    p4.Value = string.IsNullOrEmpty(course.CTestPlace) ? (object)DBNull.Value : (object)course.CTestPlace;
                    cmd1.Parameters.Add(p4);

                    try
                    {
                        cmd1.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show("保存失败！原因：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                }

               
            }

            MessageBox.Show("所有课程信息已成功保存！","提示",MessageBoxButton.OK,MessageBoxImage.Asterisk);
        }
        private void DeleteCourse_Click(object sender, RoutedEventArgs e)
        {
            // 优先使用复选框选择的课程
            var selectedCourses = CourseList.Where(c => c.IsSelected).ToList();

            // 如果复选框未选，则使用 DataGrid 中手动选中的项
            if (selectedCourses.Count == 0)
            {
                selectedCourses = CourseDataGrid.SelectedItems.Cast<CourseInfo>().ToList();
            }

            if (selectedCourses.Count == 0)
            {
                MessageBox.Show("请先选择或勾选要删除的课程！", "提示", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (MessageBox.Show($"确定要删除所选的 {selectedCourses.Count} 条课程记录吗？", "确认删除",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            foreach (var course in selectedCourses.ToList())  // 避免在循环中修改集合
            {
                bool isValid =
                    !string.IsNullOrWhiteSpace(course.Cid) &&
                    !string.IsNullOrWhiteSpace(course.CName) &&
                    !string.IsNullOrWhiteSpace(course.CCollege) &&
                    !string.IsNullOrWhiteSpace(course.CDepartment) &&
                    !string.IsNullOrWhiteSpace(course.CKind) &&
                    !string.IsNullOrWhiteSpace(course.CPlace) &&
                    !string.IsNullOrWhiteSpace(course.CYear) &&
                    !string.IsNullOrWhiteSpace(course.CWeek) &&
                    !string.IsNullOrWhiteSpace(course.CDay) &&
                    course.CNum > 0 &&
                    course.CLast > 0 &&
                    !string.IsNullOrWhiteSpace(course.TTid) &&
                    !string.IsNullOrWhiteSpace(course.TName);

                try
                {
                    if (_connection.State != ConnectionState.Open)
                        _connection.Open();

                    if (isValid)
                    {
                        // 删除 teaching_course
                        using (SqlCommand cmd1 = new SqlCommand("DELETE FROM teaching_course WHERE cid = @cid", _connection))
                        {
                            cmd1.Parameters.AddWithValue("@cid", course.Cid);
                            cmd1.ExecuteNonQuery();
                        }

                        // 删除 course
                        using (SqlCommand cmd2 = new SqlCommand("DELETE FROM course WHERE cid = @cid", _connection))
                        {
                            cmd2.Parameters.AddWithValue("@cid", course.Cid);
                            cmd2.ExecuteNonQuery();
                        }
                    }

                    // UI 删除（不论是否有效）
                    CourseList.Remove(course);
                }
                catch (SqlException ex)
                {
                    MessageBox.Show($"删除课程 {course.Cid} 失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



    }

    public class CourseInfo : INotifyPropertyChanged
    {
        public string Cid { get; set; }
        public string CName { get; set; }
        public string CCollege { get; set; }
        public string CDepartment { get; set; }
        public string CMajor { get; set; }
        public bool IsSelected { get; set; }
        public int CCredit { get; set; }
        public string CKind { get; set; }
        public int CAccomDate { get; set; }
        public string CPlace { get; set; }
        public string CYear { get; set; }
        public string CWeek { get; set; }
        
        public string CDay { get; set; }
        public string CTime { get; set; }
        public int CNum { get; set; }
        
        public int CLast { get; set; }
        public string CTest { get; set; }
        public DateTime? CTestTime { get; set; }
        public DateTime? CTestTimeEnd { get; set; }
        public string CTestPlace { get; set; }



        private string _ttid;
        public string TTid
        {
            get { return _ttid; }
            set
            {
                if (_ttid != value)
                {
                    _ttid = value;

                    // 只在当前 TName 不匹配时才更新
                    var teacher = ((App)Application.Current).TeacherList.FirstOrDefault(t => t.TTid == _ttid);
                    if (teacher != null && TName != teacher.TName)
                    {
                        _tname = teacher.TName;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TName)));
                    }

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TTid)));
                }
            }
        }

        private string _tname;
        public string TName
        {
            get { return _tname; }
            set
            {
                if (_tname != value)
                {
                    _tname = value;

                    // 只在当前 TTid 不匹配时才更新
                    var teacher = ((App)Application.Current).TeacherList.FirstOrDefault(t => t.TName == _tname);
                    if (teacher != null && TTid != teacher.TTid)
                    {
                        _ttid = teacher.TTid;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TTid)));
                    }

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TName)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class TeacherInfo
    {
        public string TTid { get; set; }
        public string TName { get; set; }
        public string Display => $"{TTid} - {TName}";
    }

}

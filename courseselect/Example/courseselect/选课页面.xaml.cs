using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace courseselect.Example.courseselect
{
    public partial class 选课页面 : Page
    {
        private NpgsqlConnection _connection;
        private List<CourseInfo> _allCourses;
        private string userId;
        private string userName; // 新增字段

        public 选课页面(NpgsqlConnection connection, string userId, string userName)
        {
            InitializeComponent();
            _connection = connection;
            this.userId = userId; // 保存用户ID
            this.userName = userName; // 保存用户姓名
            PopulateFilterDropdowns();
            LoadAndDisplayCourses();
        }

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
                _allCourses?.Clear(); // 在加载新数据之前清空列表
                string query = $@"
            SELECT
                c.cid,
                c.cname,
                cday,
                caccomdate,
                ccredit,
                cplace,
                ctesttime,
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
            FROM ""SInformation"".teaching_course tc
            JOIN ""SInformation"".course c ON tc.cid = c.cid
            JOIN ""SInformation"".teacher t ON tc.ttid = t.ttid
            WHERE 1=1";

                List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();

                // 添加查询条件
                if (!string.IsNullOrEmpty(searchText))
                {
                    query += " AND c.cname LIKE @SearchText";
                    parameters.Add(new NpgsqlParameter("@SearchText", "%" + searchText + "%"));
                }

                if (!string.IsNullOrEmpty(selectedCTime))
                {
                    query += " AND c.ctime = @CTime";
                    parameters.Add(new NpgsqlParameter("@CTime", selectedCTime));
                }

                if (!string.IsNullOrEmpty(selectedCCollege))
                {
                    query += " AND c.ccollege = @CCollege";
                    parameters.Add(new NpgsqlParameter("@CCollege", selectedCCollege));
                }

                if (!string.IsNullOrEmpty(selectedCKind))
                {
                    query += " AND c.ckind = @CKind";
                    parameters.Add(new NpgsqlParameter("@CKind", selectedCKind));
                }

                if (!string.IsNullOrEmpty(selectedTest))
                {
                    query += " AND c.ctest = @CTest";
                    parameters.Add(new NpgsqlParameter("@CTest", selectedTest));
                }

                if (!string.IsNullOrEmpty(selectedCWeek))
                {
                    query += " AND c.cweek = @CWeek";
                    parameters.Add(new NpgsqlParameter("@CWeek", selectedCWeek));
                }

                using (NpgsqlCommand command = new NpgsqlCommand(query, _connection))
                {
                    command.Parameters.AddRange(parameters.ToArray());

                    using (NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        // 只添加有效的课程信息
                        _allCourses = dataTable.AsEnumerable()
                     .Where(row => !string.IsNullOrEmpty(row.Field<string>("cid"))).Select(row => new CourseInfo
                     {
                         Cid = row.Field<string>("cid"),
                         CName = row.Field<string>("cname"),
                         CTime = row.Field<string>("ctime"),
                         CCollege = row.Field<string>("ccollege"),
                         CKind = row.Field<string>("ckind"),
                         CCredit = row.Field<int>("ccredit"),
                         CTest = row.Field<string>("ctest"),
                         TeacherName = row.Field<string>("name"),
                         CAccomdate = row.Field<int>("caccomdate"),
                         CWeek = row.Field<string>("cweek"),
                         CDay = row.Field<string>("cday"),
                         ButtonEnabled = true, // 默认可用
                         IsDeselectButtonEnabled = false // 默认退选按钮不可用
                     }).ToList();

                        // 更新所有课程的按钮状态
                        foreach (var course in _allCourses)
                        {
                            course.ButtonEnabled = !IsCourseConflict(course); // 设置选课按钮状态
                            course.IsDeselectButtonEnabled = IsCourseSelected(course); // 设置退选按钮状态
                        }

                        CourseDataGrid.ItemsSource = _allCourses;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载课程时出错: " + ex.Message);
            }
        }

        private bool IsCourseSelected(CourseInfo course)
        {
            string query = @"
        SELECT COUNT(*)
        FROM ""SInformation"".scclass
        WHERE sc_sid = @UserId AND sc_cid = @CourseId";

            using (var command = new NpgsqlCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@CourseId", course.Cid);

                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0; // 如果计数大于0，表示已选择该课程
            }
        }
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text;
            LoadAndDisplayCourses(searchText: searchText);
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedCTime = CTimeComboBox.SelectedItem as string;
            // ...获取其他筛选值...

            LoadAndDisplayCourses(selectedCTime: selectedCTime /*, 其他筛选参数 */);
        }
        private void SelectCourse_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                MessageBox.Show("按钮无效");
                return;
            }
            var course = button.DataContext as CourseInfo; // 获取当前课程信息
            if (course == null)
            {
                MessageBox.Show("未能获取课程信息");
                return;
            }

            // 检查剩余人数
            if (course.CAccomdate <= 0)
            {
                MessageBox.Show("课程已满，无法选课。");
                return;
            }

            try
            {
                string insertQuery = @"
            INSERT INTO ""SInformation"".""scclass"" (sc_sid, sc_sname, sc_cid, sc_cname)
            VALUES (@UserId, @UserName, @CourseId, @CourseName)";

                using (NpgsqlCommand command = new NpgsqlCommand(insertQuery, _connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@UserName", userName);
                    command.Parameters.AddWithValue("@CourseId", course.Cid);
                    command.Parameters.AddWithValue("@CourseName", course.CName);

                    command.ExecuteNonQuery(); // 执行插入
                    MessageBox.Show("选课成功！");
                    // 更新按钮状态
                    course.ButtonEnabled = false; // 禁用选课按钮
                    course.IsDeselectButtonEnabled = true; // 启用退选按钮
                                                           // 禁用所有冲突课程的按钮
                    foreach (var c in _allCourses)
                    {
                        if (IsCourseConflict(c)&&c!=course)
                        {
                            c.ButtonEnabled = false; // 禁用冲突课程的选课按钮按钮
                        }
                    }
                    
                    
                    CourseDataGrid.Items.Refresh(); // 刷新数据绑定
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("选课时出错: " + ex.Message);
            }
        }
        private bool IsCourseConflict(CourseInfo course)
        {
            string query = @"
        SELECT COUNT(*)
        FROM ""SInformation"".scclass sc
        JOIN ""SInformation"".course c ON sc.sc_cid = c.cid
        WHERE sc.sc_sid = @UserId
          AND cweek = @Cweek
          AND cday = @CDay
          AND ctime = @CTime";

            using (var command = new NpgsqlCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Cweek", course.CWeek);
                command.Parameters.AddWithValue("@CDay", course.CDay);
                command.Parameters.AddWithValue("@CTime", course.CTime);

                int conflictCount = Convert.ToInt32(command.ExecuteScalar());
                return conflictCount > 0; // 如果有冲突，则返回 true
            }
        }
        private void DeselectCourse_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                MessageBox.Show("按钮无效");
                return;
            }
            var course = button.DataContext as CourseInfo; // 获取当前课程信息

            try
            {
                string deleteQuery = @"
            DELETE FROM ""SInformation"".""scclass""
            WHERE sc_sid = @UserId AND sc_cid = @CourseId";

                using (NpgsqlCommand command = new NpgsqlCommand(deleteQuery, _connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId); // 替换为实际的用户ID
                    command.Parameters.AddWithValue("@CourseId", course.Cid);

                    int affectedRows = command.ExecuteNonQuery(); // 执行删除
                    if (affectedRows > 0)
                    {
                        MessageBox.Show("退选成功！");
                        // 更新按钮状态
                        course.ButtonEnabled = true; // 启用选课按钮
                        course.IsDeselectButtonEnabled = false; // 禁用退选按钮
                                                                // 检查所有课程并更新冲突状态
                        foreach (var c in _allCourses)
                        {
                            c.ButtonEnabled = !IsCourseConflict(c); // 更新选课按钮状态
                        }

                        CourseDataGrid.Items.Refresh(); // 刷新数据绑定
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
        // 更新剩余人数
        private void UpdateRemainingSeats(string courseId, int change)
        {
            string updateQuery = @"
        UPDATE ""SInformation"".""course""
        SET caccomdate = caccomdate - @Change
        WHERE cid = @CourseId";

            using (NpgsqlCommand command = new NpgsqlCommand(updateQuery, _connection))
            {
                command.Parameters.AddWithValue("@Change", change);
                command.Parameters.AddWithValue("@CourseId", courseId);
                command.ExecuteNonQuery();
            }
        }
    }

    // CourseInfo 类 (应该放在单独的文件中或者在这个文件的内部)
    public class CourseInfo
    {
        public string Cid { get; set; }
        public string CName { get; set; }
        public string CDay { get; set; } //  课程周几
        public int CAccomdate { get; set; } // 课程容纳人数
        public int CCredit { get; set; } // 课程学分
        public string CPlace { get; set; } // 课程地点
        public string CTestTime { get; set; } // 课程考试时间
        public string CKind { get; set; } // 课程类别
        public string CIntro { get; set; } // 课程介绍
        public string CCollege { get; set; } // 课程所属学院
        public string CDepartment { get; set; } // 课程所属系
        public string CMajor { get; set; } // 课程所属专业
        public string CYear { get; set; } // 课程学年
        public string CWeek { get; set; } // 课程第几周到第几周
        public string CTime { get; set; } // 课程第几节到第几节
        public string CTestPlace { get; set; } // 课程考试地点
        public string CTest { get; set; } // 考察方式
        public string TeacherName { get; set; } // 教师姓名
        public bool ButtonEnabled { get; set; } // 新增属性
        public bool IsDeselectButtonEnabled { get; set; } // 退选按钮状态
    }

}

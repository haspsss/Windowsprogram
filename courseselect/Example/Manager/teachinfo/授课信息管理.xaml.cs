using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
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

namespace courseselect.Example.Manager.teachinfo
{
    /// <summary>
    /// 授课信息管理.xaml 的交互逻辑
    /// </summary>
    public class TeacherInfo
    {
        public string Ttid { get; set; }
        public string Name { get; set; }
        public string Sex { get; set; }
        public string College { get; set; }
        public string Department { get; set; }
        public string Display => $"{Ttid} - {Name}";  // 显示学号和姓名
    }

    public class CourseInfo
    {
        public string Cid { get; set; }
        public string Cname { get; set; }
        public string Display => $"{Cid} - {Cname}";  // 显示课程号和课程名
    }



    public class CourseSelectionEntry : INotifyPropertyChanged
    {
        private string _Ttid;
        private string _name;
        private string _sex;
        private string _college;
        private string _Department;
        private string _cid;
        private string _cname;
        private string _score;
        private bool _isNew;

        public string Ttid
        {
            get => _Ttid;
            set { _Ttid = value; OnPropertyChanged("Ttid"); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged("Name"); }
        }

        public string Sex
        {
            get => _sex;
            set
            {
                _sex = value;
                OnPropertyChanged("Sex");
                OnPropertyChanged("SexDisplay"); // 性别改变后 SexDisplay 也变了
            }
        }

        public string College
        {
            get => _college;
            set { _college = value; OnPropertyChanged("College"); }
        }

        public string Department
        {
            get => _Department;
            set { _Department = value; OnPropertyChanged("Department"); }
        }

        public string Cid
        {
            get => _cid;
            set { _cid = value; OnPropertyChanged("Cid"); }
        }

        public string Cname
        {
            get => _cname;
            set { _cname = value; OnPropertyChanged("Cname"); }
        }

        public string Score
        {
            get => _score;
            set { _score = value; OnPropertyChanged("Score"); }
        }

        public bool IsNew
        {
            get => _isNew;
            set { _isNew = value; OnPropertyChanged("IsNew"); }
        }

        public string SexDisplay => Sex == "F" ? "女" : Sex == "M" ? "男" : Sex;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public partial class 授课信息管理 : Page
    {
        private SqlConnection _connection;
        public ObservableCollection<CourseSelectionEntry> SelectionList { get; set; } = new ObservableCollection<CourseSelectionEntry>();
        public ObservableCollection<TeacherInfo>TeacherList { get; set; } = new ObservableCollection<TeacherInfo>();
        public ObservableCollection<CourseInfo> CourseList { get; set; } = new ObservableCollection<CourseInfo>();
        public ObservableCollection<string> CollegeFilterList { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> DepartmentFilterList { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> SexFilterList { get; set; } = new ObservableCollection<string> { "全部", "男", "女" };

        public 授课信息管理(SqlConnection connection)
        {
            InitializeComponent();
            _connection = connection;
            this.DataContext = this;
            LoadStudentAndCourseOptions();
            LoadSelectionData();
            FilterChanged(null, null);
            CourseSelectionGrid.ItemsSource = SelectionList;

        }
        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            string keyword = SearchBox.Text.Trim(); // 获取搜索关键字
            string selectedCollege = CollegeComboBox.SelectedItem?.ToString() ?? "全部"; // 获取选择的学院
            string selectedDepartment = DepartmentComboBox.SelectedItem?.ToString() ?? "全部"; // 获取选择的系
            string selectedSex = SexComboBox.SelectedItem?.ToString() ?? "全部"; // 获取选择的性别

            var filtered = SelectionList.Where(item =>
                // 根据搜索关键字进行过滤，匹配职工号、教师姓名、课程号、课程名称
                (string.IsNullOrEmpty(keyword) ||
                 item.Ttid.Contains(keyword) || // 匹配职工号
                 item.Name.Contains(keyword) || // 匹配教师姓名
                 item.Cid.Contains(keyword) ||  // 匹配课程号
                 item.Cname.Contains(keyword)) && // 匹配课程名称

                // 根据学院、系、性别进行筛选
                (selectedCollege == "全部" || item.College == selectedCollege) &&
                (selectedDepartment == "全部" || item.Department == selectedDepartment) &&
                (selectedSex == "全部" || item.SexDisplay == selectedSex)
            ).ToList();

            // 更新 DataGrid 的数据源
            CourseSelectionGrid.ItemsSource = null;
            CourseSelectionGrid.ItemsSource = filtered;
        }



        // 加载学生和课程的下拉框选项
        private void LoadStudentAndCourseOptions()
        {
           TeacherList.Clear();
            CollegeFilterList.Clear();
            DepartmentFilterList.Clear();
            CollegeFilterList.Add("全部");
            DepartmentFilterList.Add("全部");

            string sql1 = "SELECT ttid, name, sex, college, department FROM teacher";
            using (SqlCommand cmd = new SqlCommand(sql1, _connection))
            {
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string ttid = reader["ttid"].ToString();
                        string name = reader["name"].ToString();
                        string sex = reader["sex"].ToString();
                        string college = reader["college"].ToString();
                        string department = reader["department"].ToString();

                       TeacherList.Add(new TeacherInfo
                        {
                            Ttid = ttid,
                            Name = name,
                            Sex = sex,
                            College = college,
                            Department = department
                        });

                        if (!CollegeFilterList.Contains(college))
                            CollegeFilterList.Add(college);
                        if (!DepartmentFilterList.Contains(department))
                            DepartmentFilterList.Add(department);
                    }
                }
            }

            // 加载课程
            CourseList.Clear();
            string sql2 = @"
        SELECT DISTINCT c.cid, c.cname 
        FROM course c";
            using (SqlCommand cmd = new SqlCommand(sql2, _connection))
            {
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        CourseList.Add(new CourseInfo
                        {
                            Cid = reader["cid"].ToString(),
                            Cname = reader["cname"].ToString()
                        });
                    }
                }
            }

            // 设置下拉框 ItemsSource
            CollegeComboBox.ItemsSource = CollegeFilterList;
            DepartmentComboBox.ItemsSource = DepartmentFilterList;
            SexComboBox.ItemsSource = SexFilterList;

            CollegeComboBox.SelectedIndex = 0;
            DepartmentComboBox.SelectedIndex = 0;
            SexComboBox.SelectedIndex = 0;
        }

        // 加载选课记录
        private void LoadSelectionData()
        {
            SelectionList.Clear();
            string sql = @"
                SELECT t.ttid, t.name, t.sex, t.college, t.department,
                       tc.cid AS cid, tc.cname AS cname
                FROM teaching_course tc
                JOIN teacher t ON tc.ttid = t.ttid";

            using (SqlCommand cmd = new SqlCommand(sql, _connection))
            {
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SelectionList.Add(new CourseSelectionEntry
                        {
                            Ttid = reader["ttid"].ToString(),
                            Name = reader["name"].ToString(),
                            Sex = reader["sex"].ToString(),
                            College = reader["college"].ToString(),
                            Department = reader["Department"].ToString(),
                            Cid = reader["cid"].ToString(),
                            Cname = reader["cname"].ToString(),
                            
                        });
                    }
                }
            }
        }

        // 选择学生后自动填充其信息
        private void TeacherComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is TeacherInfo selectedTeacher)
            {
                var entry = comboBox.DataContext as CourseSelectionEntry;
                if (entry != null)
                {
                    entry.Name = selectedTeacher.Name;
                    entry.Sex = selectedTeacher.Sex;
                    entry.College = selectedTeacher.College;
                    entry.Department = selectedTeacher.Department;
                    // 不需要 Refresh！
                }
            }
        }

        private void CourseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is CourseInfo selectedCourse)
            {
                var entry = comboBox.DataContext as CourseSelectionEntry;
                if (entry != null)
                {
                    entry.Cname = selectedCourse.Cname;
                    // 不需要 Refresh！
                }
            }
        }

        private void SaveSelection_Click(object sender, RoutedEventArgs e)
        {
            var newEntries = SelectionList.Where(x => x.IsNew).ToList();
            bool hasSuccess = false;
            bool hasError = false;
            var failedEntries = new List<CourseSelectionEntry>();

            foreach (var entry in newEntries)
            {
                // 校验是否信息完整
                if (string.IsNullOrWhiteSpace(entry.Ttid) ||
                    string.IsNullOrWhiteSpace(entry.Cid) ||
                    string.IsNullOrWhiteSpace(entry.Name) ||
                    string.IsNullOrWhiteSpace(entry.Cname) ||
                    string.IsNullOrWhiteSpace(entry.College) ||
                    string.IsNullOrWhiteSpace(entry.Department) ||
                    string.IsNullOrWhiteSpace(entry.Sex))
                {
                    hasError = true;
                    failedEntries.Add(entry);
                    continue;
                }

                // 检查是否重复
                string checkSql = "SELECT COUNT(*) FROM teaching_course WHERE ttid = @ttid AND cid = @cid";
                using (SqlCommand checkCmd = new SqlCommand(checkSql, _connection))
                {
                    checkCmd.Parameters.AddWithValue("@ttid", entry.Ttid);
                    checkCmd.Parameters.AddWithValue("@cid", entry.Cid);

                    if (_connection.State != ConnectionState.Open)
                        _connection.Open();

                    int count = (int)checkCmd.ExecuteScalar();
                    if (count > 0)
                    {
                        hasError = true;
                        failedEntries.Add(entry);
                        continue;
                    }
                }

                // 插入数据库
                try
                {
                    string insertSql = "INSERT INTO teaching_course (ttid, cid, cname) VALUES (@ttid, @cid, @cname)";
                    using (SqlCommand cmd = new SqlCommand(insertSql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@ttid", entry.Ttid);
                        cmd.Parameters.AddWithValue("@cid", entry.Cid);
                        cmd.Parameters.AddWithValue("@cname", entry.Cname ?? "");

                        if (_connection.State != ConnectionState.Open)
                            _connection.Open();

                        cmd.ExecuteNonQuery();
                        entry.IsNew = false;
                        hasSuccess = true;
                    }
                }
                catch (SqlException ex)
                {
                    // 检查是否是由触发器引起的错误（例如 "该课程已安排老师教授"）
                    if (ex.Number == 50000)  // 触发器错误的错误号通常为 50000
                    {
                        // 触发器错误，忽略空白或重复记录的提示
                        hasError = true;
                        failedEntries.Add(entry);
                        // 显示触发器错误的提示
                        MessageBox.Show("保存失败（触发器错误）：\n" + ex.Message, "保存失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        // 其他数据库错误
                        hasError = true;
                        failedEntries.Add(entry);
                        MessageBox.Show("保存失败（数据库错误）：\n" + ex.Message, "保存失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }

            if (hasError && !hasSuccess)
            {
                MessageBox.Show("保存失败：存在空白或重复记录，请修改后重试。", "保存失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (hasError && hasSuccess)
            {
                MessageBox.Show("部分记录保存成功，其余存在空白或重复，已保留，请修改后再次保存。", "部分保存", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (hasSuccess)
            {
                MessageBox.Show("所有记录保存成功！", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // 刷新数据，但保留未成功的行
            if (hasSuccess)
            {
                LoadSelectionData(); // 只在有成功时刷新数据库数据
                foreach (var entry in failedEntries)
                {
                    if (!SelectionList.Contains(entry))
                        SelectionList.Add(entry); // 保留失败的行
                }
                CourseSelectionGrid.ItemsSource = null;
                CourseSelectionGrid.ItemsSource = SelectionList;
            }
        }



        private void DeleteSelection_Click(object sender, RoutedEventArgs e)
        {
            if (CourseSelectionGrid.SelectedItem is CourseSelectionEntry selected)
            {
                // 判断是否是空白或未保存的新行（没有 Ttid 或 Cid）
                bool isIncomplete = string.IsNullOrWhiteSpace(selected.Ttid) || string.IsNullOrWhiteSpace(selected.Cid);

                if (isIncomplete || selected.IsNew)
                {
                    // 直接移除，不操作数据库
                    SelectionList.Remove(selected);
                    CourseSelectionGrid.ItemsSource = null;
                    CourseSelectionGrid.ItemsSource = SelectionList;
                }
                else
                {
                    var result = MessageBox.Show($"确定删除学号 {selected.Ttid} 的选课记录？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            string deleteSql = "DELETE FROM teaching_course WHERE ttid = @ttid AND cid = @cid";
                            using (SqlCommand cmd = new SqlCommand(deleteSql, _connection))
                            {
                                cmd.Parameters.AddWithValue("@ttid", selected.Ttid);
                                cmd.Parameters.AddWithValue("@cid", selected.Cid);

                                if (_connection.State != ConnectionState.Open)
                                    _connection.Open();

                                cmd.ExecuteNonQuery();
                            }

                            SelectionList.Remove(selected);
                            CourseSelectionGrid.ItemsSource = null;
                            CourseSelectionGrid.ItemsSource = SelectionList;
                        }
                        catch (SqlException ex)
                        {
                            MessageBox.Show("删除失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }


        private void AddSelection_Click(object sender, RoutedEventArgs e)
        {
            if (CourseSelectionGrid.CommitEdit(DataGridEditingUnit.Row, true) == false)
            {
                // 提交失败也继续尝试退出编辑
                CourseSelectionGrid.CancelEdit();
            }

            var newEntry = new CourseSelectionEntry
            {
                Ttid = "",
                Name = "",
                Sex = "M",
                College = "",
                Department = "",
                Cid = "",
                Cname = "",
                Score = "",
                IsNew = true
            };

            SelectionList.Add(newEntry);

            CourseSelectionGrid.ItemsSource = null;
            CourseSelectionGrid.ItemsSource = SelectionList;
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            CollegeComboBox.SelectedIndex = 0;
            DepartmentComboBox.SelectedIndex = 0;
            SexComboBox.SelectedIndex = 0;

        }
    }
    }

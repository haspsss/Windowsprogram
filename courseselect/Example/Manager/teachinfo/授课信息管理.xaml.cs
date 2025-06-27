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
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged("IsSelected");
            }
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
        public ObservableCollection<TeacherInfo> TeacherList { get; set; } = new ObservableCollection<TeacherInfo>();
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
            string keyword = SearchBox.Text.Trim().ToLower(); // 转小写做模糊匹配
            string selectedCollege = CollegeComboBox.SelectedItem?.ToString() ?? "全部";
            string selectedDepartment = DepartmentComboBox.SelectedItem?.ToString() ?? "全部";
            string selectedSex = SexComboBox.SelectedItem?.ToString() ?? "全部";

            var filtered = SelectionList.Where(item =>
                // 模糊匹配关键字：教师号、教师名、课程号、课程名、学院、系、性别（展示值）
                (string.IsNullOrEmpty(keyword) || (
                    (item.Ttid?.ToLower().Contains(keyword) ?? false) ||
                    (item.Name?.ToLower().Contains(keyword) ?? false) ||
                    (item.Cid?.ToLower().Contains(keyword) ?? false) ||
                    (item.Cname?.ToLower().Contains(keyword) ?? false) ||
                    (item.College?.ToLower().Contains(keyword) ?? false) ||
                    (item.Department?.ToLower().Contains(keyword) ?? false) ||
                    (item.SexDisplay?.ToLower().Contains(keyword) ?? false)
                )) &&
                // 下拉筛选精确匹配
                (selectedCollege == "全部" || item.College == selectedCollege) &&
                (selectedDepartment == "全部" || item.Department == selectedDepartment) &&
                (selectedSex == "全部" || item.SexDisplay == selectedSex)
            ).ToList();

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
            var failedEntries = new List<CourseSelectionEntry>();
            var emptyFieldEntries = new List<CourseSelectionEntry>();
            var duplicateEntries = new List<CourseSelectionEntry>();

            foreach (var entry in newEntries)
            {
                // ✅ 1. 判断字段是否为空
                if (string.IsNullOrWhiteSpace(entry.Ttid) ||
                    string.IsNullOrWhiteSpace(entry.Cid) ||
                    string.IsNullOrWhiteSpace(entry.Name) ||
                    string.IsNullOrWhiteSpace(entry.Cname) ||
                    string.IsNullOrWhiteSpace(entry.College) ||
                    string.IsNullOrWhiteSpace(entry.Department) ||
                    string.IsNullOrWhiteSpace(entry.Sex))
                {
                    emptyFieldEntries.Add(entry);
                    continue;
                }

                // ✅ 2. 检查是否重复（提前判断）
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
                        duplicateEntries.Add(entry);
                        continue;
                    }
                }

                // ✅ 3. 执行插入
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
                        entry.IsNew = false;  // 插入成功后标记为旧记录
                        hasSuccess = true;
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show("保存失败（数据库错误）：\n" + ex.Message, "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    failedEntries.Add(entry);
                }
            }

            // ✅ 4. 弹出分类型错误提示
            if (emptyFieldEntries.Count > 0)
            {
                MessageBox.Show("以下记录存在空字段，请补全后再试。", "字段不完整", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if(duplicateEntries.Count > 0)
            {
                MessageBox.Show("以下记录与数据库已存在的授课安排重复，请修改后重试。", "重复记录", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            else if (!hasSuccess)
            {
                MessageBox.Show("没有记录保存成功。请修改错误后再次尝试。", "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show("全部记录保存成功！", "保存结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // ✅ 5. 保留失败的记录，并重新绑定 DataGrid
            if (hasSuccess)
            {
                LoadSelectionData();

                foreach (var entry in emptyFieldEntries.Concat(duplicateEntries).Concat(failedEntries))
                {
                    entry.IsNew = true; // 继续允许编辑
                    SelectionList.Add(entry);
                }

                CourseSelectionGrid.ItemsSource = null;
                CourseSelectionGrid.ItemsSource = SelectionList;
            }
        }




        private void DeleteSelection_Click(object sender, RoutedEventArgs e)
        {
            var toDelete = SelectionList.Where(x => x.IsSelected).ToList();

            if (toDelete.Count == 0)
            {
                MessageBox.Show("请先勾选要删除的授课记录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"确定删除选中的 {toDelete.Count} 条授课记录？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
                return;

            foreach (var entry in toDelete)
            {
                bool isIncomplete = string.IsNullOrWhiteSpace(entry.Ttid) || string.IsNullOrWhiteSpace(entry.Cid);

                if (entry.IsNew || isIncomplete)
                {
                    // 本地未保存记录，直接移除
                    SelectionList.Remove(entry);
                }
                else
                {
                    try
                    {
                        string deleteSql = "DELETE FROM teaching_course WHERE ttid = @ttid AND cid = @cid";
                        using (SqlCommand cmd = new SqlCommand(deleteSql, _connection))
                        {
                            cmd.Parameters.AddWithValue("@ttid", entry.Ttid);
                            cmd.Parameters.AddWithValue("@cid", entry.Cid);

                            if (_connection.State != ConnectionState.Open)
                                _connection.Open();

                            cmd.ExecuteNonQuery();
                        }

                        SelectionList.Remove(entry);
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            CourseSelectionGrid.ItemsSource = null;
            CourseSelectionGrid.ItemsSource = SelectionList;
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

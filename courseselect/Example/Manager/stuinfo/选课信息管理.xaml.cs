using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Protocols.WSTrust;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace courseselect.Example.Manager.stuinfo
{
    public class StudentInfo
    {
        public string Sid { get; set; }
        public string Name { get; set; }
        public string Sex { get; set; }
        public string College { get; set; }
        public string Major { get; set; }
        public  int  Credit{ get; set; }
        private int _remainingCredit;


        public int RemainingCredit
        {
            get => _remainingCredit;
            set
            {
                if (_remainingCredit != value)
                {
                    _remainingCredit = value;
                   // OnPropertyChanged(nameof(RemainingCredit));

                }

            }
        }
        public string Display => $"{Sid} - {Name}";  // 显示学号和姓名
    }

    public class CourseInfo
    {
        public string Cid { get; set; }
        public string Cname { get; set; }
        public int Ccredit { get; set; }

        public string Display => $"{Cid} - {Cname}";  // 显示课程号和课程名
    }

   

public class CourseSelectionEntry : INotifyPropertyChanged
    {
        private string _sid;
        private string _name;
        private string _sex;
        private string _college;
        private string _major;
        private int _credit;
        private string _cid;
        private string _cname;
        private int _ccredit;
        private bool _isNew;
        private bool _isSelected;
        private int _remainingCredit;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        public string Sid
        {
            get => _sid;
            set { _sid = value; OnPropertyChanged("Sid"); }
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
        public int RemainingCredit
        {
            get => _remainingCredit;
            set { _remainingCredit = value; OnPropertyChanged(nameof(RemainingCredit)); }
        }
        public string College
        {
            get => _college;
            set { _college = value; OnPropertyChanged("College"); }
        }

        public string Major
        {
            get => _major;
            set { _major = value; OnPropertyChanged("Major"); }
        }
        public int Credit
        {
            get => _credit;
            set { _credit = value; OnPropertyChanged("Credit"); }
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

        public int Ccredit
        {
            get => _ccredit;
            set { _ccredit = value; OnPropertyChanged("Ccredit"); }
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


    public partial class 选课信息管理 : Page
    {
        private SqlConnection _connection;
        public ObservableCollection<CourseSelectionEntry> SelectionList { get; set; } = new ObservableCollection<CourseSelectionEntry>();
        public ObservableCollection<StudentInfo> StudentList { get; set; } = new ObservableCollection<StudentInfo>();
        public ObservableCollection<CourseInfo> CourseList { get; set; } = new ObservableCollection<CourseInfo>();
        public ObservableCollection<string> CollegeFilterList { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> MajorFilterList { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> SexFilterList { get; set; } = new ObservableCollection<string> { "全部", "男", "女" };

        public 选课信息管理(SqlConnection connection)
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
            string keyword = SearchBox.Text.Trim().ToLower(); // 统一转小写用于模糊匹配
            string selectedCollege = CollegeComboBox.SelectedItem?.ToString() ?? "全部";
            string selectedMajor = MajorComboBox.SelectedItem?.ToString() ?? "全部";
            string selectedSex = SexComboBox.SelectedItem?.ToString() ?? "全部";

            var filtered = SelectionList.Where(item =>
                // 模糊搜索匹配（包含：学号、姓名、性别、学院、专业、课程号、课程名）
                (string.IsNullOrEmpty(keyword) ||
                 (item.Sid != null && item.Sid.ToLower().Contains(keyword)) ||
                 (item.Name != null && item.Name.ToLower().Contains(keyword)) ||
                 (item.SexDisplay != null && item.SexDisplay.ToLower().Contains(keyword)) ||
                 (item.College != null && item.College.ToLower().Contains(keyword)) ||
                 (item.Major != null && item.Major.ToLower().Contains(keyword)) ||
                 (item.Cid != null && item.Cid.ToLower().Contains(keyword)) ||
                 (item.Cname != null && item.Cname.ToLower().Contains(keyword))) &&

                // 精确筛选：学院、专业、性别（下拉框筛选）
                (selectedCollege == "全部" || item.College == selectedCollege) &&
                (selectedMajor == "全部" || item.Major == selectedMajor) &&
                (selectedSex == "全部" || item.SexDisplay == selectedSex)
            ).ToList();

            CourseSelectionGrid.ItemsSource = null;
            CourseSelectionGrid.ItemsSource = filtered;
        }


        // 加载学生和课程的下拉框选项
        private void LoadStudentAndCourseOptions()
{
    StudentList.Clear();
    CollegeFilterList.Clear();
    MajorFilterList.Clear();
    CollegeFilterList.Add("全部");
    MajorFilterList.Add("全部");

    string sql1 = "SELECT sid, name, sex, college, major,credit FROM student";
    using (SqlCommand cmd = new SqlCommand(sql1, _connection))
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string sid = reader["sid"].ToString();
                string name = reader["name"].ToString();
                string sex = reader["sex"].ToString();
                string college = reader["college"].ToString();
                string major = reader["major"].ToString();
                        int credit = reader["credit"] == DBNull.Value ? 0 : Convert.ToInt32(reader["credit"]);

                        StudentList.Add(new StudentInfo
                {
                    Sid = sid,
                    Name = name,
                    Sex = sex,
                    College = college,
                    Major = major,
                    Credit = credit
                });

                if (!CollegeFilterList.Contains(college))
                    CollegeFilterList.Add(college);
                if (!MajorFilterList.Contains(major))
                    MajorFilterList.Add(major);
            }
        }
    }

    // 加载课程
    CourseList.Clear();
    string sql2 = @"
        SELECT DISTINCT c.cid, c.cname,c.ccredit 
        FROM course c
        JOIN teaching_course tc ON tc.cid = c.cid";
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
                  Cname = reader["cname"].ToString(),
                  Ccredit = reader["ccredit"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ccredit"])
                });
            }
        }
    }

    // 设置下拉框 ItemsSource
    CollegeComboBox.ItemsSource = CollegeFilterList;
    MajorComboBox.ItemsSource = MajorFilterList;
    SexComboBox.ItemsSource = SexFilterList;

    CollegeComboBox.SelectedIndex = 0;
    MajorComboBox.SelectedIndex = 0;
    SexComboBox.SelectedIndex = 0;
}

        // 加载选课记录
        private void LoadSelectionData()
        {
            // 记录每个学生的已选学分（sid -> 已选学分）
            Dictionary<string, int> usedCredits = new Dictionary<string, int>();
            Dictionary<string, int> totalCredits = new Dictionary<string, int>();

            SelectionList.Clear();
            string sql = @"
               SELECT 
    s.sid, 
    s.name, 
    s.sex, 
    s.college, 
    s.major, 
    s.credit AS credit,
    sc.sc_cid AS cid, 
    sc.sc_cname AS cname, 
    c.ccredit
FROM scclass sc
JOIN student s ON sc.sc_sid = s.sid
JOIN course c ON sc.sc_cid = c.cid
";

            
           
            using (SqlCommand cmd = new SqlCommand(sql, _connection))
            {
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string sid = reader["sid"].ToString();
                        int Ccredit = reader["ccredit"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ccredit"]);
                        int Credit = reader["credit"] == DBNull.Value ? 0 : Convert.ToInt32(reader["credit"]);
                        if (!totalCredits.ContainsKey(sid))
                            totalCredits[sid] = Credit;

                        if (!usedCredits.ContainsKey(sid))
                            usedCredits[sid] = 0;

                        usedCredits[sid] +=Ccredit;

                        if (reader["cid"] != DBNull.Value)
                        {
                            SelectionList.Add(new CourseSelectionEntry
                            {
                                Sid = reader["sid"].ToString(),
                                Name = reader["name"].ToString(),
                                Sex = reader["sex"].ToString(),
                                College = reader["college"].ToString(),
                                Major = reader["major"].ToString(),
                                Cid = reader["cid"].ToString(),
                                Cname = reader["cname"].ToString(),
                                Ccredit = reader["ccredit"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ccredit"]),
                                Credit = reader["credit"] == DBNull.Value ? 0 : Convert.ToInt32(reader["credit"]),
                                RemainingCredit = 0,  // 稍后再统一更新
                                IsNew = false
                            });
                        }
                    }
                }
                foreach (var entry in SelectionList)
                {
                    string sid = entry.Sid;
                    int total = totalCredits.ContainsKey(sid) ? totalCredits[sid] : 0;
                    int used = usedCredits.ContainsKey(sid) ? usedCredits[sid] : 0;
                    entry.RemainingCredit = total - used;
                }

                CourseSelectionGrid.ItemsSource = null;
                CourseSelectionGrid.ItemsSource = SelectionList;
            }


        }

        // 选择学生后自动填充其信息
        private void StudentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is StudentInfo selectedStudent)
            {
                var entry = comboBox.DataContext as CourseSelectionEntry;
                if (entry != null)
                {
                    entry.Name = selectedStudent.Name;
                    entry.Sex = selectedStudent.Sex;
                    entry.College = selectedStudent.College;
                    entry.Major = selectedStudent.Major;
                    entry.Credit = selectedStudent.Credit;
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
                    entry.Ccredit = selectedCourse.Ccredit;
                    // 不需要 Refresh！
                }
            }
        }
        private int GetCourseCredit(string cid)
        {
            string sql = "SELECT ccredit FROM course WHERE cid = @cid";
            using (SqlCommand cmd = new SqlCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@cid", cid);
                if (_connection.State != ConnectionState.Open)
                    _connection.Open();
                object result = cmd.ExecuteScalar();
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }


        private void SaveSelection_Click(object sender, RoutedEventArgs e)
        {
            var newEntries = SelectionList.Where(x => x.IsNew).ToList();
            bool hasSuccess = false;
            bool hasError = false;
            var failedEntries = new List<CourseSelectionEntry>();

            var insufficientCreditMessages = new List<string>();
            var duplicateSelectionMessages = new List<string>();

            foreach (var entry in newEntries)
            {
                // 校验是否信息完整（int 类型如 Credit 不判断为空）
                if (string.IsNullOrWhiteSpace(entry.Sid) ||
                    string.IsNullOrWhiteSpace(entry.Cid) ||
                    string.IsNullOrWhiteSpace(entry.Name) ||
                    string.IsNullOrWhiteSpace(entry.Cname) ||
                    string.IsNullOrWhiteSpace(entry.College) ||
                    string.IsNullOrWhiteSpace(entry.Major) ||
                    string.IsNullOrWhiteSpace(entry.Sex))
                {
                    failedEntries.Add(entry);
                    hasError = true;
                    continue;
                }

                // 检查是否重复选课
                string checkSql = "SELECT COUNT(*) FROM scclass WHERE sc_sid = @sid AND sc_cid = @cid";
                using (SqlCommand checkCmd = new SqlCommand(checkSql, _connection))
                {
                    checkCmd.Parameters.AddWithValue("@sid", entry.Sid);
                    checkCmd.Parameters.AddWithValue("@cid", entry.Cid);

                    if (_connection.State != ConnectionState.Open)
                        _connection.Open();

                    int count = (int)checkCmd.ExecuteScalar();
                    if (count > 0)
                    {
                        duplicateSelectionMessages.Add($"学生 {entry.Sid} 已选择课程 {entry.Cid}");
                        failedEntries.Add(entry);
                        hasError = true;
                        continue;
                    }
                }

                // 获取课程学分
                int courseCredit = GetCourseCredit(entry.Cid);

                // 计算已选学分（排除当前 IsNew）
                int usedCredit = SelectionList
                    .Where(sel => sel.Sid == entry.Sid && !sel.IsNew)
                    .Sum(sel => sel.Ccredit);

                int remainingCredit = entry.Credit - usedCredit;

                if (remainingCredit < courseCredit)
                {
                    insufficientCreditMessages.Add($"学生 {entry.Sid} 剩余学分不足（剩余 {remainingCredit}，需 {courseCredit}）");
                    failedEntries.Add(entry);
                    hasError = true;
                    continue;
                }

                // 插入数据库
                try
                {
                    string insertSql = "INSERT INTO scclass (sc_sid, sc_cid, sc_cname) VALUES (@sid, @cid, @cname)";
                    using (SqlCommand cmd = new SqlCommand(insertSql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@sid", entry.Sid);
                        cmd.Parameters.AddWithValue("@cid", entry.Cid);
                        cmd.Parameters.AddWithValue("@cname", entry.Cname ?? "");

                        if (_connection.State != ConnectionState.Open)
                            _connection.Open();

                        cmd.ExecuteNonQuery();
                        entry.Ccredit = courseCredit;
                        entry.RemainingCredit = remainingCredit - courseCredit;
                        entry.IsNew = false;
                        hasSuccess = true;
                    }
                }
                catch (SqlException ex)
                {
                    failedEntries.Add(entry);
                    hasError = true;
                    MessageBox.Show("保存失败（数据库触发器错误）：\n" + ex.Message, "保存失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // 汇总所有提示
            if (duplicateSelectionMessages.Count > 0)
            {
                MessageBox.Show(string.Join("\n", duplicateSelectionMessages), "重复选课", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            else if (insufficientCreditMessages.Count > 0)
            {
                MessageBox.Show(string.Join("\n", insufficientCreditMessages), "学分不足", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            else if (hasError && !hasSuccess)
            {
                MessageBox.Show("保存失败：存在错误记录，请修改后重试。", "保存失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
           
            else if (hasSuccess)
            {
                MessageBox.Show("所有记录保存成功！", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // 刷新列表
            if (hasSuccess)
            {
                LoadSelectionData(); // 重新加载数据
                foreach (var failed in failedEntries)
                {
                    if (!SelectionList.Contains(failed))
                        SelectionList.Add(failed);
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
                MessageBox.Show("请先勾选要删除的选课记录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"确定删除 {toDelete.Count} 条选课记录？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
                return;

            // 收集所有受影响学生的 ID
            HashSet<string> affectedSids = new HashSet<string>();

            foreach (var entry in toDelete)
            {
                bool isIncomplete = string.IsNullOrWhiteSpace(entry.Sid) || string.IsNullOrWhiteSpace(entry.Cid);

                if (entry.IsNew || isIncomplete)
                {
                    SelectionList.Remove(entry);
                    affectedSids.Add(entry.Sid);
                }
                else
                {
                    try
                    {
                        string deleteSql = "DELETE FROM scclass WHERE sc_sid = @sid AND sc_cid = @cid";
                        using (SqlCommand cmd = new SqlCommand(deleteSql, _connection))
                        {
                            cmd.Parameters.AddWithValue("@sid", entry.Sid);
                            cmd.Parameters.AddWithValue("@cid", entry.Cid);

                            if (_connection.State != ConnectionState.Open)
                                _connection.Open();

                            cmd.ExecuteNonQuery();
                        }

                        SelectionList.Remove(entry);
                        affectedSids.Add(entry.Sid);
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show($"删除学号 {entry.Sid} 的记录失败：\n" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            // ✅ 删除后更新每个受影响学生的剩余学分 RemainingCredit
            foreach (var sid in affectedSids)
            {
                var records = SelectionList.Where(x => x.Sid == sid).ToList();
                if (records.Count > 0)
                {
                    int totalCredit = records[0].Credit;
                    int usedCredit = records.Sum(x => x.Ccredit);
                    int remaining = totalCredit - usedCredit;

                    foreach (var rec in records)
                    {
                        rec.RemainingCredit = remaining;
                    }
                }
            }

            // ✅ 刷新绑定
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
                Sid = "",
                Name = "",
                Sex = "M",
                College = "",
                Major = "",
                Cid = "",
                Cname = "",
            
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
            MajorComboBox.SelectedIndex = 0;
            SexComboBox.SelectedIndex = 0;
            CourseSelectionGrid.ItemsSource = SelectionList;
        }



    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
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
        public string Display => $"{Sid} - {Name}";  // 显示学号和姓名
    }

    public class CourseInfo
    {
        public string Cid { get; set; }
        public string Cname { get; set; }
        public string Display => $"{Cid} - {Cname}";  // 显示课程号和课程名
    }

   

public class CourseSelectionEntry : INotifyPropertyChanged
    {
        private string _sid;
        private string _name;
        private string _sex;
        private string _college;
        private string _major;
        private string _cid;
        private string _cname;
        private string _score;
        private bool _isNew;

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
            string keyword = SearchBox.Text.Trim();
            string selectedCollege = CollegeComboBox.SelectedItem?.ToString() ?? "全部";
            string selectedMajor = MajorComboBox.SelectedItem?.ToString() ?? "全部";
            string selectedSex = SexComboBox.SelectedItem?.ToString() ?? "全部";

            var filtered = SelectionList.Where(item =>
                (string.IsNullOrEmpty(keyword) ||
                 item.Sid.Contains(keyword) ||
                 item.Name.Contains(keyword)) &&
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

    string sql1 = "SELECT sid, name, sex, college, major FROM student";
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

                StudentList.Add(new StudentInfo
                {
                    Sid = sid,
                    Name = name,
                    Sex = sex,
                    College = college,
                    Major = major
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
        SELECT DISTINCT c.cid, c.cname 
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
                    Cname = reader["cname"].ToString()
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
            SelectionList.Clear();
            string sql = @"
                SELECT s.sid, s.name, s.sex, s.college, s.major,
                       sc.sc_cid AS cid, sc.sc_cname AS cname, sc.score
                FROM scclass sc
                JOIN student s ON sc.sc_sid = s.sid";

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
                            Sid = reader["sid"].ToString(),
                            Name = reader["name"].ToString(),
                            Sex = reader["sex"].ToString(),
                            College = reader["college"].ToString(),
                            Major = reader["major"].ToString(),
                            Cid = reader["cid"].ToString(),
                            Cname = reader["cname"].ToString(),
                            Score = reader["score"]?.ToString()
                        });
                    }
                }
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
                if (string.IsNullOrWhiteSpace(entry.Sid) ||
                    string.IsNullOrWhiteSpace(entry.Cid) ||
                    string.IsNullOrWhiteSpace(entry.Name) ||
                    string.IsNullOrWhiteSpace(entry.Cname) ||
                    string.IsNullOrWhiteSpace(entry.College) ||
                    string.IsNullOrWhiteSpace(entry.Major) ||
                    string.IsNullOrWhiteSpace(entry.Sex))
                {
                    hasError = true;
                    failedEntries.Add(entry);
                    continue;
                }

                // 检查是否重复
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
                        hasError = true;
                        failedEntries.Add(entry);
                        continue;
                    }
                }

                // 插入数据库
                try
                {
                    string insertSql = "INSERT INTO scclass (sc_sid, sc_cid, sc_cname, score) VALUES (@sid, @cid, @cname, @score)";
                    using (SqlCommand cmd = new SqlCommand(insertSql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@sid", entry.Sid);
                        cmd.Parameters.AddWithValue("@cid", entry.Cid);
                        cmd.Parameters.AddWithValue("@cname", entry.Cname ?? "");
                        
                        if (string.IsNullOrWhiteSpace(entry.Score))
                            cmd.Parameters.AddWithValue("@score", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@score", entry.Score);
                        if (_connection.State != ConnectionState.Open)
                            _connection.Open();

                        cmd.ExecuteNonQuery();
                        entry.IsNew = false;
                        hasSuccess = true;
                    }
                }
                catch (SqlException ex)
                {
                    hasError = true;
                    failedEntries.Add(entry);
                    MessageBox.Show("保存失败（数据库触发器错误）：\n" + ex.Message, "保存失败", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                // 判断是否是空白或未保存的新行（没有 Sid 或 Cid）
                bool isIncomplete = string.IsNullOrWhiteSpace(selected.Sid) || string.IsNullOrWhiteSpace(selected.Cid);

                if (isIncomplete || selected.IsNew)
                {
                    // 直接移除，不操作数据库
                    SelectionList.Remove(selected);
                    CourseSelectionGrid.ItemsSource = null;
                    CourseSelectionGrid.ItemsSource = SelectionList;
                }
                else
                {
                    var result = MessageBox.Show($"确定删除学号 {selected.Sid} 的选课记录？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            string deleteSql = "DELETE FROM scclass WHERE sc_sid = @sid AND sc_cid = @cid";
                            using (SqlCommand cmd = new SqlCommand(deleteSql, _connection))
                            {
                                cmd.Parameters.AddWithValue("@sid", selected.Sid);
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
                Sid = "",
                Name = "",
                Sex = "M",
                College = "",
                Major = "",
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
            MajorComboBox.SelectedIndex = 0;
            SexComboBox.SelectedIndex = 0;
           
        }



    }
}

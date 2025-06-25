using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
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

namespace courseselect.Example.Manager.stuinfo
{
    /// <summary>
    /// 个人信息管理.xaml 的交互逻辑
    /// </summary>
    public partial class 学生信息管理 : Page
    {
        public ObservableCollection<StudentEntry> StudentList { get; set; } = new ObservableCollection<StudentEntry>();
        private SqlConnection _connection;
        public List<KeyValuePair<string, string>> SexOptions { get; set; } = new List<KeyValuePair<string, string>>
{
    new KeyValuePair<string, string>("M", "男"),
    new KeyValuePair<string, string>("F", "女")
};


        public 学生信息管理(SqlConnection connection)
        {
            InitializeComponent();
            _connection = connection;
            this.DataContext = this;
            LoadStudents();
            LoadFilterOptions();
          //  PopulateFilterDropdowns();
        }
        private void LoadFilterOptions()
        {
            HashSet<string> colleges = new HashSet<string>();
            HashSet<string> majors = new HashSet<string>();
            HashSet<string> sexes = new HashSet<string>();
            HashSet<string> politicals = new HashSet<string>();

            using (SqlCommand cmd = new SqlCommand("SELECT college, major, sex, political FROM student", _connection))
            {
                if (_connection.State != ConnectionState.Open) _connection.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        colleges.Add(reader["college"].ToString());
                        majors.Add(reader["major"].ToString());
                        sexes.Add(reader["sex"].ToString());
                        politicals.Add(reader["political"].ToString());
                    }
                }
            }

            CollegeComboBox.ItemsSource = colleges.OrderBy(c => c).ToList();
            MajorComboBox.ItemsSource = majors.OrderBy(m => m).ToList();
            SexComboBox.ItemsSource = sexes.OrderBy(s => s).ToList();
            PoliticalComboBox.ItemsSource = politicals.OrderBy(p => p).ToList();

        }

        private void LoadStudents()
        {
            StudentList.Clear();
            using (SqlCommand cmd = new SqlCommand("SELECT * FROM student", _connection))
            {
                if (_connection.State != ConnectionState.Open) _connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        StudentList.Add(new StudentEntry
                        {
                            Sid = reader["sid"].ToString(),
                            Name = reader["name"].ToString(),
                            Sex = reader["sex"].ToString(),
                            Birth = reader["birth"].ToString(),
                            Major = reader["major"].ToString(),
                            College = reader["college"].ToString(),
                            Phone = reader["phone"].ToString(),
                            Political = reader["political"].ToString(),
                            Password = reader["password"].ToString(),
                            IfChangePw = reader["ifchangepw"].ToString(),
                            Question = reader["question"]?.ToString(),
                            Answer = reader["answer"]?.ToString(),
                            IsNew = false
                        });
                    }
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            StudentList.Add(new StudentEntry { IsNew = true });
        }

       
      private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 删除完全空白行
            var emptyRows = StudentList.Where(s =>
                string.IsNullOrWhiteSpace(s.Sid) &&
                string.IsNullOrWhiteSpace(s.Name) &&
                string.IsNullOrWhiteSpace(s.Sex) &&
                string.IsNullOrWhiteSpace(s.Birth) &&
                string.IsNullOrWhiteSpace(s.Major) &&
                string.IsNullOrWhiteSpace(s.College) &&
                string.IsNullOrWhiteSpace(s.Phone) &&
                string.IsNullOrWhiteSpace(s.Political) &&
                string.IsNullOrWhiteSpace(s.Password) &&
                string.IsNullOrWhiteSpace(s.IfChangePw)).ToList();

            foreach (var s in emptyRows)
            {
                StudentList.Remove(s);
            }

            // 检查信息不完整的行
            var incompleteRows = StudentList.Where(student =>
                !string.IsNullOrWhiteSpace(student.Sid) || !string.IsNullOrWhiteSpace(student.Name) ||
                !string.IsNullOrWhiteSpace(student.Sex) || !string.IsNullOrWhiteSpace(student.Birth) ||
                !string.IsNullOrWhiteSpace(student.Major) || !string.IsNullOrWhiteSpace(student.College) ||
                !string.IsNullOrWhiteSpace(student.Phone) || !string.IsNullOrWhiteSpace(student.Political) ||
                !string.IsNullOrWhiteSpace(student.Password) || !string.IsNullOrWhiteSpace(student.IfChangePw)
            ).Where(student =>
                string.IsNullOrWhiteSpace(student.Sid) || string.IsNullOrWhiteSpace(student.Name) ||
                string.IsNullOrWhiteSpace(student.Sex) || string.IsNullOrWhiteSpace(student.Birth) ||
                string.IsNullOrWhiteSpace(student.Major) || string.IsNullOrWhiteSpace(student.College) ||
                string.IsNullOrWhiteSpace(student.Phone) || string.IsNullOrWhiteSpace(student.Political) ||
                string.IsNullOrWhiteSpace(student.Password) || string.IsNullOrWhiteSpace(student.IfChangePw)
            ).ToList();

            if (incompleteRows.Count > 0)
            {
                var result = MessageBox.Show(
                    "存在信息不完整的行。现在保存将会丢失这些行的所有信息。\n是否确定保存？",
                    "确认保存",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    return;
                }

                foreach (var row in incompleteRows)
                {
                    StudentList.Remove(row);
                }
            }

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            bool hasError = false;
            var toSave = StudentList.ToList();

            foreach (var student in toSave)
            {
                if (student.IsNew)
                {
                    // 判断 sid 是否已存在
                    using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM student WHERE sid = @sid", _connection))
                    {
                        checkCmd.Parameters.AddWithValue("@sid", student.Sid);
                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show($"学号 {student.Sid} 已存在，无法重复添加，请修改。", "学号重复", MessageBoxButton.OK, MessageBoxImage.Warning);
                            hasError = true;
                            continue; // 跳过这个
                        }
                    }

                    // 插入新数据
                    using (SqlCommand cmd = new SqlCommand(@"
                INSERT INTO student (sid, name, sex, birth, major, college, phone, political, password, ifchangepw, question, answer)
                VALUES (@sid, @name, @sex, @birth, @major, @college, @phone, @political, @password, @ifchangepw, @question, @answer)", _connection))
                    {
                        cmd.Parameters.AddWithValue("@sid", student.Sid);
                        cmd.Parameters.AddWithValue("@name", student.Name);
                        cmd.Parameters.AddWithValue("@sex", student.Sex);
                        cmd.Parameters.AddWithValue("@birth", student.Birth);
                        cmd.Parameters.AddWithValue("@major", student.Major);
                        cmd.Parameters.AddWithValue("@college", student.College);
                        cmd.Parameters.AddWithValue("@phone", student.Phone);
                        cmd.Parameters.AddWithValue("@political", student.Political);
                        cmd.Parameters.AddWithValue("@password", student.Password);
                        cmd.Parameters.AddWithValue("@ifchangepw", student.IfChangePw);
                        cmd.Parameters.AddWithValue("@question", (object)student.Question ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@answer", (object)student.Answer ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                        student.IsNew = false;
                    }
                }
                else
                {
                    // 更新已有数据
                    using (SqlCommand cmd = new SqlCommand(@"
                UPDATE student SET name=@name, sex=@sex, birth=@birth, major=@major, college=@college,
                phone=@phone, political=@political, password=@password, ifchangepw=@ifchangepw, question=@question, answer=@answer
                WHERE sid=@sid", _connection))
                    {
                        cmd.Parameters.AddWithValue("@sid", student.Sid);
                        cmd.Parameters.AddWithValue("@name", student.Name);
                        cmd.Parameters.AddWithValue("@sex", student.Sex);
                        cmd.Parameters.AddWithValue("@birth", student.Birth);
                        cmd.Parameters.AddWithValue("@major", student.Major);
                        cmd.Parameters.AddWithValue("@college", student.College);
                        cmd.Parameters.AddWithValue("@phone", student.Phone);
                        cmd.Parameters.AddWithValue("@political", student.Political);
                        cmd.Parameters.AddWithValue("@password", student.Password);
                        cmd.Parameters.AddWithValue("@ifchangepw", student.IfChangePw);
                        cmd.Parameters.AddWithValue("@question", (object)student.Question ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@answer", (object)student.Answer ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            if (!hasError)
            {
                MessageBox.Show("保存成功！");
                LoadStudents(); // 刷新数据
            }
            else
            {
                MessageBox.Show("部分数据未保存，请检查学号是否重复。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = StudentList.Where(s => s.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("请勾选要删除的学生！");
                return;
            }

            foreach (var student in selected)
            {
                // 判断是否是空白或信息不完整行（未填写学号或为新添加尚未保存的行）
                bool isBlank = string.IsNullOrWhiteSpace(student.Sid)
                            || student.IsNew
                            || string.IsNullOrWhiteSpace(student.Name)
                            || string.IsNullOrWhiteSpace(student.Sex)
                            || string.IsNullOrWhiteSpace(student.Birth)
                            || string.IsNullOrWhiteSpace(student.Major)
                            || string.IsNullOrWhiteSpace(student.College)
                            || string.IsNullOrWhiteSpace(student.Phone)
                            || string.IsNullOrWhiteSpace(student.Political)
                            || string.IsNullOrWhiteSpace(student.Password)
                            || string.IsNullOrWhiteSpace(student.IfChangePw);

                if (!isBlank)
                {
                    // 尝试从数据库删除
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM student WHERE sid = @sid", _connection))
                    {
                        cmd.Parameters.AddWithValue("@sid", student.Sid);
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"删除学号 {student.Sid} 失败: {ex.Message}", "删除失败", MessageBoxButton.OK, MessageBoxImage.Error);
                            continue; // 不移除该行
                        }
                    }
                }

                // 无论是否在数据库删除，都从列表中移除
                StudentList.Remove(student);
            }

            MessageBox.Show("删除操作完成！");
            LoadStudents(); // 可选：重新加载数据
        }

        private void FilterComboBox_Changed(object sender, SelectionChangedEventArgs e)
        {
            FilterCourses();
        }
    /*    private void PopulateFilterDropdowns()
        {
            SexComboBox.Items.Add("女");
            SexComboBox.Items.Add("男");
            PoliticalComboBox.Items.Add("党员");
            PoliticalComboBox.Items.Add("预备党员");
            PoliticalComboBox.Items.Add("团员");
            PoliticalComboBox.Items.Add("群众");
            CollegeComboBox.Items.Add("信息科学技术学院");
            CollegeComboBox.Items.Add("艺术学院");
            MajorComboBox.Items.Add("计科");
           
        }*/
        private void FilterCourses()
        {
            string keyword = SearchBox.Text.Trim().ToLower();
            string selectedTime = MajorComboBox.SelectedItem?.ToString();
            string selectedCollege = CollegeComboBox.SelectedItem?.ToString();
            string selectedKind = SexComboBox.SelectedItem?.ToString();
            string selectedTest = PoliticalComboBox.SelectedItem?.ToString();
         
        }
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string keyword = SearchBox.Text.Trim().ToLower();
            string selectedCollege = CollegeComboBox.SelectedItem?.ToString();
            string selectedMajor = MajorComboBox.SelectedItem?.ToString();
            string selectedSex = SexComboBox.SelectedItem?.ToString();
            string selectedPolitical = PoliticalComboBox.SelectedItem?.ToString();

            var filtered = StudentList.Where(s =>
                // 关键字匹配（模糊搜索）
                (string.IsNullOrEmpty(keyword) || (
                    s.Sid.ToLower().Contains(keyword) ||
                    s.Name.ToLower().Contains(keyword) ||
                    s.Major.ToLower().Contains(keyword) ||
                    s.College.ToLower().Contains(keyword) ||
                    s.Political.ToLower().Contains(keyword) ||
                    s.SexDisplay.ToLower().Contains(keyword)
                )) &&
                // 下拉框筛选项匹配（精确匹配）
                (string.IsNullOrEmpty(selectedCollege) || s.College == selectedCollege) &&
                (string.IsNullOrEmpty(selectedMajor) || s.Major == selectedMajor) &&
                (string.IsNullOrEmpty(selectedSex) || s.SexDisplay == selectedSex) &&
                (string.IsNullOrEmpty(selectedPolitical) || s.Political == selectedPolitical)
            ).ToList();

            StudentDataGrid.ItemsSource = filtered;
        }


        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            StudentDataGrid.ItemsSource = StudentList;
        }

    }
    public class StringToDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (DateTime.TryParse(value?.ToString(), out DateTime result))
            {
                return result;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                return dt.ToString("yyyy-MM-dd");
            }
            return null;
        }
    }
    public class StudentEntry : INotifyPropertyChanged
    {
        public string Sid { get; set; }
        public string Name { get; set; }
        public string Sex { get; set; }
        public string Birth { get; set; }
        public string Major { get; set; }
        public string College { get; set; }
        public string Phone { get; set; }
        public string Political { get; set; }
        public string Password { get; set; }
        public string IfChangePw { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }

        public bool IsNew { get; set; } = false;
        public string SexDisplay
        {
            get
            {
                if (Sex == "F") return "女";
                if (Sex == "M") return "男";
                return Sex; // fallback
            }
        }
        public string BirthDisplay
        {
            get
            {
                if (DateTime.TryParse(Birth, out DateTime date))
                {
                    return date.ToString("yyyy-MM-dd");
                }
                return Birth; // fallback
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

}

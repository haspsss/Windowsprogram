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

namespace courseselect.Example.Manager.teachinfo
{
    /// <summary>
    /// 个人信息管理.xaml 的交互逻辑
    /// </summary>
    public partial class 教师信息管理 : Page
    {
        public ObservableCollection<TeachEntry> TeachList { get; set; } = new ObservableCollection<TeachEntry>();
        private SqlConnection _connection;
        public List<KeyValuePair<string, string>> SexOptions { get; set; } = new List<KeyValuePair<string, string>>
{
    new KeyValuePair<string, string>("M", "男"),
    new KeyValuePair<string, string>("F", "女")
};


        public 教师信息管理(SqlConnection connection)
        {
            InitializeComponent();
            _connection = connection;
            this.DataContext = this;
            LoadTeachers();
            LoadFilterOptions();
            //  PopulateFilterDropdowns();
        }
        private void LoadFilterOptions()
        {
            HashSet<string> colleges = new HashSet<string>();
            HashSet<string> department = new HashSet<string>();
            HashSet<string> sexes = new HashSet<string>();
            HashSet<string> politicals = new HashSet<string>();

            using (SqlCommand cmd = new SqlCommand("SELECT college, department, sex, political FROM teacher", _connection))
            {
                if (_connection.State != ConnectionState.Open) _connection.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        colleges.Add(reader["college"].ToString());
                        department.Add(reader["department"].ToString());
                        sexes.Add(reader["sex"].ToString());
                        politicals.Add(reader["political"].ToString());
                    }
                }
            }

            CollegeComboBox.ItemsSource = colleges.OrderBy(c => c).ToList();
            DepartmentComboBox.ItemsSource = department.OrderBy(m => m).ToList();
            SexComboBox.ItemsSource = sexes.OrderBy(s => s).ToList();
            PoliticalComboBox.ItemsSource = politicals.OrderBy(p => p).ToList();

        }

        private void LoadTeachers()
        {
                TeachList.Clear();
            using (SqlCommand cmd = new SqlCommand("SELECT * FROM teacher", _connection))
            {
                if (_connection.State != ConnectionState.Open) _connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TeachList.Add(new TeachEntry
                        {
                            Ttid = reader["ttid"].ToString(),
                            Name = reader["name"].ToString(),
                            Sex = reader["sex"].ToString(),
                            Birth = reader["birth"].ToString(),
                            Department = reader["department"].ToString(),
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
           TeachList.Add(new TeachEntry { IsNew = true });
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 删除完全空白行
            var emptyRows =TeachList.Where(s =>
                string.IsNullOrWhiteSpace(s.Ttid) &&
                string.IsNullOrWhiteSpace(s.Name) &&
                string.IsNullOrWhiteSpace(s.Sex) &&
                string.IsNullOrWhiteSpace(s.Birth) &&
                string.IsNullOrWhiteSpace(s.Department) &&
                string.IsNullOrWhiteSpace(s.College) &&
                string.IsNullOrWhiteSpace(s.Phone) &&
                string.IsNullOrWhiteSpace(s.Political) &&
                string.IsNullOrWhiteSpace(s.Password) &&
                string.IsNullOrWhiteSpace(s.IfChangePw)).ToList();

            foreach (var s in emptyRows)
            {
               TeachList.Remove(s);
            }

            // 检查信息不完整的行
            var incompleteRows =TeachList.Where(teacher =>
                !string.IsNullOrWhiteSpace(teacher.Ttid) || !string.IsNullOrWhiteSpace(teacher.Name) ||
                !string.IsNullOrWhiteSpace(teacher.Sex) || !string.IsNullOrWhiteSpace(teacher.Birth) ||
                !string.IsNullOrWhiteSpace(teacher.Department) || !string.IsNullOrWhiteSpace(teacher.College) ||
                !string.IsNullOrWhiteSpace(teacher.Phone) || !string.IsNullOrWhiteSpace(teacher.Political) ||
                !string.IsNullOrWhiteSpace(teacher.Password) || !string.IsNullOrWhiteSpace(teacher.IfChangePw)
            ).Where(teacher =>
                string.IsNullOrWhiteSpace(teacher.Ttid) || string.IsNullOrWhiteSpace(teacher.Name) ||
                string.IsNullOrWhiteSpace(teacher.Sex) || string.IsNullOrWhiteSpace(teacher.Birth) ||
                string.IsNullOrWhiteSpace(teacher.Department) || string.IsNullOrWhiteSpace(teacher.College) ||
                string.IsNullOrWhiteSpace(teacher.Phone) || string.IsNullOrWhiteSpace(teacher.Political) ||
                string.IsNullOrWhiteSpace(teacher.Password) || string.IsNullOrWhiteSpace(teacher.IfChangePw)
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
                   TeachList.Remove(row);
                }
            }

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            bool hasError = false;
            var toSave =TeachList.ToList();

            foreach (var teacher in toSave)
            {
                if (teacher.IsNew)
                {
                    // 判断 ttid 是否已存在
                    using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM teacher WHERE ttid = @ttid", _connection))
                    {
                        checkCmd.Parameters.AddWithValue("@ttid", teacher.Ttid);
                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show($"学号 {teacher.Ttid} 已存在，无法重复添加，请修改。", "学号重复", MessageBoxButton.OK, MessageBoxImage.Warning);
                            hasError = true;
                            continue; // 跳过这个
                        }
                    }

                    // 插入新数据
                    using (SqlCommand cmd = new SqlCommand(@"
                INSERT INTO teacher (ttid, name, sex, birth, department, college, phone, political, password, ifchangepw, question, answer)
                VALUES (@ttid, @name, @sex, @birth, @department, @college, @phone, @political, @password, @ifchangepw, @question, @answer)", _connection))
                    {
                        cmd.Parameters.AddWithValue("@ttid", teacher.Ttid);
                        cmd.Parameters.AddWithValue("@name", teacher.Name);
                        cmd.Parameters.AddWithValue("@sex", teacher.Sex);
                        cmd.Parameters.AddWithValue("@birth", teacher.Birth);
                        cmd.Parameters.AddWithValue("@department", teacher.Department);
                        cmd.Parameters.AddWithValue("@college", teacher.College);
                        cmd.Parameters.AddWithValue("@phone", teacher.Phone);
                        cmd.Parameters.AddWithValue("@political", teacher.Political);
                        cmd.Parameters.AddWithValue("@password", teacher.Password);
                        cmd.Parameters.AddWithValue("@ifchangepw", teacher.IfChangePw);
                        cmd.Parameters.AddWithValue("@question", (object)teacher.Question ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@answer", (object)teacher.Answer ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                        teacher.IsNew = false;
                    }
                }
                else
                {
                    // 更新已有数据
                    using (SqlCommand cmd = new SqlCommand(@"
                UPDATE teacher SET name=@name, sex=@sex, birth=@birth, department=@department, college=@college,
                phone=@phone, political=@political, password=@password, ifchangepw=@ifchangepw, question=@question, answer=@answer
                WHERE ttid=@ttid", _connection))
                    {
                        cmd.Parameters.AddWithValue("@ttid", teacher.Ttid);
                        cmd.Parameters.AddWithValue("@name", teacher.Name);
                        cmd.Parameters.AddWithValue("@sex", teacher.Sex);
                        cmd.Parameters.AddWithValue("@birth", teacher.Birth);
                        cmd.Parameters.AddWithValue("@department", teacher.Department);
                        cmd.Parameters.AddWithValue("@college", teacher.College);
                        cmd.Parameters.AddWithValue("@phone", teacher.Phone);
                        cmd.Parameters.AddWithValue("@political", teacher.Political);
                        cmd.Parameters.AddWithValue("@password", teacher.Password);
                        cmd.Parameters.AddWithValue("@ifchangepw", teacher.IfChangePw);
                        cmd.Parameters.AddWithValue("@question", (object)teacher.Question ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@answer", (object)teacher.Answer ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            if (!hasError)
            {
                MessageBox.Show("保存成功！");
               LoadTeachers(); // 刷新数据
            }
            else
            {
                MessageBox.Show("部分数据未保存，请检查学号是否重复。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selected =TeachList.Where(s => s.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("请勾选要删除的学生！");
                return;
            }

            foreach (var teacher in selected)
            {
                // 判断是否是空白或信息不完整行（未填写学号或为新添加尚未保存的行）
                bool isBlank = string.IsNullOrWhiteSpace(teacher.Ttid)
                            || teacher.IsNew
                            || string.IsNullOrWhiteSpace(teacher.Name)
                            || string.IsNullOrWhiteSpace(teacher.Sex)
                            || string.IsNullOrWhiteSpace(teacher.Birth)
                            || string.IsNullOrWhiteSpace(teacher.Department)
                            || string.IsNullOrWhiteSpace(teacher.College)
                            || string.IsNullOrWhiteSpace(teacher.Phone)
                            || string.IsNullOrWhiteSpace(teacher.Political)
                            || string.IsNullOrWhiteSpace(teacher.Password)
                            || string.IsNullOrWhiteSpace(teacher.IfChangePw);

                if (!isBlank)
                {
                    // 尝试从数据库删除
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM teacher WHERE ttid = @ttid", _connection))
                    {
                        cmd.Parameters.AddWithValue("@ttid", teacher.Ttid);
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"删除学号 {teacher.Ttid} 失败: {ex.Message}", "删除失败", MessageBoxButton.OK, MessageBoxImage.Error);
                            continue; // 不移除该行
                        }
                    }
                }

                // 无论是否在数据库删除，都从列表中移除
               TeachList.Remove(teacher);
            }

            MessageBox.Show("删除操作完成！");
           LoadTeachers(); // 可选：重新加载数据
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
                departmentComboBox.Items.Add("计科");

            }*/
        private void FilterCourses()
        {
            string keyword = SearchBox.Text.Trim().ToLower();
            string selectedTime = DepartmentComboBox.SelectedItem?.ToString();
            string selectedCollege = CollegeComboBox.SelectedItem?.ToString();
            string selectedKind = SexComboBox.SelectedItem?.ToString();
            string selectedTest = PoliticalComboBox.SelectedItem?.ToString();

        }
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string keyword = SearchBox.Text.Trim().ToLower();
            string selectedCollege = CollegeComboBox.SelectedItem?.ToString();
            string selecteddepartment = DepartmentComboBox.SelectedItem?.ToString();
            string selectedSex = SexComboBox.SelectedItem?.ToString();
            string selectedPolitical = PoliticalComboBox.SelectedItem?.ToString();

            var filtered =TeachList.Where(s =>
                // 关键字匹配（模糊搜索）
                (string.IsNullOrEmpty(keyword) || (
                    s.Ttid.ToLower().Contains(keyword) ||
                    s.Name.ToLower().Contains(keyword) ||
                    s.Department.ToLower().Contains(keyword) ||
                    s.College.ToLower().Contains(keyword) ||
                    s.Political.ToLower().Contains(keyword) ||
                    s.SexDisplay.ToLower().Contains(keyword)
                )) &&
                // 下拉框筛选项匹配（精确匹配）
                (string.IsNullOrEmpty(selectedCollege) || s.College == selectedCollege) &&
                (string.IsNullOrEmpty(selecteddepartment) || s.Department == selecteddepartment) &&
                (string.IsNullOrEmpty(selectedSex) || s.SexDisplay == selectedSex) &&
                (string.IsNullOrEmpty(selectedPolitical) || s.Political == selectedPolitical)
            ).ToList();

            teacherDataGrid.ItemsSource = filtered;
        }


        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            teacherDataGrid.ItemsSource =TeachList;
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
    public class TeachEntry : INotifyPropertyChanged
    {
        public string Ttid { get; set; }
        public string Name { get; set; }
        public string Sex { get; set; }
        public string Birth { get; set; }
        public string Department { get; set; }
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

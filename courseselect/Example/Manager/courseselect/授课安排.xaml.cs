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
    public partial class 授课安排 : Page
    {
        public ObservableCollection<TeachingEntry> TeachingList { get; set; } = new ObservableCollection<TeachingEntry>();
        public List<CourseOption> CourseOptions { get; set; } = new List<CourseOption>();
        public List<TeacherOption> TeacherOptions { get; set; } = new List<TeacherOption>();

        private SqlConnection _connection;

        public 授课安排(SqlConnection connection)
        {
            InitializeComponent();
            _connection = connection;
            this.DataContext = this;

            LoadCourseOptions();
            LoadTeacherOptions();
            LoadTeachingAssignments();
        }

        private void LoadCourseOptions()
        {
            CourseOptions.Clear();
            using (var cmd = new SqlCommand("SELECT cid, cname FROM course", _connection))
            {
                if (_connection.State != ConnectionState.Open) _connection.Open();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    CourseOptions.Add(new CourseOption
                    {
                        Cid = reader["cid"].ToString(),
                        Cname = reader["cname"].ToString(),
                        Display = $"{reader["cid"]} - {reader["cname"]}"
                    });
                }
                reader.Close();
            }
        }

        private void LoadTeacherOptions()
        {
            TeacherOptions.Clear();
            using (var cmd = new SqlCommand("SELECT ttid, name FROM teacher", _connection))
            {
                if (_connection.State != ConnectionState.Open) _connection.Open();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    TeacherOptions.Add(new TeacherOption
                    {
                        Ttid = reader["ttid"].ToString(),
                        Tname = reader["name"].ToString(),
                        Display = $"{reader["ttid"]} - {reader["name"]}"
                    });
                }
                reader.Close();
            }
        }

        private void LoadTeachingAssignments()
        {
            TeachingList.Clear();
            using (var cmd = new SqlCommand(@"
                SELECT tc.cid, c.cname, tc.ttid, t.name AS tname 
                FROM teaching_course tc
                JOIN course c ON tc.cid = c.cid
                JOIN teacher t ON tc.ttid = t.ttid", _connection))
            {
                if (_connection.State != ConnectionState.Open) _connection.Open();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    TeachingList.Add(new TeachingEntry
                    {
                        Cid = reader["cid"].ToString(),
                        Cname = reader["cname"].ToString(),
                        Ttid = reader["ttid"].ToString(),
                        Tname = reader["tname"].ToString()
                    });
                }
                reader.Close();
            }
        }

        private void CourseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo?.SelectedItem is CourseOption option && TeachingDataGrid.SelectedItem is TeachingEntry entry)
            {
                entry.Cid = option.Cid;
                entry.Cname = option.Cname;
            }
        }

        private void TeacherComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo?.SelectedItem is TeacherOption option && TeachingDataGrid.SelectedItem is TeachingEntry entry)
            {
                entry.Ttid = option.Ttid;
                entry.Tname = option.Tname;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            bool hasError = false;

            for (int i = 0; i < TeachingList.Count; i++)
            {
                var record = TeachingList[i];

                if (string.IsNullOrWhiteSpace(record.Cid) ||
                    string.IsNullOrWhiteSpace(record.Ttid) ||
                    string.IsNullOrWhiteSpace(record.Cname) ||
                    string.IsNullOrWhiteSpace(record.Tname))
                {
                    continue;
                }

                if (record.IsNew)
                {
                    // 检查是否重复
                    using (SqlCommand checkCmd = new SqlCommand(
                        "SELECT COUNT(*) FROM teaching_course WHERE cid = @cid AND ttid = @ttid", _connection))
                    {
                        checkCmd.Parameters.AddWithValue("@cid", record.Cid);
                        checkCmd.Parameters.AddWithValue("@ttid", record.Ttid);

                        int count = (int)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show($"保存失败：课程 {record.Cid} 与教师 {record.Ttid} 的授课安排已存在。",
                                "保存失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                            hasError = true;
                            continue;
                        }
                    }

                    // 执行插入
                    using (SqlCommand insertCmd = new SqlCommand(@"
                INSERT INTO teaching_course (cid, ttid, cname, tname)
                VALUES (@cid, @ttid, @cname, @tname)", _connection))
                    {
                        insertCmd.Parameters.AddWithValue("@cid", record.Cid);
                        insertCmd.Parameters.AddWithValue("@ttid", record.Ttid);
                        insertCmd.Parameters.AddWithValue("@cname", record.Cname);
                        insertCmd.Parameters.AddWithValue("@tname", record.Tname);

                        try
                        {
                            insertCmd.ExecuteNonQuery();
                            record.IsNew = false;
                        }
                        catch (SqlException ex)
                        {
                            if (ex.Message.Contains("教师时间安排冲突"))
                            {
                                MessageBox.Show($"保存失败：教师时间冲突（课程 {record.Cid} 教师 {record.Ttid}）", "失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            else
                            {
                                MessageBox.Show("数据库错误：" + ex.Message, "保存失败", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            hasError = true;
                            continue;
                        }
                    }
                }
            }

            if (!hasError)
            {
                MessageBox.Show("授课信息保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            LoadTeachingAssignments(); // 无论成功或失败都刷新数据
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var newEntry = new TeachingEntry
            {
                IsNew = true,
                Cid = "",
                Cname = "",
                Ttid = "",
                Tname = ""
            };

            TeachingList.Add(newEntry);
            TeachingDataGrid.SelectedItem = newEntry;
        }

        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            TeachingDataGrid.ItemsSource = TeachingList;
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var currentList = TeachingDataGrid.ItemsSource as IEnumerable<TeachingEntry>;
            if (currentList == null) return;

            var toDelete = currentList.Where(t => t.IsSelected).ToList();

            if (toDelete.Count == 0)
            {
                MessageBox.Show("请勾选要删除的授课记录。");
                return;
            }

            if (MessageBox.Show($"确认删除勾选的 {toDelete.Count} 条记录吗？", "确认删除",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            foreach (var item in toDelete)
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM teaching_course WHERE cid = @cid AND ttid = @ttid", _connection))
                    {
                        cmd.Parameters.AddWithValue("@cid", item.Cid);
                        cmd.Parameters.AddWithValue("@ttid", item.Ttid);
                        cmd.ExecuteNonQuery();
                    }

                    TeachingList.Remove(item); // 仍然从 TeachingList 移除（因为这是主集合）
                }
                catch (SqlException ex)
                {
                    MessageBox.Show($"删除课程 {item.Cid} 教师 {item.Ttid} 时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            LoadTeachingAssignments(); // 建议删除后也刷新一次
        }


        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string keyword = SearchTextBox.Text.Trim().ToLower();
            var filtered = TeachingList.Where(t =>
                (t.Cid?.ToLower().Contains(keyword) ?? false) ||
                (t.Cname?.ToLower().Contains(keyword) ?? false) ||
                (t.Ttid?.ToLower().Contains(keyword) ?? false) ||
                (t.Tname?.ToLower().Contains(keyword) ?? false)).ToList();

            TeachingDataGrid.ItemsSource = filtered;
        }
    }

    public class TeachingEntry : INotifyPropertyChanged
    {
        public bool IsNew { get; set; } = false;

        public string Cid { get; set; }
        public string Cname { get; set; }
        public string Ttid { get; set; }
        public string Tname { get; set; }

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

        public bool IsDeletable => !string.IsNullOrWhiteSpace(Cid) && !string.IsNullOrWhiteSpace(Ttid);

        public string CourseDisplay => $"{Cid} - {Cname}";
        public string TeacherDisplay => $"{Ttid} - {Tname}";

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class CourseOption
    {
        public string Cid { get; set; }
        public string Cname { get; set; }
        public string Display { get; set; }
    }

    public class TeacherOption
    {
        public string Ttid { get; set; }
        public string Tname { get; set; }
        public string Display { get; set; }
    }
}

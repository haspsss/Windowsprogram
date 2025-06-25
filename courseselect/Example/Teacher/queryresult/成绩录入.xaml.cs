using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace courseselect.Example.Teacher.queryresult
{
    public partial class 成绩录入 : Page, INotifyPropertyChanged
    {

        private readonly SqlConnection _connection;
        private readonly string _userId;

        public ObservableCollection<CourseScoreEntry> CourseList { get; set; } = new ObservableCollection<CourseScoreEntry>();

        public 成绩录入(SqlConnection conn, string userId)
        {
            InitializeComponent();
            DataContext = this;
            _connection = conn;
            _userId = userId;
            LoadCourses();
        }

        private void LoadCourses()
        {
            string courseSql = @"SELECT DISTINCT c.cid, c.cname, c.cyear, c.ccredit, c.ckind, c.ccollege, 
                            ISNULL(c.usual_ratio, 0.4) AS usual_ratio, 
                            ISNULL(c.exam_ratio, 0.6) AS exam_ratio 
                     FROM teaching_course tc 
                     JOIN course c ON tc.cid = c.cid 
                     WHERE tc.ttid = @ttid";
            

            if (_connection.State != System.Data.ConnectionState.Open)
                _connection.Open();

            using (SqlCommand cmd = new SqlCommand(courseSql, _connection))
            {
                cmd.Parameters.AddWithValue("@ttid", _userId);
                SqlDataReader reader = cmd.ExecuteReader();

                CourseList.Clear();
                while (reader.Read())
                {
                   
                    CourseScoreEntry course = new CourseScoreEntry
                    {
                        Cid = reader["cid"].ToString(),
                        CName = reader["cname"].ToString(),
                        CYear = reader["cyear"].ToString(),
                        CCredit = Convert.ToInt32(reader["ccredit"]),
                        CKind = reader["ckind"].ToString(),
                        CCollege = reader["ccollege"].ToString(),
                        UsualRatio = Convert.ToDouble(reader["usual_ratio"]),





                    };
                   
                    CourseList.Add(course);
                }
                reader.Close();
            }

            foreach (var course in CourseList)
            {
                LoadStudentScores(course);
            }
        }

        private void LoadStudentScores(CourseScoreEntry course)
        {
            string sql = "SELECT s.sid, s.name, sc.usual_score, sc.exam_score,score FROM scclass sc JOIN student s ON sc.sc_sid = s.sid WHERE sc.sc_cid = @cid";

            using (SqlCommand cmd = new SqlCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@cid", course.Cid);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    course.Students.Add(new StudentScore
                    {
                        StudentId = reader["sid"].ToString(),
                        StudentName = reader["name"].ToString(),
                        UsualScore = reader["usual_score"] == DBNull.Value ? 0 : Convert.ToDouble(reader["usual_score"]),
                        ExamScore = reader["exam_score"] == DBNull.Value ? 0 : Convert.ToDouble(reader["exam_score"])
                        
                    });
                }
                reader.Close();
            }

            course.CalculateFinalScores();
        }

        private void ToggleChart_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var course = button?.Tag as CourseScoreEntry;
            if (course != null)
            {
                course.CalculateFinalScores(); // 自动更新图表
            }
        }
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void SaveScores_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var course = button?.Tag as CourseScoreEntry;

            if (course == null) return;
            // 🔁 强制刷新当前 UI 中的绑定
            var parent = VisualTreeHelper.GetParent(button);
            while (parent != null && !(parent is Expander))
                parent = VisualTreeHelper.GetParent(parent);

            if (parent is Expander expander)
            {
                var textBoxes = FindVisualChildren<TextBox>(expander);
                foreach (var tb in textBoxes)
                {
                    var binding = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
                    if (binding != null) binding.UpdateSource(); // 强制推送到数据源
                }
            }
            if (_connection.State != System.Data.ConnectionState.Open)
                _connection.Open();

            // ⚠️ 更新 course 表中的比例字段
            using (SqlCommand updateRatioCmd = new SqlCommand(@"
        UPDATE course 
        SET usual_ratio = @usualRatio, 
            exam_ratio = @examRatio 
        WHERE cid = @cid", _connection))
            {
                updateRatioCmd.Parameters.AddWithValue("@usualRatio", course.UsualRatio);
                updateRatioCmd.Parameters.AddWithValue("@examRatio", course.ExamRatio);
                updateRatioCmd.Parameters.AddWithValue("@cid", course.Cid);
                int rows = updateRatioCmd.ExecuteNonQuery(); // ← 加上这句！

                Console.WriteLine($"更新 course 表：{rows} 行");

                if (rows == 0)
                    MessageBox.Show("没有找到要更新的课程记录！");
                updateRatioCmd.ExecuteNonQuery();
            }

            // ⚠️ 更新每个学生的成绩（如有必要，scclass 中的成绩）
            foreach (var s in course.Students)
            {
                using (SqlCommand cmd = new SqlCommand(@"
          UPDATE scclass 
SET usual_score = @usual, 
    exam_score = @exam, 
    score = @score 
WHERE sc_sid = @sid AND sc_cid = @cid
", _connection))
                {
                    cmd.Parameters.AddWithValue("@usual", s.UsualScore);
                    cmd.Parameters.AddWithValue("@exam", s.ExamScore);
                    cmd.Parameters.AddWithValue("@sid", s.StudentId);
                    cmd.Parameters.AddWithValue("@cid", course.Cid);
                    cmd.Parameters.AddWithValue("@score", s.FinalScore);
                    cmd.ExecuteNonQuery();
                }
            }

            MessageBox.Show($"课程 {course.CName} 的成绩和比例已保存！");
        }
       

        private void ScoreDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // 获取当前编辑的课程
            if (DataContext is 成绩录入 page)
            {
                // 遍历 CourseList 找到对应的 DataGrid
                foreach (var course in page.CourseList)
                {
                    if (course.Students.Contains(e.Row.Item as StudentScore))
                    {
                        course.CalculateFinalScores();  // 更新对应课程成绩
                        break;
                    }
                }
            }
        }
        private void RatioTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var binding = BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty);
                if (binding != null)
                {
                    binding.UpdateSource();
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        
    }

    public class CourseScoreEntry : INotifyPropertyChanged
    {
        public string Cid { get; set; }
        public string CName { get; set; }
        public string CYear { get; set; }
        public int CCredit { get; set; }
        public string CKind { get; set; }
        public string CCollege { get; set; }
        public double AverageUsual => Students.Count > 0 ? Math.Round(Students.Average(s => s.UsualScore), 2) : 0;
        public double AverageExam => Students.Count > 0 ? Math.Round(Students.Average(s => s.ExamScore), 2) : 0;

        public ObservableCollection<StudentScore> Students { get; set; } = new ObservableCollection<StudentScore>();

        private double _usualRatio;
        public double UsualRatio
        {
            get => _usualRatio;
            set
            {
                if (value >= 0 && value <= 1)
                {
                    _usualRatio = Math.Round(value, 2);
                    _examRatio = Math.Round(1.0 - _usualRatio, 2); // 自动保持和为1
                    OnPropertyChanged(nameof(UsualRatio));
                    OnPropertyChanged(nameof(ExamRatio));
                    CalculateFinalScores();
                }
            }
        }
        
        private double _examRatio;
        public double ExamRatio
        {
            get => _examRatio;
            set // 外部不可直接设定，只由 UsualRatio 控制
            {
                _examRatio = value;
                OnPropertyChanged(nameof(ExamRatio));
            }
        }

        public double Average => Students.Count > 0 ? Math.Round(Students.Average(s => s.FinalScore), 2) : 0;
        public double Max => Students.Count > 0 ? Students.Max(s => s.FinalScore) : 0;
        public double Min => Students.Count > 0 ? Students.Min(s => s.FinalScore) : 0;
        public double MaxUsual => Students.Count > 0 ? Students.Max(s => s.UsualScore) : 0;
        public double MinUsual => Students.Count > 0 ? Students.Min(s => s.UsualScore) : 0;
        public double MaxExam => Students.Count > 0 ? Students.Max(s => s.ExamScore) : 0;
        public double MinExam => Students.Count > 0 ? Students.Min(s => s.ExamScore) : 0;

        public SeriesCollection ScoreSeries { get; set; }
        public string[] ScoreLabels { get; set; }

        public void CalculateFinalScores()
        {
            foreach (var s in Students)
            {
                s.FinalScore = Math.Round(s.UsualScore * UsualRatio + s.ExamScore * ExamRatio, 1);
            }

            UpdateChart();
            OnPropertyChanged(nameof(Average));
            OnPropertyChanged(nameof(Max));
            OnPropertyChanged(nameof(Min));
            OnPropertyChanged(nameof(AverageUsual));
            OnPropertyChanged(nameof(AverageExam));
            OnPropertyChanged(nameof(MaxUsual));
            OnPropertyChanged(nameof(MinUsual));
            OnPropertyChanged(nameof(MaxExam));
            OnPropertyChanged(nameof(MinExam));
        }



        public void UpdateChart()
        {
            int[] sections = new int[6];
            foreach (var s in Students)
            {
                int score = (int)Math.Floor(s.FinalScore);
                if (score < 60) sections[0]++;
                else if (score < 70) sections[1]++;
                else if (score < 80) sections[2]++;
                else if (score < 90) sections[3]++;
                else if (score < 100) sections[4]++;
                else sections[5]++;
            }

            ScoreLabels = new[] { "<60", "60-69", "70-79", "80-89", "90-99", "100" };
            ScoreSeries = new SeriesCollection
            {
                new ColumnSeries { Title = "人数", Values = new ChartValues<int>(sections) }
            };
            OnPropertyChanged(nameof(ScoreSeries));
            OnPropertyChanged(nameof(ScoreLabels));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class StudentScore : INotifyPropertyChanged
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }

        private double _usualScore;
        public double UsualScore
        {
            get => _usualScore;
            set { _usualScore = value; OnPropertyChanged(nameof(UsualScore)); }
        }

        private double _examScore;
        public double ExamScore
        {
            get => _examScore;
            set { _examScore = value; OnPropertyChanged(nameof(ExamScore)); }
        }

        private double _finalScore;
        public double FinalScore
        {
            get => _finalScore;
            set { _finalScore = value; OnPropertyChanged(nameof(FinalScore)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果值为 null 或者为空集合就返回 Collapsed
            if (value == null) return Visibility.Collapsed;

            // 如果是集合但没有内容
            if (value is System.Collections.IEnumerable enumerable)
                return enumerable.GetEnumerator().MoveNext() ? Visibility.Visible : Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

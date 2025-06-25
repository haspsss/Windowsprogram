using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IdentityModel;
using System.Windows;
using System.Windows.Controls;

namespace courseselect.Example.Student.queryresult
{
    public class GradeItem
    {
        public string AcademicYear { get; set; } // 学年，如 "2023-2024"
        public string CID { get; set; }
        public string CName { get; set; }
        public int CCredit { get; set; }
        public double? Score { get; set; }
        public bool IsSummary { get; set; } = false; // 区分是否为“合计行”
        public double GPA { get; set; } // 绩点（合计行用）
    }

    public partial class 成绩单 : Page, INotifyPropertyChanged
    {
        public ObservableCollection<GradeItem> GradeList { get; set; }

        public 成绩单(SqlConnection conn, string studentId)
        {
            InitializeComponent();
            GradeList = new ObservableCollection<GradeItem>();
            LoadGrades(conn, studentId);
            DataContext = this; // 设置数据上下文，绑定 GradeList 到 UI
        }

        private void LoadGrades(SqlConnection conn, string studentId)
        {
            GradeList.Clear();

            string sql = @"
        SELECT c.cid, c.cname, c.ccredit, c.cyear, sc.score
        FROM scclass sc
        JOIN course c ON sc.sc_cid = c.cid
        WHERE sc.sc_sid = @StudentId
        ORDER BY c.cyear";

            Dictionary<string, List<GradeItem>> groupedByYear = new Dictionary<string, List<GradeItem>>();

            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@StudentId", studentId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        double? score = reader["score"] != DBNull.Value ? (double?)reader["score"] : null;


                        var item = new GradeItem
                        {
                            AcademicYear = reader["cyear"].ToString(),
                            CID = reader["cid"].ToString(),
                            CName = reader["cname"].ToString(),
                            CCredit = (int)reader["ccredit"],
                            Score = score,
                            GPA = score.HasValue ? CalcGPA(score.Value) : 0.0
                        };

                        if (!groupedByYear.ContainsKey(item.AcademicYear))
                            groupedByYear[item.AcademicYear] = new List<GradeItem>();

                        groupedByYear[item.AcademicYear].Add(item);
                    }
                }
            }

            int overallCredit = 0;
            double overallWeightedGPA = 0;

            foreach (var year in groupedByYear.Keys)
            {
                var yearItems = groupedByYear[year];
                foreach (var item in yearItems)
                    GradeList.Add(item);

                int totalCredit = 0;
                double totalWeightedGPA = 0;

                foreach (var item in yearItems)
                {
                    totalCredit += item.CCredit;
                    if (item.Score.HasValue)
                    {
                        double courseGPA = CalcGPA(item.Score.Value);
                        totalWeightedGPA += courseGPA * item.CCredit;
                    }
                }

                overallCredit += totalCredit;
                overallWeightedGPA += totalWeightedGPA;

                var summary = new GradeItem
                {
                    AcademicYear = year + " 总计",
                    CID = "",
                    CName = "合计",
                    CCredit = totalCredit,
                    Score = null,
                    GPA = totalCredit > 0 ? totalWeightedGPA / totalCredit : 0,
                    IsSummary = true
                };

                GradeList.Add(summary);
            }

            // 添加“全部学年总计”行
            var finalSummary = new GradeItem
            {
                AcademicYear = "所有学年总计",
                CID = "",
                CName = "总计",
                CCredit = overallCredit,
                Score = null,
                GPA = overallCredit > 0 ? overallWeightedGPA / overallCredit : 0,
                IsSummary = true
            };

            GradeList.Add(finalSummary);

            OnPropertyChanged(nameof(GradeList));
        }


        private double CalcGPA(double score)
        {
            return score < 60 ? 0.0 : (score - 50) / 10.0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

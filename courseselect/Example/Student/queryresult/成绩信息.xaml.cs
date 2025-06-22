using System;

using System.Collections.ObjectModel;
using System.ComponentModel;

using System.Windows.Controls;

using System.Data.SqlClient;


namespace courseselect.Example.Student.queryresult
{
    /// <summary>
    /// 成绩信息.xaml 的交互逻辑
    /// </summary>
    public class GradeInfo
    {
        public string CID { get; set; }
        public string CName { get; set; }
        public string CCollege { get; set; }
        public string CKind { get; set; }
        public int CCredit { get; set; }
        public int? Score { get; set; }
        public string TName { get; set; }
        public string TTID { get; set; }
    }
    public partial class 成绩信息 : Page, INotifyPropertyChanged
    {
        public ObservableCollection<GradeInfo> GradeList { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public 成绩信息(SqlConnection connection, string studentId)
        {
            InitializeComponent();
            GradeList = new ObservableCollection<GradeInfo>();
            DataContext = this;
            LoadGrades(connection, studentId);
        }

        private void LoadGrades(SqlConnection conn, string studentId)
        {
            string sql = @"
                SELECT 
                    c.cid,
                    c.cname,
                    c.ccollege,
                    c.ckind,
                    c.ccredit,
                    sc.score,
                    t.ttid,
                    t.name AS tname
                FROM scclass sc
                JOIN course c ON sc.sc_cid = c.cid
                JOIN teaching_course tc ON c.cid = tc.cid
                JOIN teacher t ON tc.ttid = t.ttid
                WHERE sc.sc_sid = @StudentId";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@StudentId", studentId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var grade = new GradeInfo
                        {
                            CID = reader["cid"].ToString(),
                            CName = reader["cname"].ToString(),
                            CCollege = reader["ccollege"].ToString(),
                            CKind = reader["ckind"].ToString(),
                            CCredit = (int)reader["ccredit"],
                            Score = reader["score"] != DBNull.Value ? (int?)reader["score"] : null,

                            TName = reader["tname"].ToString(),
                            TTID = reader["ttid"].ToString()
                        };

                        GradeList.Add(grade);
                    }
                }
            }
        }
    }
}

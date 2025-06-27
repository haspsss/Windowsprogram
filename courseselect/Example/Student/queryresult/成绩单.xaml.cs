using PdfSharp.Drawing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IdentityModel;
using System.IO;
using System.IO;
using Microsoft.Win32;


using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;



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

        private void ExportToImageAndPdf()
        {
            try
            {
                var grid = MainGrid;
                if (grid == null)
                {
                    MessageBox.Show("无法获取课表控件");
                    return;
                }

                // 强制布局，确保获取完整尺寸
                grid.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                grid.Arrange(new Rect(grid.DesiredSize));
                grid.UpdateLayout();

                int width = (int)grid.DesiredSize.Width;
                int height = (int)grid.DesiredSize.Height;

                // 使用 DrawingVisual 绘制完整内容
                var renderBitmap = new RenderTargetBitmap(
                    width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);

                var dv = new System.Windows.Media.DrawingVisual();
                using (var ctx = dv.RenderOpen())
                {
                    var vb = new System.Windows.Media.VisualBrush(grid);
                    ctx.DrawRectangle(vb, null, new Rect(new System.Windows.Point(), new System.Windows.Size(width, height)));
                }
                renderBitmap.Render(dv);

                // 编码为 PNG
                var pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                var saveDialog = new SaveFileDialog
                {
                    Filter = "PDF 文件|*.pdf",
                    FileName = "成绩单导出.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using (var imageStream = new MemoryStream())
                    {
                        pngEncoder.Save(imageStream);
                        imageStream.Position = 0;
                        SaveImageToPdf(imageStream, saveDialog.FileName);
                        MessageBox.Show("导出成功！");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出失败: " + ex.Message);
            }
        }



        private void SaveImageToPdf(Stream imageStream, string pdfPath)
        {
            using (var doc = new PdfSharp.Pdf.PdfDocument())
            {
                var page = doc.AddPage();

                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    var image = XImage.FromStream(imageStream);

                    double ratio = Math.Min(page.Width / image.PixelWidth, page.Height / image.PixelHeight);
                    double width = image.PixelWidth * ratio;
                    double height = image.PixelHeight * ratio;

                    gfx.DrawImage(image, 0, 0, width, height);
                }

                doc.Save(pdfPath);
            }
        }



        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportToImageAndPdf();
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

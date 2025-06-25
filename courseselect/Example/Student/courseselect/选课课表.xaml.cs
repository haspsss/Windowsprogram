using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using PdfSharp.Pdf;
using PdfSharp.Drawing;


namespace courseselect.Example.Student.courseselect
{
    public class Course
    {
        public string CName { get; set; }        // 课程名称
        public string TeacherName { get; set; }  // 教师名称
        public string CPlace {  get; set; }
        public string CCredit {  get; set; }
        public string CKind {  get; set; }
    }

    public partial class 选课课表 : Page, INotifyPropertyChanged
    {
        private ObservableCollection<Course> _flatSchedule;
        public ObservableCollection<Course> FlatSchedule
        {
            get => _flatSchedule;
            set
            {
                _flatSchedule = value;
                OnPropertyChanged(nameof(FlatSchedule));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public 选课课表(SqlConnection connection, string userId)
        {
            InitializeComponent();
            LoadSchedule(connection, userId);
            DataContext = this;
        }

        private void LoadSchedule(SqlConnection connection, string userId)
        {
            // 初始化课表，共 12 行 * 7 列 = 84 个单元格
            var scheduleMatrix = new Course[12, 7];

            try
            {
                string query = @"
                    SELECT c.cname, c.cday, c.cplace, c.ckind, c.ccredit,
                           c.cnum, c.clast, tc.tname
                    FROM scclass sc
                    JOIN course c ON sc.sc_cid = c.cid
                    JOIN teaching_course tc ON c.cid = tc.cid
                    WHERE sc.sc_sid = @UserId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var course = new Course
                            {
                                CName = reader["cname"].ToString(),
                                TeacherName = reader["tname"].ToString(),
                                CPlace = reader["cplace"].ToString(),
                                CCredit = reader["ccredit"].ToString()+"学分",
                                CKind = reader["ckind"].ToString(),
                            };

                            int cnum = (int)reader["cnum"];
                            int clast = (int)reader["clast"];
                            string cday = reader["cday"].ToString();

                            int col = GetDayIndex(cday) - 1;  // 转为 0 基索引
                            for (int i = 0; i < clast; i++)
                            {
                                int row = cnum - 1 + i;
                                if (row >= 0 && row < 12 && col >= 0 && col < 7)
                                {
                                    scheduleMatrix[row, col] = course;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载课表失败: " + ex.Message);
            }

            // 扁平化为 ItemsControl 可绑定的一维集合
            var flatList = new ObservableCollection<Course>();
            for (int i = 0; i < 12; i++) // 行
            {
                for (int j = 0; j < 7; j++) // 列
                {
                    flatList.Add(scheduleMatrix[i, j] ?? new Course { CName = "", TeacherName = "" });
                }
            }

            FlatSchedule = flatList;
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
                    FileName = "课表导出.pdf"
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

        private int GetDayIndex(string day)
        {
            switch (day)
            {
                case "周一": return 1;
                case "周二": return 2;
                case "周三": return 3;
                case "周四": return 4;
                case "周五": return 5;
                case "周六": return 6;
                case "周日": return 7;
                default: return 0;
            }
        }
    }
}

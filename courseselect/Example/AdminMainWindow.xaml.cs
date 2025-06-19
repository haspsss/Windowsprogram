using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using System.Windows.Shapes;

namespace courseselect.Example
{
    /// <summary>
    /// AdminMainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AdminMainWindow : Window
    {
        public AdminMainWindow(SqlConnection connection, string userId)
        {
            InitializeComponent();
            this.Title = $"教师主界面 - {userId}";

        }

    }
}

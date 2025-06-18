using Npgsql;
using System.Windows;


namespace courseselect.Example
{
    public partial class BaseMainWindow : Window
    {
        protected NpgsqlConnection Connection { get; }
        protected string UserId { get; }
        // 无参数构造函数
        public BaseMainWindow()
        {
            InitializeComponent();
        }
        public BaseMainWindow(NpgsqlConnection connection, string userId)
        {
            Connection = connection;
            UserId = userId;
            InitializeComponent();
        }
    }
}
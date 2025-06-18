using courseselect.Example;
using Npgsql;
using System;
using System.Windows;
using System.Windows.Controls;

namespace courseselect.Example
{
    public partial class AdminMainWindow : BaseMainWindow
    {
        public AdminMainWindow(NpgsqlConnection connection, string userId)
            : base(connection, userId)
        {
            InitializeComponent();
            this.Title = $"管理主界面 - {UserId}";
         
        }

       
      
       
    }
}
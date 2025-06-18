using courseselect.Example;
using Npgsql;
using System;
using System.Windows;
using System.Windows.Controls;

namespace courseselect.Example
{
    public partial class TeacherMainWindow : BaseMainWindow
    {
        public TeacherMainWindow(NpgsqlConnection connection, string userId)
            : base(connection, userId)
        {
            InitializeComponent();
            this.Title = $"教师主界面 - {UserId}";
           
        }

        
    }

      
       
    
}
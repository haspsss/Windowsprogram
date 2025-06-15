using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace courseselect.Example.PersonInformation
{
    /// <summary>
    /// 我的信息.xaml 的交互逻辑
    /// </summary>
    public partial class 我的信息 : Page
    {
        //private NpgsqlConnection _connection;
        /// <summary>
        /// private string _userid;
        /// </summary>
       
        public 我的信息()
        {
            InitializeComponent();
           // _connection = connection;
            //_userid = userid;
            
        }

        

        private void ChangeInfo_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            ChangeInfoWindow changeInfoWindow = new ChangeInfoWindow();
            changeInfoWindow.Show();
        }
    }
}
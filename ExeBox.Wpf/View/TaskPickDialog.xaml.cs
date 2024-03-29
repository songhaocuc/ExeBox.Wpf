﻿using System;
using System.Collections.Generic;
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

namespace ExeBox.Wpf.View
{
    public class TaskSelection
    {
        public Model.LogTaskConfig Config { get; set; }
        public bool IsSelected { get; set; }
    }
    /// <summary>
    /// Interaction logic for TaskPickDialog.xaml
    /// </summary>
    public partial class TaskPickDialog : Window
    {
        /// <summary>
        /// 任务选择界面
        /// </summary>
        /// <param name="changeFile">若修改文件，则显示清理进程选项</param>
        /// <param name="selections"></param>
        public TaskPickDialog(bool changeFile, ref List<TaskSelection> selections)
        {
            InitializeComponent();
            selectionList.ItemsSource = selections;
            clearCheck.Visibility = changeFile ? Visibility.Visible : Visibility.Collapsed;
            clearCheck.IsChecked = changeFile;
        }
    }
}

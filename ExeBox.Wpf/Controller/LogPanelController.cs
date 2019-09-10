using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Xceed.Wpf.AvalonDock.Layout;

namespace ExeBox.Wpf.Controller
{
    /// <summary>
    /// 控制LogPanel的内部逻辑
    /// 主要为日志筛选
    /// </summary>
    public class LogPanelController
    {
        View.LogPanel m_Panel;
        CollectionView m_LogCollectionView;
        bool m_IsAutoScroll;

        public LogPanelController(View.LogPanel panel)
        {
            m_Panel = panel;
            // 当ItemsSource改变时 进行初始化 （一般情况下是为父控件的DataContext赋值时发生，向下传播到该控件的DataContext）
            var dpd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(DataGrid));
            if (dpd != null)
            {
                dpd.AddValueChanged(m_Panel.logListBox, (sender, e) =>
                {
                    if (m_Panel.logListBox.ItemsSource == null) return;
                    Init();
                });
            }
        }
        private void Init()
        {
            InitControlEvents();

            InitCommands();
        }

        private void InitControlEvents()
        {
            // 控制自动滚动
            m_IsAutoScroll = true;
            m_Panel.autoScrollSwitch.IsChecked = true;
            m_Panel.autoScrollSwitch.Click += (sender, e) =>
            {
                m_IsAutoScroll = m_Panel.autoScrollSwitch.IsChecked ?? true;
                if (m_IsAutoScroll)
                {
                    if (m_Panel.logListBox.Items != null && m_Panel.logListBox.Items.Count > 0)
                        m_Panel.logListBox.ScrollIntoView(m_Panel.logListBox.Items[m_Panel.logListBox.Items.Count - 1]);
                }
            };
            ((INotifyCollectionChanged)m_Panel.logListBox.Items).CollectionChanged += (sender, e) =>
            {
                if (!m_IsAutoScroll) return;
                if (m_Panel.logListBox.Items != null && m_Panel.logListBox.Items.Count > 0)
                    m_Panel.logListBox.ScrollIntoView(m_Panel.logListBox.Items[m_Panel.logListBox.Items.Count - 1]);
            };
            m_Panel.IsVisibleChanged += (sender, e) =>
            {
                if (!m_IsAutoScroll || m_Panel.Visibility != Visibility.Visible) return;
                if (m_Panel.logListBox.Items != null && m_Panel.logListBox.Items.Count > 0)
                    m_Panel.logListBox.ScrollIntoView(m_Panel.logListBox.Items[m_Panel.logListBox.Items.Count - 1]);
            };
            m_Panel.logListBox.PreviewMouseWheel += (sender, e) =>
            {
                //向上滚动滑轮 停止自动滚动
                if (e.Delta > 0)
                {
                    m_IsAutoScroll = false;
                    m_Panel.autoScrollSwitch.IsChecked = false;
                }
            };
            // 右键菜单出现时停止滚动
            m_Panel.logListBox.PreviewMouseRightButtonUp += (sender, e) =>
            {
                m_IsAutoScroll = false;
                m_Panel.autoScrollSwitch.IsChecked = false;
            };

            // 控制日志筛选
            m_Panel.errorSwitch.IsChecked = true;
            m_Panel.messageSwitch.IsChecked = true;

            m_LogCollectionView = CollectionViewSource.GetDefaultView(m_Panel.logListBox.ItemsSource) as CollectionView;
            m_LogCollectionView.Filter = CustomFilter;

            m_Panel.textFilter.PreviewTextInput += (sender, e) =>
            {
                m_LogCollectionView.Refresh();
            };
            m_Panel.textFilter.PreviewKeyUp += (sender, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Back
                || e.Key == System.Windows.Input.Key.Delete
                || e.Key == System.Windows.Input.Key.Enter)
                {
                    m_LogCollectionView.Refresh();
                }
            };

            m_Panel.errorSwitch.Click += (sender, e) =>
            {
                m_LogCollectionView.Refresh();
            };
            m_Panel.messageSwitch.Click += (sender, e) =>
            {
                m_LogCollectionView.Refresh();
            };

            // 清理日志
            m_Panel.clearAllButton.Click += (sender, e) =>
            {
                var task = m_Panel.DataContext as Model.ILoggable;
                if (task != null)
                {
                    task.Logs.Clear();
                    task.MessageCount = 0;
                    task.ErrorCount = 0;
                }
            };

        }

        /// <summary>
        /// 文本筛选函数
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool CustomFilter(object obj)
        {
            Model.Log log = obj as Model.Log;
            // 根据文本框内容筛选
            if (!string.IsNullOrEmpty(m_Panel.textFilter.Text))
            {
                if (log.Content.IndexOf(m_Panel.textFilter.Text, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }
            // 控制错误日志的显示
            if (m_Panel.errorSwitch.IsChecked == false && log.Type == Model.eLogType.Error)
            {
                return false;
            }
            // 控制消息日志的显示
            if (m_Panel.messageSwitch.IsChecked == false && log.Type == Model.eLogType.Message)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 初始化命令处理器
        /// </summary>
        private void InitCommands()
        {
            // 复制日志
            CommandBinding copyCommandBinding = new CommandBinding();
            copyCommandBinding.Command = Command.ExeboxCommands.CopyLogs;
            copyCommandBinding.CanExecute += (sender, e) =>
            {
                e.CanExecute = m_Panel.logListBox.SelectedItems.Count > 0;
                e.Handled = true;
            };
            copyCommandBinding.Executed += (sneder, e) =>
            {
                StringBuilder copyedContent = new StringBuilder();
                var logs = m_Panel.logListBox.SelectedItems;
                //Model.LogTaskManager.LogMessage(items.GetType().FullName);
                if (logs != null && logs.Count > 0)
                {
                    foreach (var iterator in logs)
                    {
                        var log = iterator as Model.Log;
                        if (log == null) break;
                        copyedContent.AppendLine(log.Content);
                    }
                }
                Clipboard.SetDataObject(copyedContent.ToString());
                Model.LogTaskManager.LogTip("复制完成");
                e.Handled = true;
            };
            m_Panel.CommandBindings.Add(copyCommandBinding);

            // 全选 （快捷键使用时不是由下面函数响应的, 自动绑定快捷键？）
            CommandBinding selectAllCommandBinding = new CommandBinding();
            selectAllCommandBinding.Command = Command.ExeboxCommands.SelectAll;
            selectAllCommandBinding.CanExecute += (sender, e) =>
            {
                e.CanExecute = m_Panel.logListBox.SelectedItems.Count > 0;
                e.Handled = true;
            };
            selectAllCommandBinding.Executed += (sneder, e) =>
            {
                m_Panel.logListBox.SelectAll();
                e.Handled = true;
            };
            m_Panel.CommandBindings.Add(selectAllCommandBinding);

            // 保存日志
            CommandBinding saveLogsCommandBinding = new CommandBinding();
            saveLogsCommandBinding.Command = Command.ExeboxCommands.SaveLogs;
            saveLogsCommandBinding.CanExecute += (sender, e) =>
            {
                e.CanExecute = m_Panel.logListBox.Items.Count > 0;
                e.Handled = true;
            };
            saveLogsCommandBinding.Executed += (sneder, e) =>
            {
                List<Model.Log> logList = new List<Model.Log>();
                var task = m_Panel.DataContext as Model.ILoggable;

                if (task != null)
                {
                    logList = task.Logs.ToList();
                }
                var saveFileDialog = new SaveFileDialog()
                {
                    Filter = "文本文件（*.txt）|*.txt",
                    Title = "保存日志"
                };
                saveFileDialog.FileName = string.Format("{0}_{1}", task.TaskName, DateTime.Now.ToString("yyyyMMddHHmmss"));
                if (saveFileDialog.ShowDialog() == true){
                    if (string.IsNullOrEmpty(saveFileDialog.FileName)) return;
                    using (var sw = new StreamWriter(saveFileDialog.FileName))
                    {

                        foreach (var log in logList)
                        {
                            sw.WriteLine(log.Content);
                        }

                    }
                    Model.LogTaskManager.LogTip($"{task.TaskName}日志已保存");
                }
                e.Handled = true;
            };
            m_Panel.CommandBindings.Add(saveLogsCommandBinding);
        }
    }
}

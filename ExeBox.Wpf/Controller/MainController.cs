﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using Xceed.Wpf.AvalonDock.Layout;

namespace ExeBox.Wpf.Controller
{
    class MainController
    {
        MainWindow main;
        Model.LogTaskManager manager;
        Dictionary<Model.LogTask, LayoutDocument> m_Documents;
        //List<Model.LogTaskConfig> m_Configs;
        List<View.TaskSelection> m_TaskSelections;

        public MainController(MainWindow mainWindow)
        {
            this.main = mainWindow ?? throw new ArgumentNullException("MainWindow argument can't be null.");
            manager = Model.LogTaskManager.Instance;
            m_Documents = new Dictionary<Model.LogTask, LayoutDocument>();
            //m_Configs = new List<Model.LogTaskConfig>();
            m_TaskSelections = new List<View.TaskSelection>();
        }

        public void Init()
        {
            var configs = LoadConfigFile();

            manager.Init(configs);

            #region  初始化主进程日志 ↓

            #endregion
            main.mainLogPane.DataContext = manager;

            #region 初始化任务浏览器 ↖

            #endregion
            main.logExplorer.ItemsSource = manager.Tasks;
            // 使TreeViewItem可以右键选中
            main.logExplorer.PreviewMouseRightButtonUp += (sender, e) =>
            {
                // 事件源是TreeViewItem的子控件
                var treeViewItem = (e.OriginalSource as DependencyObject).VisualUpwardSearch<TreeViewItem>();
                if (treeViewItem != null)
                {
                    treeViewItem.Focus();
                    //e.Handled = true;
                }
            };

            #region 初始化日志面板 ↗

            #endregion
            InitLogPanels();

            #region 初始化命令

            #endregion
            InitCommandHandler();

            manager.StartTasks();
            main.Closing += (sender, e) =>
            {
                MessageBoxResult result = System.Windows.MessageBox.Show("关闭所有任务并退出？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.None);
                if (result == MessageBoxResult.OK)
                {
                    e.Cancel = false;
                    manager.EndTasks();
                }
                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            };
        }

        //从配置文件中加载配置
        private List<Model.LogTaskConfig> LoadConfigFile()
        {

            XElement file = XElement.Load(@"ExecBox.xml");
            IEnumerable<XElement> elements = from element in file.Elements("run")
                                             select element;
            var configs = new List<Model.LogTaskConfig>();
            foreach (var element in elements)
            {
                var config = new Model.LogTaskConfig()
                {
                    Name = element.Attribute("cmd").Value,
                    Dir = element.Attribute("dir").Value,
                    Param = element.Attribute("param").Value,
                };
                configs.Add(config);
                m_TaskSelections.Add(new View.TaskSelection() { Config = config, IsSelected = (element.Attribute("enabled").Value == "1") });
            }

            //选择启动进程 弹出选择界面
            //在启动界面如果选择关闭选择界面 则认为什么都不选并退出程序
            View.TaskPickDialog dialog = new View.TaskPickDialog(ref m_TaskSelections);
            bool closedByEnsure = false;
            dialog.ensureButton.Click += (_sender, _e) =>
            {
                closedByEnsure = true;
                dialog.Close();
            };
            dialog.Closing += (_sender, _e) =>
            {
                if (!closedByEnsure)
                {
                    foreach (var selection in m_TaskSelections)
                    {
                        selection.IsSelected = false;
                    }
                    main.Close();
                    Process.GetCurrentProcess().CloseMainWindow();
                }
            };
            dialog.Top = main.Top + main.Height / 2 - dialog.Height / 2;
            dialog.Left = main.Left + main.Width / 2 - dialog.Width / 2;
            //dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialog.ShowDialog();


            foreach (var selection in m_TaskSelections)
            {
                if (selection.IsSelected == false)
                {
                    configs.Remove(selection.Config);
                }
            }

            return configs;
        }

        //为所有LogTask创建日志面板
        private void InitLogPanels()
        {
            foreach (var task in manager.Tasks)
            {
                AddTaskPage(task);
            }
        }

        //显示指定的日志
        private void ShowDocument(Model.LogTask task)
        {
            if (!m_Documents.ContainsKey(task)) return;
            var document = m_Documents[task];
            if (!main.documentsRoot.Contains(document))
            {
                main.documentsRoot.Children.Add(document);
            }
            // Extensions
            main.documentsRoot.SetActiveDocument(document);
        }

        private void InitCommandHandler()
        {
            CanExecuteRoutedEventHandler explorerCheck = (sender, e) =>
            {
                var treeItem = e.OriginalSource as TreeViewItem;
                if (treeItem != null && treeItem.DataContext is Model.LogTask)
                {
                    e.CanExecute = true;
                    e.Handled = true;
                }
            };

            // 打开日志页
            CommandBinding openCommandBinding = new CommandBinding();
            openCommandBinding.Command = Command.ExeboxCommands.OpenPage;
            openCommandBinding.CanExecute += (sender, e) =>
            {
                var treeItem = e.OriginalSource as TreeViewItem;
                if (treeItem != null && treeItem.DataContext is Model.LogTask)
                {
                    e.CanExecute = true;
                    e.Handled = true;
                }
            };
            openCommandBinding.Executed += (sneder, e) =>
            {
                var treeItem = e.OriginalSource as TreeViewItem;
                if (treeItem == null) return;

                var task = treeItem.DataContext as Model.LogTask;
                ShowDocument(task);
            };
            main.CommandBindings.Add(openCommandBinding);

            // 关闭日志页
            CommandBinding closeCommandBinding = new CommandBinding();
            closeCommandBinding.Command = Command.ExeboxCommands.ClosePage;
            closeCommandBinding.CanExecute += (sender, e) =>
            {
                var treeItem = e.OriginalSource as TreeViewItem;
                if (treeItem != null && treeItem.DataContext is Model.LogTask)
                {
                    var task = treeItem.DataContext as Model.LogTask;
                    e.CanExecute = main.documentsRoot.Contains(m_Documents[task]);
                    e.Handled = true;
                }
            };
            closeCommandBinding.Executed += (sneder, e) =>
            {
                var treeItem = e.OriginalSource as TreeViewItem;
                if (treeItem == null) return;

                var task = treeItem.DataContext as Model.LogTask;
                m_Documents[task].Close();
            };
            main.CommandBindings.Add(closeCommandBinding);

            // 停止进程
            CommandBinding stopCommandBinding = new CommandBinding();
            stopCommandBinding.Command = Command.ExeboxCommands.StopTask;
            stopCommandBinding.CanExecute += (sender, e) =>
            {
                var treeItem = e.OriginalSource as TreeViewItem;
                if (treeItem != null && treeItem.DataContext is Model.LogTask)
                {
                    var task = treeItem.DataContext as Model.LogTask;
                    e.CanExecute = (task.Status == Model.eLogTaskStatus.Running);
                    e.Handled = true;
                }
            };
            stopCommandBinding.Executed += (sneder, e) =>
            {
                var treeItem = e.OriginalSource as TreeViewItem;
                if (treeItem == null) return;

                var task = treeItem.DataContext as Model.LogTask;
                task.End();
            };
            main.CommandBindings.Add(stopCommandBinding);

            // 重新启动进程
            CommandBinding restartCommandBinding = new CommandBinding();
            restartCommandBinding.Command = Command.ExeboxCommands.RestartTask;
            restartCommandBinding.CanExecute += (sender, e) =>
            {
                var treeItem = e.OriginalSource as TreeViewItem;
                if (treeItem != null && treeItem.DataContext is Model.LogTask)
                {
                    var task = treeItem.DataContext as Model.LogTask;
                    e.CanExecute = (task != null);
                    e.Handled = true;
                }
            };
            restartCommandBinding.Executed += (sneder, e) =>
            {
                var treeItem = e.OriginalSource as TreeViewItem;
                if (treeItem == null) return;

                var task = treeItem.DataContext as Model.LogTask;
                task.Restart();
                ShowDocument(task);
            };
            main.CommandBindings.Add(restartCommandBinding);

            // 停止所有进程
            CommandBinding stopAllCommandBinding = new CommandBinding();
            stopAllCommandBinding.Command = Command.ExeboxCommands.StopAllTasks;
            stopAllCommandBinding.CanExecute += (sender, e) =>
            {
                foreach (var task in manager.Tasks)
                {
                    if (task.Status == Model.eLogTaskStatus.Running)
                    {
                        e.CanExecute = true;
                        break;
                    }
                }
                e.Handled = true;
            };
            stopAllCommandBinding.Executed += (sneder, e) =>
            {
                manager.EndTasks();
            };
            main.CommandBindings.Add(stopAllCommandBinding);

            // 重启所有进程
            CommandBinding restartAllCommandBinding = new CommandBinding();
            restartAllCommandBinding.Command = Command.ExeboxCommands.RestartAllTasks;
            restartAllCommandBinding.CanExecute += (sender, e) =>
            {
                e.CanExecute = true;
                e.Handled = true;
            };
            restartAllCommandBinding.Executed += (sneder, e) =>
            {
                manager.RestartTasks();
                foreach (var task in manager.Tasks)
                {
                    ShowDocument(task);
                }
            };
            main.CommandBindings.Add(restartAllCommandBinding);

            // 重新选择任务
            CommandBinding modifySelectionsCommandBinding = new CommandBinding();
            modifySelectionsCommandBinding.Command = Command.ExeboxCommands.ModifySelections;
            modifySelectionsCommandBinding.CanExecute += (sender, e) =>
            {
                e.CanExecute = true;
                e.Handled = true;
            };
            modifySelectionsCommandBinding.Executed += (sender, e) =>
            {
                var selections = new List<View.TaskSelection>(m_TaskSelections);
                View.TaskPickDialog dialog = new View.TaskPickDialog(ref selections);
                dialog.ensureButton.Click += (_sender, _e) =>
                {
                    dialog.Close();
                    m_TaskSelections = selections;
                    UpdateTaskSelections();
                };
                dialog.Top = main.Top + main.Height / 2 - dialog.Height / 2;
                dialog.Left = main.Left + main.Width / 2 - dialog.Width / 2;
                //dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dialog.ShowDialog();

            };
            main.CommandBindings.Add(modifySelectionsCommandBinding);

            // 帮助-关于
            CommandBinding helpAboutCommandBinding = new CommandBinding();
            helpAboutCommandBinding.Command = Command.ExeboxCommands.About;
            helpAboutCommandBinding.CanExecute += (sender, e) =>
            {
                e.CanExecute = true;
                e.Handled = true;
            };
            helpAboutCommandBinding.Executed += (sender, e) =>
            {
                MessageBox.Show("像素软件 寻仙手游2 程序部 \n宋昊 \n欢迎反馈BUG");
            };
            main.CommandBindings.Add(helpAboutCommandBinding);
        }

        // 更新所选任务
        private void UpdateTaskSelections()
        {
            foreach (var selection in m_TaskSelections)
            {
                if (!selection.IsSelected && manager.Configs.Contains(selection.Config))
                {
                    var task = manager.RemoveTask(selection.Config);
                    RemoveTaskPage(task);
                }
                else if (selection.IsSelected && !manager.Configs.Contains(selection.Config))
                {
                    var task = manager.AddTask(selection.Config);
                    AddTaskPage(task);
                }
            }
        }

        private void AddTaskPage(Model.LogTask task)
        {
            var logPanel = new View.LogPanel();
            logPanel.DataContext = task;
            var document = new LayoutDocument();
            document.Content = logPanel;
            document.Title = task.Config.Name;
            main.documentsRoot.Children.Add(document);
            m_Documents.Add(task, document);
        }

        private void RemoveTaskPage(Model.LogTask task)
        {
            if (task == null) return;
            var document = m_Documents[task];
            if (document != null && main.documentsRoot.Children.Contains(document))
            {
                main.documentsRoot.Children.Remove(document);
            }
        }
    }
}
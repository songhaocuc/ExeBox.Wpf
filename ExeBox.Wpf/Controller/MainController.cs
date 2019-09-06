using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        const string DEFAULT_CONFIG_FILE = @"ExecBox.xml";

        MainWindow main;
        Model.LogTaskManager manager;
        Dictionary<Model.LogTask, LayoutDocument> m_Documents;
        // 从配置文件中加载的任务（包含未选择的）
        List<Model.LogTaskConfig> m_Configs;
        List<View.TaskSelection> m_TaskSelections;
        bool m_DirectExit = false;

        public MainController(MainWindow mainWindow)
        {
            this.main = mainWindow ?? throw new ArgumentNullException("MainWindow argument can't be null.");
            manager = Model.LogTaskManager.Instance;
            m_Documents = new Dictionary<Model.LogTask, LayoutDocument>();
            m_Configs = new List<Model.LogTaskConfig>();
            m_TaskSelections = new List<View.TaskSelection>();
        }

        public void Init(string filepath = DEFAULT_CONFIG_FILE)
        {
            
            Clear();

            //如果当前目录没有ExecBox.xml,选择自定义配置文件
            while (!System.IO.File.Exists(filepath))
            {
                Model.LogTaskManager.LogError($"找不到配置文件:[{filepath}]");
                filepath = SelectConfigFile();
            }

            manager.FileName = Path.GetFileNameWithoutExtension(filepath);
            var configs = LoadConfigFile(filepath);

            manager.Init(configs);
            main.DataContext = manager;

            //初始化主进程日志 ↓
            //main.mainLogPane.DataContext = manager;
            //main.logExplorer.DataContext = manager;
            //main.statusBar.DataContext = manager;

            //初始化任务浏览器 ↖
            //main.logExplorer.ItemsSource = manager.Tasks;

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

            #region 开始所有任务

            #endregion
            manager.StartAllTasks();
            foreach (var task in manager.Tasks)
            {
                ShowDocument(task);
            }


            main.Closing += (sender, e) =>
            {
                if (m_DirectExit)
                {
                    e.Cancel = false;
                    return;
                }

                MessageBoxResult result = System.Windows.MessageBox.Show("正常关闭所有任务并退出？\n    [Yes]:任务将会正常停止，这可能会花费一些时间。\n    [No]:将会强制退出", "提示", MessageBoxButton.YesNoCancel, MessageBoxImage.None);
                if (result == MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    manager.StopAllTasks(() =>
                    {
                        m_DirectExit = true;
                        Environment.Exit(0);
                    });
                }
                else if (result == MessageBoxResult.No)
                {
                    manager.EndAllTasks();
                    m_DirectExit = true;
                    Environment.Exit(0);
                }
                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            };
        }

        private string SelectConfigFile()
        {
            var filepath = string.Empty;
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "请选择配置文件";
            fileDialog.InitialDirectory = Environment.CurrentDirectory;
            fileDialog.Filter = "XML files(*.xml)|*.xml|All files(*.*)|*.*";
            fileDialog.RestoreDirectory = true;
            bool? result = fileDialog.ShowDialog();
            if (result == true)
            {
                filepath = fileDialog.FileName;
                Environment.CurrentDirectory = Path.GetDirectoryName(filepath);
            }
            else
            {
                Environment.Exit(0);
            }
            return filepath;
        }

        //从配置文件中加载配置
        private List<Model.LogTaskConfig> LoadConfigFile(string filepath)
        {

            XElement file = XElement.Load(filepath);
            IEnumerable<XElement> elements = from element in file.Elements("run")
                                             select element;
            var configs = new List<Model.LogTaskConfig>();
            foreach (var element in elements)
            {
                string name = "undefined";
                string dir = string.Empty;
                string param = string.Empty;
                int priority = 0;
                try
                {
                    name = element.Attribute("cmd").Value;
                    dir = element.Attribute("dir").Value;
                    param = element.Attribute("param").Value;
                    priority = int.Parse(element.Attribute("priority").Value);
                }
                catch
                {

                }
                // param 不能为空
                if (param == string.Empty)
                {
                    Model.LogTaskManager.LogError($"Empty [param] arrtibute, [name]:{name}");
                    break;
                }
                var config = new Model.LogTaskConfig()
                {
                    Name = name,
                    Dir = dir,
                    Param = param,
                    Priority = priority,
                };
                configs.Add(config);

                //enable没设置默认为没有选择
                bool enabled = false;
                try
                {
                    enabled = element.Attribute("enabled").Value == "1";
                }
                catch
                {

                }

                m_TaskSelections.Add(new View.TaskSelection() { Config = config, IsSelected = enabled });
            }

            m_Configs = new List<Model.LogTaskConfig>(configs);

            ////询问是否清理残留进程
            //MessageBoxResult result = MessageBox.Show("是否清理残留进程（清理与配置文件中exe文件的同名进程）？", "清理残留进程", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            //if (result == MessageBoxResult.OK)
            //{
            //    Model.LogTaskManager.ClearRemainTasks(configs);
            //}

            //选择启动进程 弹出选择界面
            //在启动界面如果选择关闭选择界面 则认为什么都不选并退出程序
            View.TaskPickDialog dialog = new View.TaskPickDialog(true, ref m_TaskSelections);
            bool closedByEnsure = false;
            dialog.ensureButton.Click += (_sender, _e) =>
            {
                closedByEnsure = true;
                if (dialog.clearCheck.IsChecked == true)
                {
                    main.Dispatcher.Invoke(()=>
                    {
                        Model.LogTaskManager.ClearRemainTasks(configs);
                    });
                }
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

        private void Clear()
        {
            manager.Clear();
            main.documentsRoot.Children.Clear();
            m_Documents.Clear();
            m_Configs.Clear();
            m_TaskSelections.Clear();
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

            //1 打开日志页
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
                Model.LogTaskManager.LogTip($"打开[{task.Config.Name}]日志页");
            };
            main.CommandBindings.Add(openCommandBinding);

            //2 关闭日志页
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

            //3 停止进程
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
                task.Stop();
            };
            main.CommandBindings.Add(stopCommandBinding);

            //4 强制结束进程
            CommandBinding endCommandBinding = new CommandBinding();
            endCommandBinding.Command = Command.ExeboxCommands.EndTask;
            endCommandBinding.CanExecute += (sender, e) =>
            {
                var treeItem = e.OriginalSource as TreeViewItem;
                if (treeItem != null && treeItem.DataContext is Model.LogTask)
                {
                    var task = treeItem.DataContext as Model.LogTask;
                    e.CanExecute = (task.Status != Model.eLogTaskStatus.Terminated);
                    e.Handled = true;
                }
            };
            endCommandBinding.Executed += (sneder, e) =>
            {
                var treeItem = e.OriginalSource as TreeViewItem;
                if (treeItem == null) return;

                var task = treeItem.DataContext as Model.LogTask;
                task.End();
            };
            main.CommandBindings.Add(endCommandBinding);

            //5 重新启动进程
            CommandBinding restartCommandBinding = new CommandBinding();
            restartCommandBinding.Command = Command.ExeboxCommands.RestartTask;
            restartCommandBinding.CanExecute += (sender, e) =>
            {
                var treeItem = e.OriginalSource as TreeViewItem;
                if (treeItem != null && treeItem.DataContext is Model.LogTask)
                {
                    var task = treeItem.DataContext as Model.LogTask;
                    if (task != null && task.Status != Model.eLogTaskStatus.Stopping)
                    {
                        e.CanExecute = true;
                    }
                    e.Handled = true;
                }
            };
            restartCommandBinding.Executed += (sneder, e) =>
            {
                var treeItem = e.OriginalSource as TreeViewItem;
                if (treeItem == null) return;

                var task = treeItem.DataContext as Model.LogTask;
                task.Restart(false, () =>
                {
                    this.main.Dispatcher.Invoke(() => { ShowDocument(task); });
                });
            };
            main.CommandBindings.Add(restartCommandBinding);

            //6 强制重新启动进程
            CommandBinding forceRestartCommandBinding = new CommandBinding();
            forceRestartCommandBinding.Command = Command.ExeboxCommands.ForceRestartTask;
            forceRestartCommandBinding.CanExecute += (sender, e) =>
            {
                var treeItem = e.OriginalSource as TreeViewItem;
                if (treeItem != null && treeItem.DataContext is Model.LogTask)
                {
                    var task = treeItem.DataContext as Model.LogTask;
                    e.CanExecute = (task != null);
                    e.Handled = true;
                }
            };
            forceRestartCommandBinding.Executed += (sneder, e) =>
            {
                var treeItem = e.OriginalSource as TreeViewItem;
                if (treeItem == null) return;

                var task = treeItem.DataContext as Model.LogTask;
                task.Restart(true);
                ShowDocument(task);
            };
            main.CommandBindings.Add(forceRestartCommandBinding);

            //7 停止所有进程
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
                manager.StopAllTasks();
            };
            main.CommandBindings.Add(stopAllCommandBinding);

            //8 强制结束所有进程
            CommandBinding endAllCommandBinding = new CommandBinding();
            endAllCommandBinding.Command = Command.ExeboxCommands.EndAllTasks;
            endAllCommandBinding.CanExecute += (sender, e) =>
            {
                foreach (var task in manager.Tasks)
                {
                    if (task.Status != Model.eLogTaskStatus.Terminated)
                    {
                        e.CanExecute = true;
                        break;
                    }
                }
                e.Handled = true;
            };
            endAllCommandBinding.Executed += (sneder, e) =>
            {
                manager.EndAllTasks();
            };
            main.CommandBindings.Add(endAllCommandBinding);

            //9 重启所有进程
            CommandBinding restartAllCommandBinding = new CommandBinding();
            restartAllCommandBinding.Command = Command.ExeboxCommands.RestartAllTasks;
            restartAllCommandBinding.CanExecute += (sender, e) =>
            {
                e.CanExecute = true;
                e.Handled = true;
            };
            restartAllCommandBinding.Executed += (sneder, e) =>
            {
                manager.RestartAllTasks(() =>
                {
                    this.main.Dispatcher.Invoke(() =>
                    {
                        foreach (var task in manager.Tasks)
                        {
                            ShowDocument(task);
                        }
                    });
                });
            };
            main.CommandBindings.Add(restartAllCommandBinding);

            //10 强制重启所有进程
            CommandBinding forceRestartAllCommandBinding = new CommandBinding();
            forceRestartAllCommandBinding.Command = Command.ExeboxCommands.ForceRestartAllTasks;
            forceRestartAllCommandBinding.CanExecute += (sender, e) =>
            {
                e.CanExecute = true;
                e.Handled = true;
            };
            forceRestartAllCommandBinding.Executed += (sneder, e) =>
            {
                manager.ForceRestartAllTasks();
                foreach (var task in manager.Tasks)
                {
                    ShowDocument(task);
                }
            };
            main.CommandBindings.Add(forceRestartAllCommandBinding);

            //11 重新选择任务
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
                View.TaskPickDialog dialog = new View.TaskPickDialog(false, ref selections);
                dialog.ensureButton.Click += (_sender, _e) =>
                {
                    dialog.Close();
                    if (dialog.clearCheck.IsChecked == true)
                    {
                        main.Dispatcher.Invoke(() =>
                        {
                            Model.LogTaskManager.ClearRemainTasks(m_Configs);
                        });
                    }
                    m_TaskSelections = selections;
                    UpdateTaskSelections();
                };
                dialog.Top = main.Top + main.Height / 2 - dialog.Height / 2;
                dialog.Left = main.Left + main.Width / 2 - dialog.Width / 2;
                //dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dialog.ShowDialog();

            };
            main.CommandBindings.Add(modifySelectionsCommandBinding);

            //12 帮助-关于
            CommandBinding helpAboutCommandBinding = new CommandBinding();
            helpAboutCommandBinding.Command = Command.ExeboxCommands.About;
            helpAboutCommandBinding.CanExecute += (sender, e) =>
            {
                e.CanExecute = true;
                e.Handled = true;
            };
            helpAboutCommandBinding.Executed += (sender, e) =>
            {
                View.AboutCard about = new View.AboutCard();
                about.Top = main.Top + main.Height / 2 - about.Height / 2;
                about.Left = main.Left + main.Width / 2 - about.Width / 2;
                about.ShowDialog();
                e.Handled = true;
            };
            main.CommandBindings.Add(helpAboutCommandBinding);

            //13 清理残留进程
            CommandBinding clearRemainTasksCommandBinding = new CommandBinding();
            clearRemainTasksCommandBinding.Command = Command.ExeboxCommands.ClearRemainTasks;
            clearRemainTasksCommandBinding.CanExecute += (sender, e) =>
            {
                e.CanExecute = true;
                e.Handled = true;
            };
            clearRemainTasksCommandBinding.Executed += (sender, e) =>
            {
                Model.LogTaskManager.ClearRemainTasks(m_Configs);
                e.Handled = true;
            };
            main.CommandBindings.Add(clearRemainTasksCommandBinding);

            //14 打开配置文件
            CommandBinding openConfigCommandBinding = new CommandBinding();
            openConfigCommandBinding.Command = Command.ExeboxCommands.OpenConfigFile;
            openConfigCommandBinding.CanExecute += (sender, e) =>
            {

                e.CanExecute = Model.LogTaskManager.Instance.IsAllTasksExited;
                e.Handled = true;
            };
            openConfigCommandBinding.Executed += (sender, e) =>
            {
                main.Dispatcher.Invoke(()=>
                {
                    Init(SelectConfigFile());
                });

                e.Handled = true;
            };
            main.CommandBindings.Add(openConfigCommandBinding);
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

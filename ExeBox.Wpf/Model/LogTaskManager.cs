using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExeBox.Wpf.Model
{
    class LogTaskManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        //先用单例来实现LogTaskManager
        public static LogTaskManager Instance
        {
            get
            {
                return LogTaskManagerInstance.INSTANCE;
            }
        }
        private static class LogTaskManagerInstance
        {
            public static readonly LogTaskManager INSTANCE = new LogTaskManager();
        }
        private LogTaskManager()
        {
            Logs = new ObservableCollection<Log>();
            Tasks = new ObservableCollection<LogTask>();
        }

        //打印ExeBox的日志
        public ObservableCollection<Log> Logs { get; set; }
        private int m_MessageCount;
        private int m_ErrorCount;
        public int MessageCount
        {
            get { return m_MessageCount; }
            set
            {
                m_MessageCount = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("MessageCount"));
                }
            }
        }
        public int ErrorCount
        {
            get { return m_ErrorCount; }
            set
            {
                m_ErrorCount = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("ErrorCount"));
                }
            }
        }
        //状态栏消息
        private string m_Tip;
        public string Tip
        {
            get { return m_Tip; }
            set { m_Tip = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Tip"));
                }
            }
        }

        /// <summary>
        /// LogTaskManager管理的所有LogTask
        /// </summary>
        public ObservableCollection<LogTask> Tasks { get; set; }
        public List<LogTaskConfig> Configs { get; private set; }

        //在stop所有任务时，由于是异步的，所以用事件来提示所有任务结束
        private event AllTasksStoppedEventHandler AllTasksStopped;

        /// <summary>
        /// 根据所给配置初始化日志任务
        /// </summary>
        /// <param name="configs"></param>
        public void Init(List<LogTaskConfig> configs)
        {
            LogTaskManager.LogMessage("初始化所有日志任务");
            Configs = configs;
            foreach (var config in Configs)
            {
                var task = new LogTask(config);
                Tasks.Add(task);
            }
            LogTaskManager.LogMessage("初始化完成");
        }

        /// <summary>
        /// 启动所有任务（同步）
        /// </summary>
        public void StartAllTasks()
        {
            // 按优先级升序启动
            ActionByPriority(true, (t) => { t.Start(); });
        }

        /// <summary>
        /// 结束所有任务（同步）
        /// </summary>
        public void EndAllTasks()
        {
            // 按照优先级降序结束
            ActionByPriority(false, (t) => { t.End(); });
        }


        /// <summary>
        /// 停止所有任务（异步）
        /// </summary>
        /// <param name="callback"></param>
        public void StopAllTasks(AllTasksStoppedEventHandler callback = null)
        {
            AllTasksStopped += callback;
            // 按照优先级降序结束
            var tasks = PriorityTaskQueue(false);
            DoStopTasks(tasks);
        }

        //递归结束剩余的任务
        private void DoStopTasks(Queue<LogTask> remainTasks)
        {
            if (remainTasks.Count > 0)
            {
                var task = remainTasks.Dequeue();
                if (task.Status == eLogTaskStatus.Running)
                {
                    task.Stop(() =>
                    {
                        DoStopTasks(remainTasks);
                    });
                }
                else
                {
                    DoStopTasks(remainTasks);
                }

            }
            else
            {
                AllTasksStopped?.Invoke();
                AllTasksStopped = null;
            }
        }

        /// <summary>
        /// 重启所有任务(异步)
        /// </summary>
        public void RestartAllTasks(Action callback = null)
        {
            StopAllTasks(() =>
            {
                StartAllTasks();
                callback?.Invoke();
            });
        }

        /// <summary>
        /// 强制重启所有任务（同步）
        /// </summary>
        public void ForceRestartAllTasks()
        {
            EndAllTasks();
            StartAllTasks();
        }

        /// <summary>
        /// 添加新的日志任务
        /// </summary>
        /// <param name="config"></param>
        public LogTask AddTask(LogTaskConfig config)
        {
            if (!Configs.Contains(config))
                Configs.Add(config);
            var addedTask = new LogTask(config);
            addedTask.Start();
            Tasks.Add(addedTask);
            return addedTask;
        }
        /// <summary>
        /// 移除日志任务
        /// </summary>
        /// <param name="config"></param>
        public LogTask RemoveTask(LogTaskConfig config)
        {
            if (Configs.Contains(config))
                Configs.Remove(config);
            Model.LogTask removedTask = null;
            foreach (var item in Tasks)
            {
                if (item.Config == config)
                {
                    removedTask = item;
                }
            }
            Tasks.Remove(removedTask);
            removedTask.End();
            return removedTask;
        }


        /// <summary>
        /// 按照优先级对任务进行操作
        /// </summary>
        /// <param name="up">是否升序（升序：优先级低的先执行）</param>
        /// <param name="action">操作内容</param>
        private void ActionByPriority(bool up, Action<LogTask> action)
        {
            var tasks = new List<LogTask>(this.Tasks);
            tasks.Sort((LogTask t1, LogTask t2) =>
            {
                var p1 = t1.Config.Priority;
                var p2 = t2.Config.Priority;
                // up 按照升序
                var result = up ? p1 - p2 : p2 - p1;
                return result;
            });

            foreach (var task in tasks)
            {
                action(task);
            }
        }
        /// <summary>
        /// 获取按优先级排列的任务队列，up为true时 优先级低的在队前
        /// </summary>
        /// <param name="up"></param>
        /// <returns></returns>
        private Queue<LogTask> PriorityTaskQueue(bool up)
        {
            var tasks = new List<LogTask>(this.Tasks);
            tasks.Sort((LogTask t1, LogTask t2) =>
            {
                var p1 = t1.Config.Priority;
                var p2 = t2.Config.Priority;
                // up 按照升序
                var result = up ? p1 - p2 : p2 - p1;
                return result;
            });
            return new Queue<LogTask>(tasks);
        }


        private void PrintMainLog(eLogType type, string content)
        {
            Logs.Add(new Log() { Type = type, Content = content });
        }
        /// <summary>
        /// 打印ExeBox消息日志
        /// </summary>
        /// <param name="type"></param>
        /// <param name="content"></param>
        public static void LogMessage(string content)
        {
            Instance.PrintMainLog(eLogType.Message, content);
            Instance.MessageCount++;
        }
        /// <summary>
        /// 打印ExeBox错误日志
        /// </summary>
        /// <param name="content"></param>
        public static void LogError(string content)
        {
            Instance.PrintMainLog(eLogType.Error, content);
            Instance.ErrorCount++;
        }
        /// <summary>
        /// 状态栏打印
        /// </summary>
        /// <param name="tip"></param>
        public static void LogTip(string tip)
        {
            Instance.Tip = tip;
        }
        public static void ClearRemainTasks(List<LogTaskConfig> configs)
        {
            List<string> processes = new List<string>();
            //提取进程名
            foreach (var config in configs)
            {
                processes.Add(Path.GetFileNameWithoutExtension(config.ExecFile));
            }
            processes = processes.Distinct().ToList();
            //清理进程
            foreach (var process in processes)
            {
                try
                {
                    //精确进程名  用GetProcessesByName
                    foreach (Process p in Process.GetProcessesByName(process))
                    {
                        if (!p.CloseMainWindow())
                        {
                            p.Kill();
                        }
                        LogTaskManager.LogMessage($"[ClearRemainTasks]清除进程[{process}]");
                    }
                }
                catch (Exception e)
                {
                    //LogTaskManager.LogError($"[ClearRemainTasks]{e.GetType().FullName}:{e.Message}");
                }
            }

            Thread.Sleep(300);
            //检查
            foreach (var process in processes)
            {
                LogTaskManager.LogMessage($"[ClearRemainTasks]进程[{process}]剩余数量: {Process.GetProcessesByName(process).Length}");
            }
        }
    }

    public delegate void AllTasksStoppedEventHandler();
}

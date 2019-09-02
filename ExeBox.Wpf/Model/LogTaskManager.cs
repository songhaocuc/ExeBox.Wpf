using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
        /// <summary>
        /// LogTaskManager管理的所有LogTask
        /// </summary>
        public ObservableCollection<LogTask> Tasks { get; set; }
        public List<LogTaskConfig> Configs { get; private set; }

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
                InitTask(config);
            }
            LogTaskManager.LogMessage("初始化完成");
        }

        public void StartTasks()
        {
            foreach (var task in Tasks)
            {
                task.Start();
            }
        }

        public void EndTasks()
        {
            foreach (var task in Tasks)
            {
                task.End();
            }
        }

        public void RestartTasks()
        {
            foreach (var task in Tasks)
            {
                task.Restart();
            }
        }

        private void InitTask(LogTaskConfig config)
        {
            if (!Configs.Contains(config))
                Configs.Add(config);
            var task = new LogTask(config);
            task.Init();
            Tasks.Add(task);
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
            addedTask.Init();
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
                if(item.Config == config)
                {
                    removedTask = item;
                }
            }
            Tasks.Remove(removedTask);
            removedTask.End();
            return removedTask;
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
    }
}

﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ExeBox.Wpf.Model
{
    /// <summary>
    /// 日志任务：
    ///     一条日志任务代表一个可执行文件的进程；
    ///     提供用于界面数据绑定的状态、配置、日志、日志总数等属性；
    ///     提供操控进程的接口。
    /// </summary>
    class LogTask : INotifyPropertyChanged
    {
        public LogTaskConfig Config { get; set; }
        public ObservableCollection<Log> Logs { get; set; }
        private int m_MessageCount;
        private int m_ErrorCount;
        private eLogTaskStatus m_Status;
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
        public eLogTaskStatus Status
        {
            get { return m_Status; }
            set
            {
                m_Status = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Status"));
                }
            }
        }

        private Thread m_StatusObserver;

        private delegate void PreviousProcessExitedEventHandler();
        private event PreviousProcessExitedEventHandler m_PreviousProcessExited;

        //执行进程
        private Process m_ExecProcess;
        private object m_LogsLock;

        public event PropertyChangedEventHandler PropertyChanged;

        public LogTask(LogTaskConfig config)
        {
            Config = config;

            Logs = new ObservableCollection<Log>();

            m_LogsLock = new object();
            BindingOperations.EnableCollectionSynchronization(Logs, m_LogsLock);

        }

        /// <summary>
        /// 初始化任务
        /// </summary>
        public void Init()
        {
            if (Config == null) return;
            //初始化执行进程
            m_ExecProcess = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(Config.Dir, Config.ExecFile);
            //startInfo.FileName = Config.ExecFile; //exe文件路径
            startInfo.Arguments = Config.Arguments;
            startInfo.UseShellExecute = false;   // 是否使用外壳程序 
            startInfo.CreateNoWindow = true;   //是否在新窗口中启动该进程的值 
            startInfo.RedirectStandardInput = true;  // 重定向输入流 
            startInfo.RedirectStandardOutput = true;  //重定向输出流 
            startInfo.RedirectStandardError = true;  //重定向错误流 

            m_ExecProcess.StartInfo = startInfo;

            //设置Output输出回调
            m_ExecProcess.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    MessageCount++;
                    PrintLog(eLogType.Message, e.Data);
                }
            });

            //设置Error输出回调
            m_ExecProcess.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    ErrorCount++;
                    PrintLog(eLogType.Error, e.Data);
                }
            });


            m_ExecProcess.Exited += (sender, e) =>
            {
                Status = eLogTaskStatus.Terminated;
            };


            m_StatusObserver = new Thread(() =>
            {
                m_ExecProcess.WaitForExit();
                Status = eLogTaskStatus.Terminated;
                //m_StatusObserver.Abort();
                m_PreviousProcessExited?.Invoke();
                m_PreviousProcessExited = null;
            });

        }

        /// <summary>
        /// 开始任务
        /// </summary>
        public void Start()
        {
            
            m_ExecProcess.Start();
            // Asynchronously read the standard output of the spawned process. 
            // This raises OutputDataReceived events for each line of output.
            m_ExecProcess.BeginOutputReadLine();
            m_ExecProcess.BeginErrorReadLine();

            // 检测进程是否关闭
            m_StatusObserver.Start();
            Status = eLogTaskStatus.Running;

        }

        /// <summary>
        /// 结束任务
        /// </summary>
        public void End()
        {
            if (m_ExecProcess!=null &&!m_ExecProcess.HasExited)
            {
                m_ExecProcess.Kill();
                //m_ExecProcess.CloseMainWindow();
                //m_ExecProcess.Close();
            }
                
            m_ExecProcess = null;
            Status = eLogTaskStatus.Terminated;

        }

        /// <summary>
        /// 重新启动
        /// </summary>
        public void Restart()
        {
            if (m_ExecProcess != null && !m_ExecProcess.HasExited) {
                m_PreviousProcessExited += () =>
                {
                    Clear();
                    Init();
                    Start();
                };
                End();
            }
            else
            {
                Clear();
                Init();
                Start();
            }
        }

        // 打印日志，实质上是将日志添加到被绑定的Logs中
        private void PrintLog(eLogType type, string massage)
        {
            Log log = new Log() { Type = type, Content = massage };
            Logs.Add(log);
        }

        // 清理任务
        private void Clear()
        {
            Logs.Clear();
            ErrorCount = 0;
            MessageCount = 0;
            m_ExecProcess = null;
        }
    }


    /// <summary>
    /// 任务配置
    /// 通过解析配置XML文件获取
    /// </summary>
    public class LogTaskConfig
    {
        /// <summary>
        /// 命令名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 执行文件路径
        /// </summary>
        public string Dir { get; set; }

        private string param;
        /// <summary>
        /// 原始指令 Param = ExecFile + " " + Arguments
        /// </summary>
        public string Param
        {
            get { return param; }
            set
            {
                param = value;
                //将Param分割为ExecFile和Arguments
                var separator = new char[] { ' ' };
                var args = param.Split(separator, 2);
                ExecFile = args[0];
                if (args.Length > 1)
                    Arguments = args[1];
            }
        }

        /// <summary>
        /// 可执行文件
        /// </summary>
        public string ExecFile { get; private set; }
        /// <summary>
        /// 执行参数
        /// </summary>
        public string Arguments { get; private set; }
    }

    enum eLogTaskStatus
    {
        None,
        Initialized,
        Running,
        Terminated
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExeBox.Wpf.Model
{
    /// <summary>
    /// 日志类型
    /// </summary>
    enum eLogType
    {
        Message,
        Error
    }

    /// <summary>
    /// 日志数据模型
    /// </summary>
    class Log
    {
        public eLogType Type { get; set; }
        public string Content { get; set; }
    }
}

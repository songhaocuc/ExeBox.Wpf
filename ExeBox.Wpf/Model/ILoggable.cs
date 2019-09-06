using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExeBox.Wpf.Model
{
    interface ILoggable
    {
        ObservableCollection<Log> Logs { get;}
        int MessageCount { get; set; }
        int ErrorCount { get; set; }
        string TaskName { get; }
    }
}

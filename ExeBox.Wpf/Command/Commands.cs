
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ExeBox.Wpf.Command
{
    class ExeboxCommands
    {
        const int CommandAmount = 10;
        private enum CommandId : byte
        {
            CopyLogs,                   //复制 
            SelectAll,                  //选中所有
            OpenPage,                   //打开日志页
            ClosePage,                  //关闭日志页
            StopTask,                   //停止任务
            RestartTask,                //重启任务
            StopAllTasks,               //停止所有任务
            RestartAllTasks,            //重启所有任务
            ModifySelections,            //修改任务选择
            About
        }

        private static RoutedUICommand[] _internalCommands = new RoutedUICommand[CommandAmount];

        public static RoutedUICommand CopyLogs => _EnsureCommand(CommandId.CopyLogs);
        public static RoutedUICommand SelectAll => _EnsureCommand(CommandId.SelectAll);
        public static RoutedUICommand OpenPage => _EnsureCommand(CommandId.OpenPage);
        public static RoutedUICommand ClosePage => _EnsureCommand(CommandId.ClosePage);
        public static RoutedUICommand StopTask => _EnsureCommand(CommandId.StopTask);
        public static RoutedUICommand RestartTask => _EnsureCommand(CommandId.RestartTask);
        public static RoutedUICommand StopAllTasks => _EnsureCommand(CommandId.StopAllTasks);
        public static RoutedUICommand RestartAllTasks => _EnsureCommand(CommandId.RestartAllTasks);
        public static RoutedUICommand ModifySelections => _EnsureCommand(CommandId.ModifySelections);
        public static RoutedUICommand About => _EnsureCommand(CommandId.About);

        private static InputGestureCollection LoadDefaultGestureFromResource(CommandId commandId)
        {
            InputGestureCollection inputGestureCollection = new InputGestureCollection();
            switch (commandId)
            {
                case CommandId.CopyLogs:
                    inputGestureCollection.Add(new KeyGesture(Key.C, ModifierKeys.Control));
                    break;
                case CommandId.SelectAll:
                    inputGestureCollection.Add(new KeyGesture(Key.A, ModifierKeys.Control));
                    break;
                case CommandId.OpenPage:
                    break;
                case CommandId.ClosePage:
                    break;
                case CommandId.StopTask:
                    break;
                case CommandId.RestartTask:
                    break;
                case CommandId.StopAllTasks:
                    break;
                case CommandId.RestartAllTasks:
                    break;
                default:
                    break;
            }
            return inputGestureCollection;
        }
        private static RoutedUICommand _EnsureCommand(CommandId idCommand)
        {
            if ((int)idCommand >= 0 && (int)idCommand < CommandAmount)
            {
                lock (_internalCommands.SyncRoot)
                {
                    if (_internalCommands[(uint)idCommand] == null)
                    {
                        RoutedUICommand routedUICommand = new RoutedUICommand(idCommand.ToString(), idCommand.ToString(), typeof(ExeboxCommands), LoadDefaultGestureFromResource(idCommand));
                        _internalCommands[(uint)idCommand] = routedUICommand;
                    }
                }
                return _internalCommands[(uint)idCommand];
            }
            return null;
        }
    }
}

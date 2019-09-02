using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Xceed.Wpf.AvalonDock.Layout;

namespace ExeBox.Wpf
{
    //一些AvalonDock的扩展方法
    static class AvalonDockExtensions
    {
        /// <summary>
        /// 判断容器是否包含某个LayoutContent
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pane"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static bool Contains<T>(this LayoutPositionableGroup<T> pane, T doc) where T : LayoutContent
        {
            return pane.Children.IndexOf(doc) != -1;
        }

        /// <summary>
        /// 设置LayoutContent选中
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pane"></param>
        /// <param name="doc"></param>
        public static void SetActiveDocument<T>(this LayoutPositionableGroup<T> pane, T doc) where T : LayoutContent
        {
            foreach (var document in pane.Children)
            {
                document.IsActive = (document == doc);
            }
        }
    }

    static class ControlExtensions
    {
        /// <summary>
        /// 找到指定类型的父节点
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static T VisualUpwardSearch<T>(this DependencyObject source) where T: FrameworkElement
        {
            if (source is Run) return null;
            while (source != null && source.GetType() != typeof(T))
                source = VisualTreeHelper.GetParent(source);

            return source as T;
        }

    }
}

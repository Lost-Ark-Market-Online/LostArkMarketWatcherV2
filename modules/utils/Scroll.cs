using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace LostArkMarketWatcherV2.Modules.Utils
{
    public partial class Scroll
    {
        private static ScrollViewer FindViewer(DependencyObject root)
        {
            var queue = new Queue<DependencyObject>(new[] { root });

            do
            {
                var item = queue.Dequeue();
                if (item is ScrollViewer) { return (ScrollViewer)item; }
                var count = VisualTreeHelper.GetChildrenCount(item);
                for (var i = 0; i < count; i++) { queue.Enqueue(VisualTreeHelper.GetChild(item, i)); }
            } while (queue.Count > 0);

            return null;
        }

        public static void ToBottom(System.Windows.Controls.ListBox listBox)
        {
            var scrollViewer = FindViewer(listBox);

            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += (o, args) =>
                {
                    if (args.ExtentHeightChange > 0) { scrollViewer.ScrollToBottom(); }
                };
            }
        }
    }
}


using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Controls;
using ListBox = System.Windows.Controls.ListBox;

namespace LostArkMarketWatcherV2.Modules.Utils
{   
    public class ConsoleWritter: TextWriter
    {
        private readonly ListBox _listBox;
        private readonly Levels _level;
        public enum Levels
        {
            Debug,
            Info,
            Error
        }

        public ConsoleWritter(ListBox listBox, Levels level)
        {
            _listBox = listBox;
            _level = level;
        }

        public override void Write(char value)
        {
            _listBox.Dispatcher.Invoke(() => _listBox.Items.Add(value));
        }

        public override Encoding Encoding => Encoding.ASCII;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;

namespace ExceptionMonitor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //读取配置
            var fileSavePath = ConfigurationManager.AppSettings["fileSavePath"];
        }

        // 运行程序
        private void CheckBox_RunApp(object sender, RoutedEventArgs e)
        {

        }

        // 设置开机自启动
        private void CheckBox_Bootstrap(object sender, RoutedEventArgs e)
        {

        }

        // 设置文件目录
        private void Button_SetFilePath(object sender, RoutedEventArgs e)
        {
        }

        // 打开文件目录
        private void Button_OpenFilePath(object sender, RoutedEventArgs e)
        {

        }
    }
}

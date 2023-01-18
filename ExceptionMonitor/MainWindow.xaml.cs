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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Threading;
using System.IO;

namespace ExceptionMonitor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Boolean isRun = false;
        int nTimeInterval = 3;
        DateTime nLastRunTime = DateTime.Now;
        DispatcherTimer dispatcherTimer = null;

        //是否解锁
        bool isUnlock = false;

        public MainWindow()
        {
            InitializeComponent();

            //窗口事件
            this.Closing += Window_Closing;
            this.Closed += Window_Closed;
            this.StateChanged += Window_StateChanged;
            this.Activated += Window_Activated;

            //系统托盘图标
            var systemtray = SystemTray.Instance;

            //读取配置
            var fileSavePath = ConfigurationManager.AppSettings["fileSavePath"];
            var strTimeInterval = ConfigurationManager.AppSettings["timeInterval"];
            var strIsRun = ConfigurationManager.AppSettings["isRun"];

            //更新到界面
            this.filePath.Text = fileSavePath;
            this.timeInterval.Text = strTimeInterval;

            //应用配置
            var result = int.TryParse(strTimeInterval, out this.nTimeInterval);
            if (!result) {
                this.nTimeInterval = 3;
                strTimeInterval = String.Format("{0:D}", this.nTimeInterval);
                this.timeInterval.Text = strTimeInterval;
            }
            int nIsRun = 0;
            result = int.TryParse(strIsRun, out nIsRun);
            if (!result) {
                this.isRun = false;
            }
            
            //打开计时器
            this.dispatcherTimer = new DispatcherTimer();
            this.dispatcherTimer.Interval = TimeSpan.FromMilliseconds(1000);  // 1000ms 更新一次
            this.dispatcherTimer.Tick += DispatcherTimer_Tick;
            this.dispatcherTimer.Start();

            //运行配置
            this.isRun = false;
            if (nIsRun == 1) {
                this.isRun = true;
            }
            this.runApp.Content = this.isRun ? "停止截图" : "启动截图";
        }

        // 每秒更新的计时器
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (this.isRun == false) {
                return;
            }

            //时间间隔
            var strTimeInterval = ConfigurationManager.AppSettings["timeInterval"];

            //目录检查, 没有目录就停止运行
            var fileSavePath = ConfigurationManager.AppSettings["fileSavePath"];
            if (fileSavePath.Length <= 0 || Directory.Exists(fileSavePath) == false) { 
                this.runApp.Content = "启动截图";
                this.isRun = false;
                MessageBox.Show("截图保存目录不存在, 停止运行!");
                return;
            }

            //时间间隔检查
            var subTime = (DateTime.Now - nLastRunTime).TotalSeconds;
            if (subTime < this.nTimeInterval) {
                return;
            }
            this.nLastRunTime = DateTime.Now;
            
            //截图并保存
            var day = DateTime.Now.ToLocalTime().ToString("yyyy_MM_dd-hh_mm_ss");
            var fileName = "screen_" + day + ".jpg";
            var screen = MainWindow.CaptureCurrentScreen();
            screen.Save(fileSavePath + "\\" + fileName);
            Console.WriteLine(fileName);
        }

        // 运行程序
        private void CheckBox_RunApp(object sender, RoutedEventArgs e)
        {
            Configuration _configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            //运行中点击, 直接停止运行
            if (this.isRun) {
                this.isRun = false;
                this.runApp.Content = "启动截图";
                Console.WriteLine("停止运行");
                _configuration.AppSettings.Settings["isRun"].Value = (bool)this.isRun ? "1" : "0";
                _configuration.Save();
                ConfigurationManager.RefreshSection("appSettings");
                return;
            }

            //读取当前输入的时间
            var strTimeInterval = this.timeInterval.Text;
            var nInterval = -1;
            var result = int.TryParse(strTimeInterval, out nInterval);
            if (!result) {
                this.runApp.Content = "启动截图";
                MessageBox.Show("时间间隔输入有误, 请重新输入");
                return;
            }
            if (nInterval < 3) {
                nInterval = 3;
            }
            this.nTimeInterval = nInterval;
            strTimeInterval = String.Format("{0:D}", nInterval);
            this.timeInterval.Text = strTimeInterval;

            //启动截图工具
            this.isRun = !this.isRun;
            this.runApp.Content = "停止截图";
            Console.WriteLine("开始运行");

            //保存输入的时间间隔和是否在运行中配置
            _configuration.AppSettings.Settings["timeInterval"].Value = strTimeInterval;
            _configuration.AppSettings.Settings["isRun"].Value = (bool)this.isRun ? "1" : "0";
            _configuration.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }

        // 设置开机自启动
        private void CheckBox_Bootstrap(object sender, RoutedEventArgs e)
        {

        }

        // 设置文件目录
        private void Button_SetFilePath(object sender, RoutedEventArgs e)
        {
            //打开目录
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog.Description = "请选择一个用来保存截图的目录:";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;    //设置初始目录
            folderBrowserDialog.ShowDialog();        //这个方法可以显示文件夹选择对话框
            string directoryPath = folderBrowserDialog.SelectedPath;    //获取选择的文件夹的全路径名
            if (directoryPath.Length <= 0) {
                //MessageBox.Show("选择的路径无效, 请重新选择路径！");
                return;
            }

            //设置文件目录, 等待回调
            var fileName = directoryPath;
            this.filePath.Text = fileName;

            //保存配置
            Configuration _configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            _configuration.AppSettings.Settings["fileSavePath"].Value = fileName;
            _configuration.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }

        // 打开文件目录
        private void Button_OpenFilePath(object sender, RoutedEventArgs e)
        {
            var fileSavePath = ConfigurationManager.AppSettings["fileSavePath"];
            System.Diagnostics.Process.Start(fileSavePath);
        }
        
        // 获取当前截屏
        public static Bitmap CaptureCurrentScreen()
        {
            //创建与屏幕大小相同的位图对象
            var bmpScreen = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            //使用位图对象来创建Graphics的对象
            using (Graphics g = Graphics.FromImage(bmpScreen))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;   //设置平滑模式，抗锯齿
                g.CompositingQuality = CompositingQuality.HighQuality;  //设置合成质量
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;     //设置插值模式
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;   //设置文本呈现的质量
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;    //设置呈现期间，像素偏移的方式

                //利用CopyFromScreen将当前屏幕截图并将内容存储在bmpScreen的位图中
                g.CopyFromScreen(0, 0, 0, 0, bmpScreen.Size, CopyPixelOperation.SourceCopy);
            }

            return bmpScreen;
        }

        /// <summary>
        /// 关闭前回调事件
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.isUnlock) {
                return;
            }

            //没有解锁, 不能关闭, 直接最小化
            e.Cancel = true;
            this.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 窗体退出事件
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            SystemTray.Instance.DisposeNotifyIcon();
        }

        /// <summary>
        /// 窗体状态改变
        /// </summary>
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized){
                this.Hide();
            }
        }

        /// <summary>
        /// 创建进入前台
        /// </summary>
        private void Window_Activated(object sender, EventArgs e)
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            gridPassword.Visibility = Visibility.Visible;
            this.isUnlock = false;
        }

        /// <summary>
        /// 检查输入的密码
        /// </summary>
        private void BtnPassword_Click(object sender, RoutedEventArgs e)
        {
            var paaaword = this.inputPassword.Text;
            if (paaaword == "dabukai.123") {
                this.inputPassword.Text = "";
                gridPassword.Visibility = Visibility.Hidden;
                this.isUnlock = true;
            }
        }
    }

    /// <summary>
    /// 系统托盘图标管理类
    /// </summary>
    public class SystemTray
    {
        public static SystemTray Instance;

        /// <summary>
        /// 静态构造函数,在类第一次被创建或者静态成员被调用的时候调用
        /// </summary>
        static SystemTray()
        {
            Instance = new SystemTray();
        }

        public System.Windows.Forms.NotifyIcon Ni { get; set; }

        private SystemTray()
        {
            string path = System.IO.Path.GetFullPath(@"icon/icon.ico");
            if (File.Exists(path))
            {
                Ni = new System.Windows.Forms.NotifyIcon();
                System.Drawing.Icon icon = new System.Drawing.Icon(path);//程序图标
                Ni.Icon = icon;
                Ni.Text = "截图工具";
                Ni.Visible = true;
                Ni.MouseClick += this.Ni_MouseClick;
            }
        }

        private void Ni_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            App.Current.MainWindow.Show();
            App.Current.MainWindow.Activate();
            App.Current.MainWindow.WindowState = WindowState.Maximized;
        }

        /// <summary>
        /// 销毁系统托盘图标的资源
        /// </summary>
        public void DisposeNotifyIcon()
        {
            Ni?.Dispose();
        }
    }
}

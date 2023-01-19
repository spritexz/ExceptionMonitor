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

//using IWshRuntimeLibrary;
using System.Diagnostics;

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

            //开机启动配置
            if (Powerboot.Instance.CheckOpen()) {
                powerboot.Content = "关闭开机启动";
            } else {
                powerboot.Content = "设为开机启动";
            }

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

            //启动后自动最小化
            this.WindowState = WindowState.Minimized;
            this.Hide();
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
            screen.Dispose();
            screen = null;
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
            if (Powerboot.Instance.CheckOpen()) {
                Powerboot.Instance.SetMeAutoStart(false);
            } else {
                Powerboot.Instance.SetMeAutoStart(true);
            }
            if (Powerboot.Instance.CheckOpen()) {
                powerboot.Content = "关闭开机启动";
            } else {
                powerboot.Content = "设为开机启动";
            }
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
            if (this.WindowState == WindowState.Maximized){
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                gridPassword.Visibility = Visibility.Visible;
                this.isUnlock = false;
            }
        }

        /// <summary>
        /// 创建进入前台
        /// </summary>
        private void Window_Activated(object sender, EventArgs e)
        {
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

    /// <summary>
    /// 开机启动管理
    /// </summary>
    public class Powerboot
    {
        public static Powerboot Instance;

        static Powerboot()
        {
            Instance = new Powerboot();
        }

        private Powerboot()
        {
        }

        /// <summary>
        /// 快捷方式名称-任意自定义
        /// </summary>
        private const string QuickName = "ExceptionMonitor";

        /// <summary>
        /// 自动获取系统自动启动目录
        /// </summary>
        private string systemStartPath { get { return Environment.GetFolderPath(Environment.SpecialFolder.Startup); } }

        /// <summary>
        /// 自动获取程序完整路径
        /// </summary>
        private string appAllPath { get { return Process.GetCurrentProcess().MainModule.FileName; } }

        /// <summary>
        /// 自动获取桌面目录
        /// </summary>
        private string desktopPath { get { return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); } }

        /// <summary>
        /// 设置开机自动启动-只需要调用改方法就可以了参数里面的bool变量是控制开机启动的开关的，默认为开启自启启动
        /// </summary>
        /// <param name="onOff">自启开关</param>
        public void SetMeAutoStart(bool onOff = true)
        {
            if (onOff)//开机启动
            {
                //获取启动路径应用程序快捷方式的路径集合
                List<string> shortcutPaths = GetQuickFromFolder(systemStartPath, appAllPath);
                //存在2个以快捷方式则保留一个快捷方式-避免重复多于
                if (shortcutPaths.Count >= 2)
                {
                    for (int i = 1; i < shortcutPaths.Count; i++)
                    {
                        DeleteFile(shortcutPaths[i]);
                    }
                }
                else if (shortcutPaths.Count < 1)//不存在则创建快捷方式
                {
                    CreateShortcut(systemStartPath, QuickName, appAllPath, "中吉售货机");
                }
            }
            else//开机不启动
            {
                //获取启动路径应用程序快捷方式的路径集合
                List<string> shortcutPaths = GetQuickFromFolder(systemStartPath, appAllPath);
                //存在快捷方式则遍历全部删除
                if (shortcutPaths.Count > 0)
                {
                    for (int i = 0; i < shortcutPaths.Count; i++)
                    {
                        DeleteFile(shortcutPaths[i]);
                    }
                }
            }
            //创建桌面快捷方式-如果需要可以取消注释
            //CreateDesktopQuick(desktopPath, QuickName, appAllPath);
        }

        /// <summary>
        /// 检查自启动是否开启
        /// </summary>
        /// <returns></returns>
        public bool CheckOpen()
        {
            List<string> shortcutPaths = GetQuickFromFolder(systemStartPath, appAllPath);
            if (shortcutPaths.Count > 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        ///  向目标路径创建指定文件的快捷方式
        /// </summary>
        /// <param name="directory">目标目录</param>
        /// <param name="shortcutName">快捷方式名字</param>
        /// <param name="targetPath">文件完全路径</param>
        /// <param name="description">描述</param>
        /// <param name="iconLocation">图标地址</param>
        /// <returns>成功或失败</returns>
        private bool CreateShortcut(string directory, string shortcutName, string targetPath, string description = null, string iconLocation = null)
        {
            try
            {
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);                         //目录不存在则创建
                //添加引用 Com 中搜索 Windows Script Host Object Model
                string shortcutPath = System.IO.Path.Combine(directory, string.Format("{0}.lnk", shortcutName));//合成路径
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);  //创建快捷方式对象
                shortcut.TargetPath = targetPath;                                                               //指定目标路径
                shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(targetPath);                        //设置起始位置
                shortcut.WindowStyle = 1;                                                                       //设置运行方式，默认为常规窗口
                shortcut.Description = description;                                                             //设置备注
                shortcut.IconLocation = string.IsNullOrWhiteSpace(iconLocation) ? targetPath : iconLocation;    //设置图标路径
                shortcut.Save();                                                                                //保存快捷方式
                return true;
            }
            catch (Exception ex)
            {
                string temp = ex.Message;
                temp = "";
            }
            return false;
        }

        /// <summary>
        /// 获取指定文件夹下指定应用程序的快捷方式路径集合
        /// </summary>
        /// <param name="directory">文件夹</param>
        /// <param name="targetPath">目标应用程序路径</param>
        /// <returns>目标应用程序的快捷方式</returns>
        private List<string> GetQuickFromFolder(string directory, string targetPath)
        {
            List<string> tempStrs = new List<string>();
            tempStrs.Clear();
            string tempStr = null;
            string[] files = Directory.GetFiles(directory, "*.lnk");
            if (files == null || files.Length < 1)
            {
                return tempStrs;
            }
            for (int i = 0; i < files.Length; i++)
            {
                //files[i] = string.Format("{0}\\{1}", directory, files[i]);
                tempStr = GetAppPathFromQuick(files[i]);
                if (tempStr == targetPath)
                {
                    tempStrs.Add(files[i]);
                }
            }
            return tempStrs;
        }

        /// <summary>
        /// 获取快捷方式的目标文件路径-用于判断是否已经开启了自动启动
        /// </summary>
        /// <param name="shortcutPath"></param>
        /// <returns></returns>
        private string GetAppPathFromQuick(string shortcutPath)
        {
            //快捷方式文件的路径 = @"d:\Test.lnk";
            if (System.IO.File.Exists(shortcutPath))
            {
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                IWshRuntimeLibrary.IWshShortcut shortct = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);
                //快捷方式文件指向的路径.Text = 当前快捷方式文件IWshShortcut类.TargetPath;
                //快捷方式文件指向的目标目录.Text = 当前快捷方式文件IWshShortcut类.WorkingDirectory;
                return shortct.TargetPath;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// 根据路径删除文件-用于取消自启时从计算机自启目录删除程序的快捷方式
        /// </summary>
        /// <param name="path">路径</param>
        private void DeleteFile(string path)
        {
            FileAttributes attr = System.IO.File.GetAttributes(path);
            if (attr == FileAttributes.Directory)
            {
                Directory.Delete(path, true);
            }
            else
            {
                System.IO.File.Delete(path);
            }
        }

        /// <summary>
        /// 在桌面上创建快捷方式-如果需要可以调用
        /// </summary>
        /// <param name="desktopPath">桌面地址</param>
        /// <param name="appPath">应用路径</param>
        public void CreateDesktopQuick(string desktopPath = "", string quickName = "", string appPath = "")
        {
            List<string> shortcutPaths = GetQuickFromFolder(desktopPath, appPath);
            //如果没有则创建
            if (shortcutPaths.Count < 1)
            {
                CreateShortcut(desktopPath, quickName, appPath, "软件描述");
            }
        }

    }
}

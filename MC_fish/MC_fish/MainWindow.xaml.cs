using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace MC_fish
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 刷新钓鱼状态
        /// </summary>
        private static DispatcherTimer fishTimer;
        /// <summary>
        /// 钓鱼坐标 X
        /// </summary>
        private int fishX = 0;
        /// <summary>
        /// 钓鱼坐标 Y
        /// </summary>
        private int fishY = 0;
        private bool fishBusy = false;
        private bool fishTimerBusy = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 使界面上半部分白色都可以拖动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextInfo_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        /// <summary>
        /// 数据初始化、装钩子
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainFish_Loaded(object sender, RoutedEventArgs e)
        {
            fishTimer = new DispatcherTimer();
            fishTimer.Tick += new EventHandler(Try2Fish);
            fishTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            //设置坐标初始值
            fishX = ((int)System.Windows.SystemParameters.PrimaryScreenWidth) / 2 - 7;
            fishY = ((int)System.Windows.SystemParameters.PrimaryScreenHeight) / 2 - 7;

            Hook_Start();
        }

        /// <summary>
        /// 卸载钩子
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainFish_Unloaded(object sender, RoutedEventArgs e)
        {
            //UnregisterHotKey(IntPtr.Zero, 19);
            Hook_Clear();
        }

        private void Try2Fish(object sender, EventArgs e)
        {
            if (fishBusy == false)
            {
                UpdateColor();
            }
        }

        //https://blog.csdn.net/csndcsndwei/article/details/7533044
        /// <summary>
        /// 获取指定窗口的设备场景
        /// </summary>
        /// <param name="hwnd">将获取其设备场景的窗口的句柄。若为0，则要获取整个屏幕的DC</param>
        /// <returns>指定窗口的设备场景句柄，出错则为0</returns>
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        /// <summary>
        /// 释放由调用GetDC函数获取的指定设备场景
        /// </summary>
        /// <param name="hwnd">要释放的设备场景相关的窗口句柄</param>
        /// <param name="hdc">要释放的设备场景句柄</param>
        /// <returns>执行成功为1，否则为0</returns>
        [DllImport("user32.dll")]
        private static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        /// <summary>
        /// 在指定的设备场景中取得一个像素的RGB值
        /// </summary>
        /// <param name="hdc">一个设备场景的句柄</param>
        /// <param name="nXPos">逻辑坐标中要检查的横坐标</param>
        /// <param name="nYPos">逻辑坐标中要检查的纵坐标</param>
        /// <returns>指定点的颜色</returns>
        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        //https://www.cnblogs.com/soundcode/p/11081475.html
        private readonly int MOUSEEVENTF_RIGHTDOWN = 0x0008; //模拟鼠标右键按下 
        private readonly int MOUSEEVENTF_RIGHTUP = 0x0010; //模拟鼠标右键抬起 
        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        IntPtr hdc;
        private void UpdateColor()
        {
            hdc = GetDC(IntPtr.Zero); uint pixel = GetPixel(hdc, fishX, fishY);
            ReleaseDC(IntPtr.Zero, hdc);
            this.colorInfo.Fill = new SolidColorBrush(Color.FromRgb((byte)(pixel & 0x000000FF),
                (byte)((pixel & 0x0000FF00) >> 8), (byte)((pixel & 0x00FF0000) >> 16)));

            if ((pixel & 0x000000FF) != 0)
            {
                fishBusy = true;
                mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                Thread.Sleep(400);
                mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                Thread.Sleep(2000);
                fishBusy = false;
            }

            if (fishTimerBusy)
            {
                fishTimer.Stop();
            }
        }

        //委托 
        public delegate int HookProc(int nCode, int wParam, IntPtr lParam);
        static int hHook = 0;
        private const int WH_KEYBOARD_LL = 13;
        //LowLevel键盘截获，如果是WH_KEYBOARD＝2，并不能对系统键盘截取，Acrobat Reader会在你截取之前获得键盘。 
        HookProc KeyBoardHookProcedure;
        //键盘Hook结构函数 
        [StructLayout(LayoutKind.Sequential)]
        public class KeyBoardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        #region DllImport
        //设置钩子 
        [DllImport("user32.dll")]
        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        //抽掉钩子 
        public static extern bool UnhookWindowsHookEx(int idHook);
        [DllImport("user32.dll")]
        //调用下一个钩子 
        public static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string name);

        #endregion

        #region 自定义事件
        private void Hook_Start()
        {
            // 安装键盘钩子 
            if (hHook == 0)
            {
                KeyBoardHookProcedure = new HookProc(KeyBoardHookProc);

                //hHook = SetWindowsHookEx(2,
                // KeyBoardHookProcedure,
                // GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), GetCurrentThreadId());

                hHook = SetWindowsHookEx(WH_KEYBOARD_LL,
                KeyBoardHookProcedure,
                GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
                //如果设置钩子失败. 
                if (hHook == 0)
                {
                    Hook_Clear();
                    //throw new Exception("设置Hook失败!");
                    System.Windows.MessageBox.Show("hook set default");
                }
            }
        }

        //取消钩子事件
        private void Hook_Clear()
        {
            bool retKeyboard = true;
            if (hHook != 0)
            {
                retKeyboard = UnhookWindowsHookEx(hHook);
                hHook = 0;
            }
            //如果去掉钩子失败. 
            if (!retKeyboard) System.Windows.MessageBox.Show("UnhookWindowsHookEx failed.");
        }

        /// <summary>
        /// 钓鱼开关
        /// </summary>
        private bool ifOpenTimer = false;
        /// <summary>
        /// 去重
        /// </summary>
        private bool doubleAimKeyEvent = false;

        /// <summary>
        /// 钩子处理
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private int KeyBoardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KeyBoardHookStruct kbh = (KeyBoardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyBoardHookStruct));

                //钓鱼 ctrl t
                if (kbh.vkCode == (int)Keys.T
                && (int)System.Windows.Forms.Control.ModifierKeys == (int)Keys.Control)
                {
                    if (doubleAimKeyEvent)
                    {
                        doubleAimKeyEvent = false;
                    }
                    else
                    {
                        if (ifOpenTimer)
                        {
                            FishStop();
                            fishTimerBusy = true;
                            //ShowTimer.Stop();
                            ifOpenTimer = false;
                        }
                        else
                        {
                            FishStart();
                            fishTimerBusy = false;
                            fishTimer.Start();
                            ifOpenTimer = true;
                        }
                        doubleAimKeyEvent = true;
                    }

                    return 1;
                }

                //重置焦点 ctrl y
                if (kbh.vkCode == (int)Keys.Y
                && (int)System.Windows.Forms.Control.ModifierKeys == (int)Keys.Control)
                {
                    if (doubleAimKeyEvent)
                    {
                        doubleAimKeyEvent = false;
                    }
                    if (ifOpenTimer)
                    {
                        ifOpenTimer = false;
                    }

                    //添加了一个引用集队
                    fishX = System.Windows.Forms.Control.MousePosition.X;
                    fishY = System.Windows.Forms.Control.MousePosition.Y;
                    TextInfo.Text = fishX.ToString() + ", " + fishY.ToString();
                    fishTimerBusy = true;
                    //ShowTimer.Stop();

                    hdc = GetDC(IntPtr.Zero); uint pixel = GetPixel(hdc, fishX, fishY);
                    ReleaseDC(IntPtr.Zero, hdc);
                    this.colorInfo.Fill = new SolidColorBrush(Color.FromRgb((byte)(pixel & 0x000000FF), (byte)((pixel & 0x0000FF00) >> 8), (byte)((pixel & 0x00FF0000) >> 16)));

                    return 1;
                }
            }
            return CallNextHookEx(hHook, nCode, wParam, lParam);
        }

        private void FishStop()
        {
            TextInfo.Text = "stop fish";
        }
        private void FishStart()
        {
            TextInfo.Text = "start fish";
        }

        #endregion
    }
}

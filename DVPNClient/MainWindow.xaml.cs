using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Hardcodet.Wpf.TaskbarNotification;
using DowngradVPN.MVVM.View;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System;


namespace DowngradVPN
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TaskbarIcon tb;
        private static Mutex _mutex = null;
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        
        public MainWindow()
        {
        const string appName = "DowngradVPNApp";
        _mutex = new Mutex(true, appName, out bool createdNew);

        if (!createdNew)
            {
                // Application already running, bring it to the foreground
                Process currentProcess = Process.GetCurrentProcess();
                foreach (var process in Process.GetProcessesByName(currentProcess.ProcessName))
                {
                    if (process.Id != currentProcess.Id)
                    {
                        SetForegroundWindow(process.MainWindowHandle);
                        break;
                    }
                }
                Application.Current.Shutdown();
                return;
            }

            InitializeComponent();

            tb = new TaskbarIcon();
            tb.Icon = new System.Drawing.Icon("Images/icon.ico");
            tb.ToolTipText = "DowngradVPN";
            tb.ContextMenu = new System.Windows.Controls.ContextMenu();

            var openMenuItem = new System.Windows.Controls.MenuItem { Header = "Open" };
            openMenuItem.Click += OpenMenuItem_Click;
            tb.ContextMenu.Items.Add(openMenuItem);

            var exitMenuItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
            exitMenuItem.Click += ExitMenuItem_Click;
            tb.ContextMenu.Items.Add(exitMenuItem);

            this.StateChanged += MainWindow_StateChanged;
            this.Closing += MainWindow_Closing;
        }
        

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();

        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //homeView.StopVpn();
            var processManager = ProcessManager.Instance;
            processManager.KillProcess();
            tb.Dispose();

            Application.Current.Shutdown();
            Console.WriteLine("EIXXXXKJKLDDFKJDKFKJSDFJKL");
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (WindowState == WindowState.Normal)
                {
                    WindowState = WindowState.Maximized;
                }
                else
                {
                    WindowState = WindowState.Normal;
                }
            }
            else
            {
                DragMove();
            }
        }




    }
}
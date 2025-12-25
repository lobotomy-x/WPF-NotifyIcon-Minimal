using System.Runtime.InteropServices;
using System.Windows;


namespace WpfApp1
{
    public partial class TrayContextMenu : Window
    {
        private Window _window;
        private double screenRight;
        private double screenBottom;
        private bool offsetInwards;

        // Must be initialized with the associated window (i.e. your main window, not the message handler)
        // if offsetInwards is true (default) then the window will originate with the notify icon at its bottom right corner
        // otherwise only the minimal offset needed to keep the entire window in screen bounds will be applied
        public TrayContextMenu(Window owner, bool _offsetInwards = true)
        {
            InitializeComponent();
            _window = owner;

            offsetInwards = _offsetInwards;
            this.Loaded += MainWindow_Loaded;
            ShowAtMouse();
        }

        // I had difficulty getting the cursor position from the notify icon message handler
        // It should be easy to get from the lparam but for whatever reason it gave me junk data
        // So instead we just spawn this window and detect the mouse position 
        // This does break when stepping through with a debugger but its otherwise no problem
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point { public int X; public int Y; }


        private void ShowAtMouse()
        {
            Win32Point mousePos = new Win32Point();
            GetCursorPos(ref mousePos);

            // this section is vibe coded because wtf was wrong with microsoft when they came up with wpf

            // Get DPI scaling factors for the current display
            var source = PresentationSource.FromVisual(this);
            double dpiX = source?.CompositionTarget.TransformToDevice.M11 ?? 1.0;
            double dpiY = source?.CompositionTarget.TransformToDevice.M22 ?? 1.0;

            // Ensure the window's size is calculated before positioning
            if (double.IsNaN(this.Width)) this.UpdateLayout();

            // Position the bottom-right corner at the mouse cursor
            // (Divide mousePos by DPI to convert pixels to WPF DIPs
            screenRight = SystemParameters.WorkArea.Width;
            screenBottom = SystemParameters.WorkArea.Height;     
            this.Left = Math.Min(screenRight, (mousePos.X / dpiX) - this.DesiredSize.Width);
            this.Top = Math.Min(screenBottom, (mousePos.Y/ dpiY) - this.DesiredSize.Height);

            this.Show();
            // bring to front and focus to allow pressing tab and then using keyboard controls
            this.Activate();
            this.Focus();
        }

        // this will occur immediately after Show() is called to offset the window
        // with this the mouse position and notifyicon will be at the bottom right corner
        // width, actualwidth, and desiredsize.width all return 0 before the window is drawn
        // its honestly baffling because what's the point of using a retained UI framework
        // if we can't predetermine layout? 
        // Anyway this will work well if you have standard taskbar layout
        // for default win11 you have no choice anyway but if someone is using a vertical taskbar with explorer patcher
        // or windows 10 then it could potentially go out of screen bounds
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
          
            if (offsetInwards)
            {
                // Offset the window up by its height
                this.Top -= this.ActualHeight;

                // Offset the window left by its width
                this.Left -= this.ActualWidth;
            }
            else
            {
                var Right = this.Left + this.ActualWidth;
                var Bottom = this.Top + this.ActualHeight;
                var leftOffset = Right - screenRight;
                var topOffset = Bottom - screenBottom;
                if (Right > screenRight)
                {
                    this.Left -= (leftOffset);
                }
                if (Bottom > screenBottom)
                {
                    this.Top -= (topOffset);
                }
            }
         
        }
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.CloseWindow(_window);
        }

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            WindowManager.RestoreWindow(_window);
        }

        // Mimic popup behavior from a wpf Window
        // This triggers any time you click outside of the window
        // Better just to hide this window and let someone else handle closing it
        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Hide();       
        }
    }
}

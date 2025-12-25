using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;
        private bool canDragWindow = false;

        public MainWindow()
        {
            this.InitializeComponent();
            // stored as Icon 
            // this is a Windows Forms class but doesnt actually require winforms
            var mainIcon = WpfApp1.Properties.Resources.mainIcon;
            notifyIcon = new NotifyIcon();
            // Converted from png
            var altIcon = WpfApp1.Properties.Resources.Icon2;
            notifyIcon.RegisterAlternateIcon("alt", altIcon.GetHicon());
            notifyIcon.InitializeTrayIcon(this, mainIcon.Handle, "WPF Notify Example");
            this.Closed += MainWindow_Closed;
        }
        
        // Automatically called when app is closed
        // Since that is the only way to actually close the window its fine
        // But if you wanted to fully close the window instead of just hiding it
        // You would need to setup some other way to ensure the notifyIcon is removed
        // Since the notifyIcon is not actually owned by this app and is instead just a handle
        // to a winapi object owned by the explorer instance that draws the taskbar
        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            notifyIcon.RemoveTrayIcon();
        }
       
        // We are not using the default window minimize, maximize, close buttons
        // You could do so and handle those events but that is out of scope
        private void MainWindow_Minimized(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

       private void MainWindow_ClosedToTray(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
            this.ShowInTaskbar = false;
        }

        private void MainWindow_LostFocus(object sender, RoutedEventArgs e)
        {
               canDragWindow = false;
        }

        private void MainWindow_GotMouseCapture(object sender, MouseEventArgs e)
        {
            canDragWindow = true;
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (canDragWindow)
            {
                try
                {
                    this.DragMove();
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            canDragWindow = true;
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            var curIcon = notifyIcon.GetIconType();
            
            notifyIcon.ModifyTrayIcon(curIcon == "default" ? "alt" : "default");
            TestButton.Content = notifyIcon.GetIconType();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace WpfApp1
{
    internal static class WindowManager
    {
        // This is what we call when single clicking the notify icon
        public static void ActivateWindow(Window window)
        {
            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }
            window.Activate();
            window.Focus();
        }

        // This is what we call when double clicking the notify icon
        // or clicking a context menu item to restore the window
        public static void RestoreWindow(Window window)
        {
            if (window.Visibility == Visibility.Visible)
            {
                ActivateWindow(window);
            }
            else
            {
                window.Visibility = Visibility.Visible;
                window.ShowInTaskbar = true;
            }
        }

        // This is the same as the private event handler for clicking the close button in the window class
        // I'm not using it here but it makes sense to make available
        // This will not actually close the window or kill the instance of it so any logic can keep running
        // Actually even if you close the window it won't stop all app logic
        //  but this way even if the logic is tied to xaml binding events you should be fine
        public static void HideWindow(Window window)
        {
            window.Visibility = Visibility.Hidden;
            window.ShowInTaskbar = false;
        }

        // make sure any necessary cleanup actions like removing the notify icon
        // are handled in the OnClose event for the window 
        public static void CloseWindow(Window window)
        {
            window.Close();
            Application.Current.Shutdown();
        }

    }
}

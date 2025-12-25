using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WpfApp1
{
    internal class NotifyIcon
    {

        #region native
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(NotifyIconMessage dwMessage, ref NotifyIconData lpData);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadImage(IntPtr hInstance, string lpName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        private const int WM_USER = 0x0400;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_CONTEXTMENU = 0x007B;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_TRAYICON = WM_USER + 1;
        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x00000010;
        #endregion


        private Window? _window;
        private NotifyIconData _notifyIconData;
        private static TrayContextMenu? ctxMenu;


        private string defaultTip = "";
        private IntPtr defaultIcon;

    /*
        I'm leaving this commented if anyone wants to use my old implementation
        Even with just a few icons I think a dictionary is just more flexible
        and I want this NotifyIcon class to be fully drop in ready so only the mainwindow needs to be edited

        // If you have several icons maybe you want to put them in a collection
        // I only needed to cycle between 3 icons so this approach was adequate
        private IconType _iconType;
        
        private IntPtr alternateIcon;
        public enum IconType
        {
            Default = 0,
            Variant1 = 1
        }*/

        public Dictionary<string, IntPtr> IconDictionary = new Dictionary<string, IntPtr>();
        

        // Message-only window for receiving tray icon callbacks
        private HwndSource? _messageWindow;
        private HwndSourceHook? _messageHook;
        private IntPtr _hWnd;


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NotifyIconData
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public NotifyIconFlags uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
        }


        [Flags]
        private enum NotifyIconFlags
        {
            Message = 0x01,
            Icon = 0x02,
            Tip = 0x04
        }

        private enum NotifyIconMessage
        {
            Add = 0x00,
            Modify = 0x01,
            Delete = 0x02
        }

        // Register additional icons that can be switched to by name
        // The main use for this is to mimic behaviors like status badges 
        public void RegisterAlternateIcon(string name, IntPtr icon, bool update = true)
        {
            if (IconDictionary.ContainsKey(name))
            {
                if (update)
                {
                    Debug.WriteLine($"Key {name} already exists. Removing old value");
                    IconDictionary.Remove(name);
                }
                else
                {
                    Debug.WriteLine($"Key {name} already exists. Skipping entry.");
                    return;
                }
            }
            IconDictionary.Add(name, icon);
        }
    
        // why would you ever need to bulk add icons? no clue really
        public void RegisterAlternateIcons(Dictionary<string, IntPtr> icons)
        {
            IconDictionary.Concat(icons);
        }

        // idk why you would need this but you never know
        public void RemoveIconFromDictionary(string name)
        {
            if (IconDictionary.ContainsKey(name))
            {
                IconDictionary.Remove(name);
            }
        }
        // Icons must be passed to this class as handles. You can add Icon resources and just pass the .Handle
        // Or you can use an ImageSourceConverter or native method to load from png files or streams
        // Both methods are shown in the example
        public void InitializeTrayIcon(Window window, IntPtr icon = 0, string tip = "")
        {
            // we could also get the MainWindow instance through Application.Current
            // but its convenient to just hold a handle to it
            // And for more complex apps maybe you want to associate a specific window with a notify icon instance
            // I've gone ahead and made it generalized by separating the necessary methods into a static class
            _window = window;

            defaultTip = string.IsNullOrEmpty(tip) ? Application.Current.MainWindow.Title : tip;
            if (icon != IntPtr.Zero)
            {
                defaultIcon = icon;
            }
            else
            {
                var hIcon = System.Drawing.Icon.ExtractAssociatedIcon(Application.Current.StartupUri.AbsolutePath);
                if (hIcon is not null)
                    defaultIcon = hIcon.Handle;
            }
            IconDictionary.Add("default", defaultIcon);
            var parameters = new HwndSourceParameters("TrayIconMessageWindow")
            {
                PositionX = 0,
                PositionY = 0,
                Height = 0,
                Width = 0,
                ParentWindow = new IntPtr(-3), // HWND_MESSAGE
                WindowStyle = 0
            };

            _messageHook = new HwndSourceHook(WndProc);
            _messageWindow = new HwndSource(parameters);
            _messageWindow.AddHook(_messageHook);
            _hWnd = _messageWindow.Handle;




            // Populate the NotifyIconData structure
            _notifyIconData = new NotifyIconData
            {
                cbSize = Marshal.SizeOf(typeof(NotifyIconData)),
                hWnd = _hWnd,
                uID = 1,
                uFlags = NotifyIconFlags.Message | NotifyIconFlags.Icon | NotifyIconFlags.Tip,
                uCallbackMessage = WM_TRAYICON,
                hIcon = defaultIcon,
                szTip = defaultTip

            };
            // Add the icon to the system tray
            bool result = Shell_NotifyIcon(NotifyIconMessage.Add, ref _notifyIconData);
            if (!result)
            {
                System.Diagnostics.Debug.WriteLine("Failed to add tray icon.");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (_window is not null)
            {
                // Use a 64-bit safe conversion for lParam
                int lParamInt = unchecked((int)lParam.ToInt64());

                // On single click we activate and focus the window 
                // This also will restore the window if it is minimized but not closed
                // i.e. the normal taskbar icon is still displayed
                // This mimics behavior of popular apps like Steam and EGS
                if (lParamInt == WM_LBUTTONUP)
                {
                    WindowManager.ActivateWindow(_window);
                    handled = true;
                }
                // On double click we restore the window if its been closed
                // If its not closed this will call ActivateMainWindow
                else if (lParamInt == WM_LBUTTONDBLCLK)
                {
                    WindowManager.RestoreWindow(_window);
                    handled = true;
                }
                // WM_CONTEXTMENU doesn't actually have anything to do with context menus,
                // it covers right click and enter if the system tray has keyboard focus
                else if (lParamInt == WM_RBUTTONUP || lParamInt == WM_CONTEXTMENU)
                {
                    // ensure we only have one instance of the context menu window
                    if (ctxMenu is not null) ctxMenu.Close();
                    // pass the window handle to the context menu
                    ctxMenu = new TrayContextMenu(_window);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }
        public void RemoveTrayIcon()
        {
            // Remove the icon from the system tray
            Shell_NotifyIcon(NotifyIconMessage.Delete, ref _notifyIconData);

            if (_messageWindow is not null)
            {
                if (_messageHook is not null)
                {
                    _messageWindow.RemoveHook(_messageHook);
                    _messageHook = null;
                }
                if (ctxMenu is not null) ctxMenu.Close();
                _messageWindow.Dispose();
                _messageWindow = null;
            }
        }

        // Call without args to reset to default
        public void ModifyToolTip(string? message = null)
        {
            _notifyIconData.szTip = message is not null ? message : defaultTip;
            Shell_NotifyIcon(NotifyIconMessage.Modify, ref _notifyIconData);
        }

        // I would probably track last key from the calling window but just in case we can do this
        public string GetIconType()
        {
            var icon = _notifyIconData.hIcon;
            if (icon == defaultIcon) 
                return "default";
            if (IconDictionary.Count != 0)
            {
                foreach (var key in IconDictionary.Keys)
                {
                    if (IconDictionary.TryGetValue(key, out IntPtr value))
                    {
                        if (value == icon)
                        {
                            return key;
                        }
                    }
                }
            }
            return "";
        }

        // icon should be registered in Dictionary ahead of time
        public void ModifyTrayIcon(string iconKey)
        {
            if (IconDictionary.TryGetValue(iconKey, out IntPtr icon))
            {
                _notifyIconData.hIcon = icon;
                Shell_NotifyIcon(NotifyIconMessage.Modify, ref _notifyIconData);
            }
        }

        public void ResetTrayIcon()
        {
            _notifyIconData.hIcon = defaultIcon;
            Shell_NotifyIcon(NotifyIconMessage.Modify, ref _notifyIconData);
        }

        // If you want to manage the icons from another class just pass the handle here
        public void ModifyTrayIcon(IntPtr icon)
        {
            _notifyIconData.hIcon = icon;
            Shell_NotifyIcon(NotifyIconMessage.Modify, ref _notifyIconData);
        }

        // I personally prefer having this class nonstatic and let it hold all the data instead of tying it up in a window class
        // but you could very easily make this whole thing static 
        //public static void ModifyTrayIcon(IntPtr icon, NotifyIconData _notifyIconData)
        //{
        //    _notifyIconData.hIcon = icon;
        //    Shell_NotifyIcon(NotifyIconMessage.Modify, ref _notifyIconData);
        //}

        // enum version
        /*        // Let this class handle tracking its own icon state so we don't have to pass the notifyIconData ref around
                public IconType GetIconType()
                {
                    return _iconType;
                }*/

        /*        public void ModifyTrayIcon(IconType iconType, string? message = null)
                {
                    _iconType = iconType;
                    // optionally update tip
                    if (message is not null)
                    {
                        _notifyIconData.szTip = message;
                    }
                    // Optionally reset if no message is passed
                    // It may be preferable to do this based on icontype as shown
                    //else if (_notifyIconData.szTip != defaultTip)
                    //{
                    //    _notifyIconData.szTip = defaultTip; 
                    //}
                    switch (iconType)
                    {
                        case IconType.Default:
                            _notifyIconData.hIcon = defaultIcon;
                            _notifyIconData.szTip = defaultTip;
                            break;
                        case IconType.Variant1:
                            _notifyIconData.hIcon = alternateIcon;
                            break;
                    }
                    Shell_NotifyIcon(NotifyIconMessage.Modify, ref _notifyIconData);
                }*/
    }
}

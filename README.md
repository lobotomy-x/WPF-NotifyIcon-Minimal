# WPF-NotifyIcon-Minimal
WinAPI implementation of a notify icon (system tray) + context menu for those who don't want to use a huge nuget package or windows forms. Easy to drop in existing WPF apps or even WinUI3 with minimal work needed.

I am well aware that there is a nuget package available but it leans too heavily into dotnet design patterns with far too many files and overengineering simple problems with services, delegates, and helpers. There is also the version in Windows Forms but that was never even a consideration for me. Actually even WPF is not my first choice but since I am proposing changes to someone else's project I felt the only reasonable option was to write my own implementation with WinAPI bindings. This turned out to be much more of a hassle than I expected but I'm quite happy with the result. 



In this repo you will find a complete basic WPF application that shows off the available features. The app demonstrates standard system tray behaviors inspired by apps like Steam and shows examples of loading .ico and .png files from project resources. The NotifyIcon class contains a string-keyed dictionary and supports updating notify icon tooltips and icons on the fly allowing for custom implementations of a status update or badge feature.    



https://github.com/user-attachments/assets/4ad58a17-78a0-4eab-abba-28135f523d21

I am providing the example project alongside the notify icon module as a separate class library, but you can also easily just copy the NotifyIcon, WindowManager, and TrayContextMenu files (4 small files in total) into your project. Add the following to your MainWindow and resolve any namespace issues and you should have the minimal function ready to go. 

```C#
        // MainWindow.xaml.cs
        private NotifyIcon notifyIcon;
        private bool canDragWindow = false;
        public MainWindow()
        {
            this.InitializeComponent();
            var mainIcon = YourProjectName.Properties.Resources.Icon;
            notifyIcon = new NotifyIcon();
            notifyIcon.InitializeTrayIcon(this, mainIcon.Handle, "YOUR TOOLTIP");
            this.Closed += MainWindow_Closed;
        }
        
        // Automatically called when app is closed
        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            notifyIcon.RemoveTrayIcon();
        }
       
        // We are not using the default window minimize, maximize, close buttons
        // So this is just the binding for a custom minimize button
        private void MinimizeButton_Clicked(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

       private void CloseButton_Clicked(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
            // Hide the regular taskbar entry 
            this.ShowInTaskbar = false;
        }

```

You can register and use additional icons like so.
```C#
    
 public MainWindow()
  {
  // Converted from png
 var altIcon = WpfApp1.Properties.Resources.Icon2;
 notifyIcon.RegisterAlternateIcon("alt", altIcon.GetHicon());
  }

  private void TestButton_Click(object sender, RoutedEventArgs e)
  {
      var curIcon = notifyIcon.GetIconType();
      
      notifyIcon.ModifyTrayIcon(curIcon == "default" ? "alt" : "default");
      TestButton.Content = notifyIcon.GetIconType();
  }
```
The NotifyIcon instance holds all the relevant data in this case but it could be modified to be static. The WindowManager class could also be eliminated if you have no interest in reusability, just move the needed functions to another class or write the actions inline in your context menu. However it is a fully static class that has static versions of all the logic used in the context menu as well as functions to dynamically create new instances of Window subclasses meaning you can easily build your own context menus or setup bindings with your existing controls. I have partially setup a framework to handle this painlessly (for you, not so much for me) but it ate up too much time and my hopes that copilot could do something on its own for once proved to be misplaced. I don't know if I'll finish it. As of now you have a largely complete template and I would recommend simply building off of the supplied TrayContextMenu window or even just copying the xaml.cs code into your own controls.  

I will also add that while I tried to vibe code this to "save time" very little of it actually worked for the core logic and mostly it sent me going around in circles until I got serious and just wrote the important parts myself. However AI has been quite nice for fixing weird C# and XAML bugs.

With that in mind there are no particular credits as basically no singular source out there had the info I needed. I honestly have no idea if this will get any usage but hopefully it does help anyone struggling with the same issues and similarly weary of the convoluted MVVM crap you get funneled into if you use existing options. Otherwise if nothing else I needed this to finish up a large set of changes to the UEVR Frontend. And well I don't particularly want to do C# for a living and could probably have done this in a quarter of the time in C++, but this was a pretty good and useful project for a couple days of light work in a language I barely use so I guess its a resume piece. 

using TaskbarTray.Views;


namespace TaskbarTray;

public sealed partial class App : Application
{
    //public static Window? MainWindow { get; set; }

    public static Window? MainWindow { get; set; }

    public static bool HandleClosedEvents { get; set; } = true;


    #region notify boiler plate


    public App()
    {
        InitializeComponent();
    }


    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window
        {
            Content = new Frame
            {
                Content = new MainView(),
            },
        };

        MainWindow.Closed += (sender, args) =>
        {
            if (HandleClosedEvents)
            {
                args.Handled = true;
                MainWindow.Hide();
            }
        };

        MainWindow.Hide();// Hide by default at startup, as this is a tray app
    }

    #endregion


    #region Use the MainWindow component in project (ass oppose to code created one above) 

    //// private Window? _window;

    // /// <summary>
    // /// Initializes the singleton application object.  This is the first line of authored code
    // /// executed, and as such is the logical equivalent of main() or WinMain().
    // /// </summary>
    // public App()
    // {
    //     InitializeComponent();
    // }

    // /// <summary>
    // /// Invoked when the application is launched.
    // /// </summary>
    // /// <param name="args">Details about the launch request and process.</param>
    // protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    // {
    //     MainWindow = new MainWindow();
    //     MainWindow.Activate();
    // }

    #endregion

}

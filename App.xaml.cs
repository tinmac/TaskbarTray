using TaskbarTray.Views;


namespace TaskbarTray;

public sealed partial class App : Application
{

    public static Window? MainWindow { get; set; }

    public static bool HandleClosedEvents { get; set; } = true;



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


}

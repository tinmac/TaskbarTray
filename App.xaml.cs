using CommunityToolkit.Mvvm.Messaging;
using TaskbarTray.Views;
using WinUIEx;


namespace TaskbarTray;

public sealed partial class App : Application
{

    public static Window? MainWindow { get; set; }

    public static bool HandleClosedEvents { get; set; } = true;


    //public static WeakReferenceMessenger Messenger { get; } = new WeakReferenceMessenger();

    public App()
    {
        InitializeComponent();
    }


    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new WindowEx
        {
            Width = 400,
            Height = 300,
            Content = new Frame
            {
                Content = new MainView(),
            },
        };       

        MainWindow.Closed += (sender, args) =>
        {
            //Messenger.Send(new Msg_CloseMainWin { CloseMainWin = true });
            WeakReferenceMessenger.Default.Send(new MyMessage("Hello from Messenger!"));
         
            if (HandleClosedEvents)
            {
                args.Handled = true;
                MainWindow.Hide();
            }
        };

        MainWindow.Hide();// Hide by default at startup, as this is a tray app
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        throw new System.NotImplementedException();
    }
}

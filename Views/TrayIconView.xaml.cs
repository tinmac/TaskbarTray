using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;



namespace TaskbarTray.Views;


[ObservableObject]
public sealed partial class TrayIconView : UserControl
{

    private bool _isWindowVisible;
    public bool IsWindowVisible
    {
        get => _isWindowVisible;
        set => SetProperty(ref _isWindowVisible, value);
    }

    private bool _isOptionOneChecked;
    public bool IsOptionOneChecked
    {
        get => _isOptionOneChecked;
        set => SetProperty(ref _isOptionOneChecked, value);
    }

    private bool _isOptionTwoChecked;
    public bool IsOptionTwoChecked
    {
        get => _isOptionTwoChecked;
        set => SetProperty(ref _isOptionTwoChecked, value);
    }

    private bool _isOptionThreeChecked;
    public bool IsOptionThreeChecked
    {
        get => _isOptionThreeChecked;
        set => SetProperty(ref _isOptionThreeChecked, value);
    }


    public TrayIconView()
    {
        InitializeComponent();

        MyMenuFlyout.ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway;

    }


    [RelayCommand]
    public void OptionOne()
    {
        DisplayValues();
    }


    [RelayCommand]
    public void OptionTwo()
    {
        DisplayValues();
    }


    [RelayCommand]
    public void OptionThree()
    {
        DisplayValues();
    }


    [RelayCommand]
    public void ShowHideWindow()
    {
        // If we try to show the context flyout on left click,
        // it does not work
        //
        bool LeftClickOpensContextFlyout = false;

        if (LeftClickOpensContextFlyout)
        {
            Debug.WriteLine($"Left: Show Context...");
           
            MyMenuFlyout.ShowAt(TrayIcon);

        }
        else
        {
            Debug.WriteLine($"Left: Show/Hide Main...");

            var window = App.MainWindow;
            if (window == null)
            {
                return;
            }

            if (window.Visible)
            {
                window.Hide();
            }
            else
            {
                window.Show();
            }
            IsWindowVisible = window.Visible;
        }
    }



    [RelayCommand]
    public void ExitApplication()
    {
        App.HandleClosedEvents = false;
        TrayIcon.Dispose();
        App.MainWindow?.Close();
    }


    private void DisplayValues()
    {
        Debug.WriteLine($"Option  [1 {IsOptionOneChecked}]  [2 {IsOptionTwoChecked}]  [3 {IsOptionThreeChecked}]");
    }


    private void MyMenuFlyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
    {
        Debug.WriteLine($"Flyout Closing called..."); // this is not beoing called when the flyout is closed!

        args.Cancel = true; // Can we prevent the flyout from closing?
    }
}


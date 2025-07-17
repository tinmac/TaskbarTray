using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PowerSwitch.Views
{
    public sealed partial class MainView
    {
        public MainView()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // No navigation, Settings page is shown by default
        }
    }
}

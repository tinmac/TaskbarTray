using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarTray.Views
{
    public enum ImageSourceType
    {
        Icon1,
        Icon2,
        Icon3,
        Icon4
    }

    public class TrayIconVM : INotifyPropertyChanged
    {
        private ImageSourceType _selectedImageType = ImageSourceType.Icon1;

        public ImageSourceType SelectedImageType
        {
            get => _selectedImageType;
            set
            {
                if (_selectedImageType != value)
                {
                    _selectedImageType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedImage));
                    OnPropertyChanged(nameof(IsIcon1Selected));
                    OnPropertyChanged(nameof(IsIcon2Selected));
                    OnPropertyChanged(nameof(IsIcon3Selected));
                    OnPropertyChanged(nameof(IsIcon4Selected));
                }
            }
        }

        public BitmapImage SelectedImage => ConvertEnumToImage(SelectedImageType);

        public bool IsIcon1Selected
        {
            get => SelectedImageType == ImageSourceType.Icon1;
            set { if (value) SelectedImageType = ImageSourceType.Icon1; }
        }

        public bool IsIcon2Selected
        {
            get => SelectedImageType == ImageSourceType.Icon2;
            set { if (value) SelectedImageType = ImageSourceType.Icon2; }
        }

        public bool IsIcon3Selected
        {
            get => SelectedImageType == ImageSourceType.Icon3;
            set { if (value) SelectedImageType = ImageSourceType.Icon3; }
        }

        public bool IsIcon4Selected
        {
            get => SelectedImageType == ImageSourceType.Icon4;
            set { if (value) SelectedImageType = ImageSourceType.Icon4; }
        }

        private BitmapImage ConvertEnumToImage(ImageSourceType sourceType)
        {
            string uri = sourceType switch
            {
                ImageSourceType.Icon1 => "/Assets/gauge_low.ico",
                ImageSourceType.Icon2 => "/Assets/Inactive.ico",
                ImageSourceType.Icon3 => "/Assets/Red.ico",
                ImageSourceType.Icon4 => "/Assets/gauge_high.ico",
                _ => throw new ArgumentOutOfRangeException()
            };
            return new BitmapImage(new Uri(uri));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

                
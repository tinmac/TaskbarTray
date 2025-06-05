using CommunityToolkit.Mvvm.ComponentModel;
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

    public partial class TrayIconVM : ObservableObject
    {
        [ObservableProperty]
        private ImageSourceType selectedImageType = ImageSourceType.Icon1;

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

        partial void OnSelectedImageTypeChanged(ImageSourceType oldValue, ImageSourceType newValue)
        {
            OnPropertyChanged(nameof(SelectedImage));
            OnPropertyChanged(nameof(IsIcon1Selected));
            OnPropertyChanged(nameof(IsIcon2Selected));
            OnPropertyChanged(nameof(IsIcon3Selected));
            OnPropertyChanged(nameof(IsIcon4Selected));
        }

        private BitmapImage ConvertEnumToImage(ImageSourceType sourceType)
        {
            string uri = sourceType switch
            {
                ImageSourceType.Icon1 => "ms-appx:///Assets/Icon1.png",
                ImageSourceType.Icon2 => "ms-appx:///Assets/Icon2.png",
                ImageSourceType.Icon3 => "ms-appx:///Assets/Icon3.png",
                ImageSourceType.Icon4 => "ms-appx:///Assets/Icon4.png",
                _ => throw new ArgumentOutOfRangeException()
            };
            return new BitmapImage(new Uri(uri));
        }
    }
}

                
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace AudioMerge.WPF.Converters
{
    public class StringToImageConverter : IValueConverter
    {
        public string DefaultImage { get; set; }
        public string AlternateImage { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = (string)value;
            string imagePath = path == "Import Video" ? DefaultImage : AlternateImage;
            return new BitmapImage(new Uri(imagePath, UriKind.Relative));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
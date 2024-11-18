using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace AudioMerge.WPF.Converters;

public class BoolToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isEnabled = (bool)value;
        string imagePath = isEnabled ? "/Images/audio_on.png" : "/Images/audio_off.png";
        return new BitmapImage(new Uri(imagePath, UriKind.Relative));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
using System.Globalization;
using Microsoft.Maui.Controls;

namespace SrcSinavUygulamasi.Converters
{
    public class IndexToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Şimdilik standart bir renk döndürsün, hatayı kesmek için yeterli.
            // Zaten biz renkleri ViewModel'den yönetiyoruz ama XAML'da tanımlı kaldığı için bu dosya şart.
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System.Globalization;

namespace SrcSinavUygulamasi.Converters;

/// <summary>
/// İlerleme çubuğu için progress hesaplar (mevcut soru / toplam soru)
/// </summary>
public class ProgressConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is int current && values[1] is int total && total > 0)
        {
            return (double)current / total;
        }
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

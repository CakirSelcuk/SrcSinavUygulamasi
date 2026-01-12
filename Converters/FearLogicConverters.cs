using System.Globalization;

namespace SrcSinavUygulamasi.Converters
{
    /// <summary>
    /// Bool -> Kritik durum rengi (Kırmızı/Normal)
    /// Fear Logic için kullanılır
    /// </summary>
    public class CriticalStateColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isCritical = (bool)(value ?? false);
            string paramStr = parameter as string ?? "background";

            if (isCritical)
            {
                return paramStr.ToLower() switch
                {
                    "text" => Color.FromArgb("#FFFFFF"),       // Beyaz yazı
                    "border" => Color.FromArgb("#B00020"),     // Kan kırmızısı border
                    _ => Color.FromArgb("#B00020")             // Kan kırmızısı arka plan
                };
            }
            else
            {
                return paramStr.ToLower() switch
                {
                    "text" => Color.FromArgb("#1e293b"),       // Koyu yazı
                    "border" => Color.FromArgb("#22c55e"),     // Yeşil border
                    _ => Color.FromArgb("#22c55e")             // Yeşil arka plan
                };
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Double (0-100) -> Progress bar rengi
    /// </summary>
    public class PercentageToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percentage = (double)(value ?? 0);

            return percentage switch
            {
                < 50 => Color.FromArgb("#B00020"),   // Kan kırmızısı - KRİTİK
                < 70 => Color.FromArgb("#FF6B00"),   // Turuncu - RİSKLİ
                _ => Color.FromArgb("#22c55e")       // Yeşil - GÜVENLI
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// String -> Color dönüşümü (hex kod)
    /// </summary>
    public class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string colorHex = value as string;
            if (string.IsNullOrEmpty(colorHex))
                return Colors.Gray;

            try
            {
                return Color.FromArgb(colorHex);
            }
            catch
            {
                return Colors.Gray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

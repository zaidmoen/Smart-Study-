using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SmartStudy.Converters;

public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            return (Brush)new BrushConverter().ConvertFromString(value?.ToString() ?? "#6C63FF")!;
        }
        catch
        {
            return Brushes.SlateBlue;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

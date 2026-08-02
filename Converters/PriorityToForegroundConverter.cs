using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SmartStudy.Models;

namespace SmartStudy.Converters;

public sealed class PriorityToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is StudyPriority priority
            ? priority switch
            {
                StudyPriority.Low => new SolidColorBrush(Color.FromRgb(22, 101, 52)),
                StudyPriority.Medium => new SolidColorBrush(Color.FromRgb(3, 105, 161)),
                StudyPriority.High => new SolidColorBrush(Color.FromRgb(194, 65, 12)),
                StudyPriority.Critical => new SolidColorBrush(Color.FromRgb(185, 28, 28)),
                _ => Brushes.SlateGray
            }
            : Brushes.SlateGray;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

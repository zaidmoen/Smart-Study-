using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SmartStudy.Models;

namespace SmartStudy.Converters;

public sealed class PriorityToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is StudyPriority priority
            ? priority switch
            {
                StudyPriority.Low => new SolidColorBrush(Color.FromRgb(220, 252, 231)),
                StudyPriority.Medium => new SolidColorBrush(Color.FromRgb(224, 242, 254)),
                StudyPriority.High => new SolidColorBrush(Color.FromRgb(255, 237, 213)),
                StudyPriority.Critical => new SolidColorBrush(Color.FromRgb(254, 226, 226)),
                _ => Brushes.WhiteSmoke
            }
            : Brushes.WhiteSmoke;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

using CommuniZEN.Models;
using System.Globalization;

namespace CommuniZEN.Converters
{

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AppointmentStatus status)
            {
                return status switch
                {
                    AppointmentStatus.Approved => Color.FromArgb("#4CAF50"),
                    AppointmentStatus.Pending => Color.FromArgb("#FFA500"),
                    AppointmentStatus.Rejected => Color.FromArgb("#FF4B4B"),
                    AppointmentStatus.Cancelled => Color.FromArgb("#95A5A6"),
                    AppointmentStatus.Completed => Color.FromArgb("#2B3A67"),
                    _ => Colors.Black
                };
            }
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}

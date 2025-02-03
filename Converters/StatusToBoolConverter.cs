using System.Globalization;
using CommuniZEN.Models;

namespace CommuniZEN.Converters
{
    public class StatusToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AppointmentStatus status)
            {
                return status switch
                {
                    AppointmentStatus.Pending => "#F6AD55",    // Orange
                    AppointmentStatus.Confirmed => "#48BB78",   // Green
                    AppointmentStatus.Cancelled => "#FC8181",   // Red
                    AppointmentStatus.Completed => "#4299E1",   // Blue
                    _ => "#4A5568"                             // Default gray
                };
            }
            return "#4A5568";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

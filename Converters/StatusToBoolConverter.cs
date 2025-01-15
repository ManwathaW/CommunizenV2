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
                // Only show cancel button for Pending and Approved appointments
                return status == AppointmentStatus.Pending || status == AppointmentStatus.Approved;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
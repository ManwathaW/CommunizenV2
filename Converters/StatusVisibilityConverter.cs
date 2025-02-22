using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CommuniZEN.Models;
using System.Threading.Tasks;

namespace CommuniZEN.Converters
{
    public class StatusVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AppointmentStatus status)
            {
                // Show cancel button only for Pending and Confirmed appointments
                if (parameter?.ToString() == "CancelAllowed")
                {
                    return status == AppointmentStatus.Pending ||
                           status == AppointmentStatus.Confirmed;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

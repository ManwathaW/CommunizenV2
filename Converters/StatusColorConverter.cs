using System;
using System.Collections.Generic;
using System.Globalization;
using CommuniZEN.Models;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace CommuniZEN.Converters
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not AppointmentStatus status)
                return "#4A5568";

            return status switch
            {
                AppointmentStatus.Pending => "#F6AD55",    // Orange
                AppointmentStatus.Confirmed => "#48BB78",   // Green
                AppointmentStatus.Cancelled => "#FC8181",   // Red
                AppointmentStatus.Completed => "#4299E1",   // Blue
                _ => "#4A5568"                             // Default gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
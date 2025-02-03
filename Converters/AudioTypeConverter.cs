using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommuniZEN.Models;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace CommuniZEN.Converters
{
    public class AudioTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Return true only if the entry type is Audio
            if (value is JournalEntryType type)
            {
                return type == JournalEntryType.Audio;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using CommuniZEN.Models;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Converters
{
    public class TextTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Return true only if the entry type is Text
            if (value is JournalEntryType type)
            {
                return type == JournalEntryType.Text;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

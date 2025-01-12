using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Converters
{

    public class BoolToColorConverter : IValueConverter, IMarkupExtension
    {
        public Color TrueColor { get; set; }
        public Color FalseColor { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSelected = (bool)value;
            if (parameter != null && parameter.ToString() == "text")
            {
                return isSelected ? Colors.White : Colors.Black;
            }
            return isSelected ? TrueColor : FalseColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

}

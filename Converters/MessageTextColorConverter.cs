using System;
using System.Collections.Generic;
using System.Globalization;
using CommuniZEN.Interfaces;
using CommuniZEN.Helpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Converters
{


    public class MessageTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var authService = ServiceHelper.GetService<IFirebaseAuthService>();
                if (authService == null) return Colors.Black;

                var senderId = value as string;
                var currentUserId = authService.GetCurrentUserIdAsync().Result;
                return senderId == currentUserId ? Color.FromArgb("#1976D2") : Colors.Black;
            }
            catch
            {
                return Colors.Black;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}



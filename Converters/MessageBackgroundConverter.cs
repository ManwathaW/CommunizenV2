using System;
using CommuniZEN.Interfaces;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommuniZEN.Helpers;

namespace CommuniZEN.Converters
{

    public class MessageBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var authService = ServiceHelper.GetService<IFirebaseAuthService>();
                if (authService == null) return Color.FromArgb("#F5F5F5");

                var senderId = value as string;
                var currentUserId = authService.GetCurrentUserIdAsync().Result;
                return senderId == currentUserId ? Color.FromArgb("#E3F2FD") : Color.FromArgb("#F5F5F5");
            }
            catch
            {
                return Color.FromArgb("#F5F5F5");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}

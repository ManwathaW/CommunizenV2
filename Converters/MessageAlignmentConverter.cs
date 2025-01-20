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

    public class MessageAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var authService = ServiceHelper.GetService<IFirebaseAuthService>();
                if (authService == null) return LayoutOptions.Start;

                var senderId = value as string;
                var currentUserId = authService.GetCurrentUserIdAsync().Result;
                return senderId == currentUserId ? LayoutOptions.End : LayoutOptions.Start;
            }
            catch
            {
                return LayoutOptions.Start;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}

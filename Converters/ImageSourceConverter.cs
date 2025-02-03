using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;


    namespace CommuniZEN.Converters
    {

    public class ImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (string.IsNullOrEmpty(value?.ToString()))
                    return "default_profile.png";

                var imageString = value.ToString();
                if (imageString.StartsWith("http"))
                    return ImageSource.FromUri(new Uri(imageString));

                if (imageString.StartsWith("data:image"))
                {
                    var base64Data = imageString.Split(",")[1];
                    var bytes = System.Convert.FromBase64String(base64Data);
                    return ImageSource.FromStream(() => new MemoryStream(bytes));
                }

                return ImageSource.FromFile(imageString);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Image conversion error: {ex}");
                return "default_profile.png";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}





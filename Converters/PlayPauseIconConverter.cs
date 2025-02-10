using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CommuniZEN.ViewModels;

using System.Threading.Tasks;
using System.Diagnostics;

namespace CommuniZEN.Converters
{
    public class PlayPauseIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (parameter is JournalViewModel viewModel && value is string entryId)
                {
                    return viewModel.CurrentlyPlayingEntryId == entryId && viewModel.IsPlaying ? "⏸" : "▶";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PlayPauseIconConverter error: {ex.Message}");
            }
            return "▶";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

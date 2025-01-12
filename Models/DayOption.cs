using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{

    public partial class DayOption : ObservableObject
    {
        [ObservableProperty]
        private string dayName;

        [ObservableProperty]
        private bool isSelected;
    }

}

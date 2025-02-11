using System;
using System.Collections.Generic;
using CommuniZEN.Interfaces;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Services
{
    public class NavigationService : INavigationService
    {
        public async Task NavigateToAsync(string route)
        {
            await Shell.Current.GoToAsync(route);
        }
    }
}

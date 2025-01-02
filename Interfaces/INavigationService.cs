using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Interfaces
{
    public interface INavigationService
    {
        Task NavigateToAsync(string route);
    }
}

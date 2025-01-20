using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Helpers
{
    public static class ServiceHelper
    {
        public static TService GetService<TService>() where TService : class
        {
            if (Application.Current?.Handler?.MauiContext != null)
            {
                return Application.Current.Handler.MauiContext.Services.GetService<TService>();
            }
            return null;
        }
    }
}

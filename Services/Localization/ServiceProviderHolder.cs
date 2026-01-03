using System;
using Microsoft.Extensions.DependencyInjection;

namespace UnifiWatch.Services.Localization
{
    public static class ServiceProviderHolder
    {
        public static IServiceProvider? Provider { get; set; }

        public static T? GetService<T>() where T : class
        {
            return Provider?.GetService<T>();
        }
    }
}
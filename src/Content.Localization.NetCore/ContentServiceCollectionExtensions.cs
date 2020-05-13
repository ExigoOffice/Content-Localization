using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Content.Localization
{
    public static class ContentServiceCollectionExtensions
    {
        public static IContentBuilder AddContentLocalization(this IServiceCollection services )
        {
            
    
            return new NetCoreLocalizationBuilder(services);

        }


        public static IServiceCollection GetServices(this IContentBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return ((NetCoreLocalizationBuilder)builder).Services;
        }

        public class NetCoreLocalizationBuilder : IContentBuilder
        {
            
            public IServiceCollection Services { get; }
            public IContentLogger ContentLogger { get; set; } = new NullContentLogger();

            public NetCoreLocalizationBuilder(IServiceCollection services)
            {
                Services = services;
            }

            public IContentBuilder AddContentSource<TContentSource>() where TContentSource : IContentSource
            {
                return this;
            }

            public IContentBuilder AddContentSource<TContentSource>(Func<TContentSource> factory) where TContentSource : IContentSource
            {
                throw new NotImplementedException();
            }

            public IContentBuilder AddLogger<TLogger>(Func<TLogger> factory) where TLogger : IContentLogger
            {
                throw new NotImplementedException();
            }
        }



    }
}

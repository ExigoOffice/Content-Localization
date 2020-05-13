using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Content.Localization
{
    public static class ApiContentBuilderExtensions
    {
        
        static readonly HttpClient _cachedClient = new HttpClient {  Timeout = TimeSpan.FromMinutes(10) };
            
        public static IContentBuilder AddApiSource(this IContentBuilder builder,  Action<ApiContentSourceOptions> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var o = new ApiContentSourceOptions();
            options(o);

            #pragma warning disable CA1303 // Do not pass literals as localized parameters
            if (string.IsNullOrEmpty(o.EnvironmentCode))
                throw new Exception("EnvironmentCode is expected for Api Calls");

            if (string.IsNullOrEmpty(o.SubscriptionKey))
               throw new Exception("SubscriptionKey is expected for Api Calls");
            #pragma warning restore CA1303 // Do not pass literals as localized parameters

            //We need a version of this which uses the HttpClientFactory
            builder.AddContentSource(() => new ApiContentSource(() => _cachedClient, o, builder.ContentLogger));

            return builder;
        }
    }
}

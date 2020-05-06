using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Localization
{
    public static class JsonFileBuilderExtensions
    {
        public static IContentBuilder AddJsonFileSource(this IContentBuilder builder, Action<JsonFileContentSourceOptions> options )
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var o = new JsonFileContentSourceOptions();
            options(o);

            builder.AddContentSource(() => new JsonFileContentSource (o.Location));
            return builder;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Localization
{
    public static class ProtoFileBuilderExtensions
    {
        public static IContentBuilder AddProtoFileSource(this IContentBuilder builder, string location )
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (location is null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            builder.AddContentSource(() => new ProtoFileContentSource (location));
            return builder;

        }


        public static IContentBuilder AddProtoFileSource(this IContentBuilder builder, Action<ProtoFileContentSourceOptions> options )
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var o = new ProtoFileContentSourceOptions();
            options(o);

            builder.AddContentSource(() => new ProtoFileContentSource (o.Location));
            return builder;
        }
    }
}

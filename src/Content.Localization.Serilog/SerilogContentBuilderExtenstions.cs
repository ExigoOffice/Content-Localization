using System;
using System.Collections.Generic;
using System.Text;

using Content.Localization.Serilog;

using Serilog;

namespace Content.Localization
{
    public static class SerilogContentBuilderExtenstions
    {

        public static IContentBuilder AddSerilog(this IContentBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddSerilog(Log.Logger);
        }


        public static IContentBuilder AddSerilog(this IContentBuilder builder,  ILogger logger)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddLogger( () => new SerilogContentLogger(logger));

            return builder;
            
        }
    }
}

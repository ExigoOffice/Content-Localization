using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Localization
{
    public static class LocalizerConfigurationExtensions
    {
        public static IContentLocalizer BuildLocalizer(this IContentBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var ourBuilder = (LocalizerConfiguration)builder;
            var list = ourBuilder.Sources;
            IContentSource last = null;
            foreach (var source in list)
            {
                if (last!=null)
                    last.NextSource = source;

                last = source;
            }


            var updater = ourBuilder.UpdaterFactory?.Value;
            _ = updater?.StartAsync();

            return new LocalizerContainer(
                contentLocalizer: new ContentLocalizer(list[0], "en-US"),
                updater: updater
                );

        }

        public static IContentBuilder AddUpdater(this IContentBuilder builder, Action<ContentUpdaterOptions> options)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var o = new ContentUpdaterOptions();
            options(o);

            var ourBuilder = (LocalizerConfiguration)builder;

            ourBuilder.UpdaterFactory = new Lazy<ContentUpdater>( () => new ContentUpdater(
                ourBuilder.Sources[0], 
                o.StartupDelay,
                o.Frequency, 
                ourBuilder.ContentLogger,
                ourBuilder.ClassGenerator
                ));

            return builder;
        }

        public static IContentBuilder AddClassGenerator(this IContentBuilder builder, Action<ContentClassGeneratorOptions> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var ourBuilder = (LocalizerConfiguration)builder;
            
            var o = new ContentClassGeneratorOptions();
            options(o);

            ourBuilder.ClassGenerator = new  ContentClassGenerator(o);

            return builder;
        }
    }
}

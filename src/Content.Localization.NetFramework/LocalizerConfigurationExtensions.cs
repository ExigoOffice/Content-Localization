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


        public static IContentBuilder AddUpdater(this IContentBuilder builder, TimeSpan frequency)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var ourBuilder = (LocalizerConfiguration)builder;

            ourBuilder.UpdaterFactory = new Lazy<ContentUpdater>( () => new ContentUpdater(ourBuilder.Sources[0], frequency, ourBuilder.ContentLogger ));

            return builder;
        }
    }
}

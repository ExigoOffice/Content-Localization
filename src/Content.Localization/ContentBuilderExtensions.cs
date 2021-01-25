using System;

namespace Content.Localization
{
    public static class MemoryContentSourceExtensions
    {
        public static IContentBuilder AddMemorySource(this IContentBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddContentSource(() => new MemoryContentSource());
            return builder;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Localization
{
    public interface IContentBuilder
    {
        IContentLogger ContentLogger { get; set; }
        IContentBuilder AddContentSource<TContentSource>(Func<TContentSource> factory)  where TContentSource : IContentSource;
        IContentBuilder AddLogger<TLogger>(Func<TLogger> factory) where TLogger : IContentLogger;
    }
}

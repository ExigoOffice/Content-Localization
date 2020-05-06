using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Localization
{
    public interface IContentBuilder
    {
        
        //IContentBuilder AddContentSource<TContentSource>()  where TContentSource : IContentSource;
        IContentBuilder AddContentSource<TContentSource>(Func<TContentSource> factory)  where TContentSource : IContentSource;

    }
}

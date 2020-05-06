using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Localization
{
    public interface IContentLocalizer
    {
        /// <summary>
        /// Fully localized and qualified based on scheduling and enabled flags
        /// </summary>
        ContentItem this[string name] { get; }
    }
}

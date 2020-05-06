using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Content.Localization
{
    /// <summary>
    /// Holder of localizer. 
    /// </summary>
    public static class Localizer
    {
        public static IContentLocalizer Content { get; set; }

        /// <summary>
        /// Call in application shutdown to gracefully handle cancellation
        /// </summary>
        public static void Close()
        {
            if (Content is IDisposable disposable)
                disposable.Dispose();
        }
    }
}

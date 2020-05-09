using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Localization
{
    /// <summary>
    /// Holder for localizer and updater
    /// </summary>
    internal class LocalizerContainer : IContentLocalizer, IDisposable
    {
        private readonly IContentLocalizer _contentLocalizer;
        private readonly ContentUpdater _updater;

        internal LocalizerContainer(IContentLocalizer contentLocalizer, ContentUpdater updater)
        {
            _contentLocalizer = contentLocalizer;
            _updater = updater;
        }
        public string this[string name] => _contentLocalizer[name];
        public ContentItem Localize(string name) => Localize(name);
        public void Dispose()
        {
            _updater?.StopAsync().GetAwaiter().GetResult();
        }
    }
}

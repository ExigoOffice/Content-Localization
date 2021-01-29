using System;
using System.Globalization;

namespace Content.Localization
{
    //The instance here is meant to be one per request 
    public class ContentLocalizer : IContentLocalizer
    {
        private readonly IContentSource _source;
        private readonly string _defaultCultureCode;

        public ContentLocalizer(IContentSource source, string defaultCultureCode)
        {
            var renderedSource = new RenderedContentSource( defaultCultureCode);

            if (source?.NextSource != null)
            {
                renderedSource.NextSource = source.NextSource;
                source.NextSource = renderedSource;
                _source = source;
            }
            else
            {
                renderedSource.NextSource = source;
                _source = renderedSource;
            }

            _defaultCultureCode = defaultCultureCode;
        }

        public string this[string name] => GetContent(name);

        public ContentItem Localize(string name) => GetContent(name);

        public ContentItem GetContent(string name)
        {
            var item = FindContentItem(name);

            if (item == null || !item.Enabled || !Between(DateTime.UtcNow, item.EnabledStartDate, item.EnabledEndDate) )
                return new ContentItem { Name = name, Value = "", Enabled = true };
            
            return item;
        }

        //TODO: fine tune fall back logic
        internal ContentItem FindContentItem(string key)
        {
            // what is my culture? 
            var myCulture = CultureInfo.CurrentUICulture;

            // Do we have it on the first try
            var item = _source.GetContentItem(key, myCulture.Name);

            if (item == null && !myCulture.IsNeutralCulture && myCulture.Name != _defaultCultureCode)
                item = _source.GetContentItem(key, myCulture.TwoLetterISOLanguageName);
            
            // If we are not in the default culture code 
            if (item == null && myCulture.Name != _defaultCultureCode)
                item = _source.GetContentItem(key, _defaultCultureCode);
            
            return item;
        }


        private static bool Between(DateTime input, DateTime? date1 = null, DateTime? date2 = null)
        {
            if (!date1.HasValue || !date2.HasValue)
                return true;

            return input > date1 && input < date2;
        }


    }
}

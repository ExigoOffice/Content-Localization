using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Content.Localization
{
    //The instance here is meant to be one per request 
    public class ContentLocalizer : IContentLocalizer
    {
        private readonly IContentSource _source;
        private readonly string _defaultCultureCode;

        public ContentLocalizer(IContentSource source, string defaultCultureCode)
        {
            _source = source;
            _defaultCultureCode = defaultCultureCode;
        }

        public ContentItem this[string name] => GetContent(name);

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

            if (item != null)
                return item;

            if (!myCulture.IsNeutralCulture && myCulture.Name != _defaultCultureCode)
            {
                item = _source.GetContentItem(key, myCulture.TwoLetterISOLanguageName);

                if (item != null)
                    return item;
            }

            // If we are not in the default culture code 
            if (myCulture.Name != _defaultCultureCode)
            {
                //Are we then 
                return _source.GetContentItem(key, _defaultCultureCode);

            }

            return null;
        }

        private static bool Between(DateTime input, DateTime? date1 = null, DateTime? date2 = null)
        {
            if (!date1.HasValue || !date2.HasValue)
                return true;

            return input > date1 && input < date2;
        }

    }
}

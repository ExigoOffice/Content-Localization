using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Localization
{
    public class RenderedContentSource : IContentSource
    {
        private const string CircularReferenceDetectionMessage = "Circular Reference Detected";
        private const string SelfReferenceMessage = "Self Reference Detected";
        private static readonly Regex ResourceTokenPattern = new("{{(.*?)}}", RegexOptions.Compiled);
        
        private readonly Func<IContentSource> _alternateContentSourceFactory;
        private readonly string _defaultCultureCode;

        /// <summary>
        /// Proxy content source that sits between two other content sources A and B.
        /// It's primary purpose is to take raw resources from A and return it rendered to B.
        /// </summary>
        /// <param name="alternateContentSourceFactory">Should build content source that supports efficient lookups.</param>
        /// <param name="defaultCultureCode"></param>
        public RenderedContentSource(Func<IContentSource> alternateContentSourceFactory, string defaultCultureCode)
        {
            _alternateContentSourceFactory = alternateContentSourceFactory;
            _defaultCultureCode = defaultCultureCode;
        }

        public ContentItem GetContentItem(string key, string cultureCode)
        {
            var source = _alternateContentSourceFactory();
            source.NextSource = NextSource;

            var cultureInfo = CultureInfo.GetCultureInfo(cultureCode);

            var item = LayeredLanguageItemLookup(key, cultureInfo, source);

            return LoadNestedResources(item, cultureInfo, source);
        }

        private ContentItem LayeredLanguageItemLookup(string key, CultureInfo cultureInfo, IContentSource source)
        {
            var item = source.GetContentItem(key, cultureInfo.Name);

            if (item == null && !cultureInfo.IsNeutralCulture && cultureInfo.Name != _defaultCultureCode)
            {
                item = source.GetContentItem(key, cultureInfo.TwoLetterISOLanguageName);
            }

            if (item == null && cultureInfo.Name != _defaultCultureCode)
            {
                item = source.GetContentItem(key, _defaultCultureCode);
            }

            return item;
        }
        
        private ContentItem LoadNestedResources(ContentItem content, CultureInfo culture, IContentSource source, HashSet<string> seen = null, StringBuilder builder = null, int stackDepth = 1)
        {
            if (!string.IsNullOrWhiteSpace(content?.Value))
            {
                var matches = ResourceTokenPattern.Matches(content.Value);
                if (matches.Count <= 0)
                    return content;

                // Create copy so we don't modify existing stuff
                var contentItem = new ContentItem
                {
                    Enabled = content.Enabled,
                    Name = content.Name,
                    Value = content.Value,
                    EnabledEndDate = content.EnabledEndDate,
                    EnabledStartDate = content.EnabledStartDate
                };

                builder ??= new StringBuilder(contentItem.Value);
                seen ??= new HashSet<string>();

                seen.Add(contentItem.Name);

                foreach (Match match in matches)
                {
                    var resourceName = match.Groups[1].Value;

                    if (contentItem.Name == resourceName)
                    {
                        builder.Replace(match.Value, $"{SelfReferenceMessage} [{resourceName}]");
                        continue;
                    }

                    if (seen.Contains(resourceName))
                    {
                        builder.Replace(match.Value, $"{CircularReferenceDetectionMessage} [{resourceName}]");
                        continue;
                    }

                    var nestedItem = LayeredLanguageItemLookup(resourceName, culture, source);

                    // Replace Resource Token With Actual Value
                    builder.Replace(match.Value, nestedItem?.Value ?? string.Empty);

                    LoadNestedResources(nestedItem, culture, source, seen, builder, stackDepth + 1);
                }

                seen.Remove(contentItem.Name);

                if (stackDepth == 1)
                {
                    contentItem.Value = builder.ToString();
                    return contentItem;
                }
            }

            return content;
        }



        public Task<ContentVersion> CheckForChangesAsync(ContentVersion knownVersion = null, CancellationToken token = default)
        {
            return NextSource.CheckForChangesAsync(knownVersion, token);
        }

        public async Task<IEnumerable<ContentItem>> GetAllContentItemsAsync(string cultureCode, ContentVersion requestedVersion)
        {
            var defaultResources = await NextSource.GetAllContentItemsAsync(_defaultCultureCode, requestedVersion);
            var defaultResourceNames = new HashSet<string>(defaultResources.Select(resource => resource.Name));

            return defaultResourceNames.Select(name => GetContentItem(name, cultureCode));
        }

        public Task<IEnumerable<string>> GetCultureCodesAsync(ContentVersion requestedVersion)
        {
            return NextSource.GetCultureCodesAsync(requestedVersion);
        }

        public Task SaveAllContentItemsAsync(string cultureCode, IEnumerable<ContentItem> items)
        {
            return Task.CompletedTask;
        }

        public IContentSource NextSource { get; set; }
    }
}
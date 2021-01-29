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
        private readonly IContentSource _memorySource = new MemoryContentSource();

        private readonly string _defaultCultureCode;
        /// <summary>
        /// Proxy content source that sits between two other content sources A and B.
        /// It's primary purpose is to take raw resources from A and return it rendered to B.
        /// </summary>
        /// <param name="defaultCultureCode"></param>
        public RenderedContentSource(string defaultCultureCode)
        {
            _defaultCultureCode = defaultCultureCode;
        }
        public ContentItem GetContentItem(string key, string cultureCode)
        {
            var cultureInfo = CultureInfo.GetCultureInfo(cultureCode);
            var item = LayeredLanguageItemLookup(key, cultureInfo);

            if (item != null && (!item.Enabled || !Between(DateTime.UtcNow, item.EnabledStartDate, item.EnabledEndDate)))
                return new ContentItem { Name = item.Name, Value = "", Enabled = false };

            return LoadNestedResources(item, cultureInfo);
        }

        private static bool Between(DateTime input, DateTime? date1 = null, DateTime? date2 = null)
        {
            if (!date1.HasValue || !date2.HasValue)
                return true;

            return input > date1 && input < date2;
        }

        private ContentItem LayeredLanguageItemLookup(string key, CultureInfo cultureInfo)
        {
            var item = _memorySource.GetContentItem(key, cultureInfo.Name);
            if (item == null && !cultureInfo.IsNeutralCulture && cultureInfo.Name != _defaultCultureCode)
            {
                item = _memorySource.GetContentItem(key, cultureInfo.TwoLetterISOLanguageName);
            }
            if (item == null && cultureInfo.Name != _defaultCultureCode)
            {
                item = _memorySource.GetContentItem(key, _defaultCultureCode);
            }
            return item;
        }
        private ContentItem LoadNestedResources(ContentItem content, CultureInfo culture, HashSet<string> seen = null, StringBuilder builder = null, int stackDepth = 1)
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
                    var nestedItem = LayeredLanguageItemLookup(resourceName, culture);
                    if (nestedItem?.Enabled == true && Between(DateTime.UtcNow, nestedItem.EnabledStartDate, nestedItem.EnabledEndDate))
                    {
                        // Replace Resource Token With Actual Value
                        builder.Replace(match.Value, nestedItem?.Value ?? string.Empty);
                        LoadNestedResources(nestedItem, culture, seen, builder, stackDepth + 1);
                    }
                    else
                    {
                        builder.Replace(match.Value, string.Empty);
                    }
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
            return _memorySource.CheckForChangesAsync(knownVersion, token);
        }
        public async Task<IEnumerable<ContentItem>> GetAllContentItemsAsync(string cultureCode, ContentVersion requestedVersion)
        {
            var defaultResources = await _memorySource.GetAllContentItemsAsync(_defaultCultureCode, requestedVersion)
                .ConfigureAwait(false);
            var defaultResourceNames = new HashSet<string>(defaultResources.Select(resource => resource.Name));
            return defaultResourceNames.Select(name => GetContentItem(name, cultureCode));
        }
        public Task<IEnumerable<string>> GetCultureCodesAsync(ContentVersion requestedVersion)
        {
            return _memorySource.GetCultureCodesAsync(requestedVersion);
        }
        public Task SaveAllContentItemsAsync(string cultureCode, IEnumerable<ContentItem> items)
        {
            return Task.CompletedTask;
        }
        public IContentSource NextSource
        {
            get => _memorySource.NextSource;
            set => _memorySource.NextSource = value;
        }
    }
}
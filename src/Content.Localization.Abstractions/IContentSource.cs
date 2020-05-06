using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Localization
{
    public interface IContentSource
    {
        ContentItem GetContentItem(string key, string cultureCode);
        Task<ContentVersion> CheckForChangesAsync(ContentVersion knownVersion=null, CancellationToken token=default);
        Task<IEnumerable<ContentItem>> GetAllContentItemsAsync(string cultureCode, ContentVersion requestedVersion);
        Task<IEnumerable<string>> GetCultureCodesAsync(ContentVersion requestedVersion);
        Task SaveAllContentItemsAsync(string cultureCode, IEnumerable<ContentItem> items);
        IContentSource NextSource { get; set; }
    }
}

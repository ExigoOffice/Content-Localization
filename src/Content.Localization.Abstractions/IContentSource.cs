using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Content.Localization
{
    public interface IContentSource
    {
        //Mainly used by memory cache, not async as needs fast memory only path 99.999% pc of time
        ContentItem GetContentItem(string key, string cultureCode);

        //Gets sources known version
        //Task<ContentVersion> GetVersionAsync();

        //Invokes the source to call into its inner sources to optionally cascade updates
        Task<ContentVersion> CheckForChangesAsync(ContentVersion currentVersion=null);
        Task<IEnumerable<string>> GetCultureCodesAsync(ContentVersion currentVersion=null);
        Task<IEnumerable<ContentItem>> GetAllContentItemsAsync(string cultureCode,ContentVersion currentVersion=null);
        Task SaveAllContentItemsAsync(string cultureCode, IEnumerable<ContentItem> items);

        IContentSource NextSource { get; }
    }
}

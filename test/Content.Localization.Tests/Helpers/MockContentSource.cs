using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Localization.Tests
{
    //Not intended to be thread safe
    public class MockContentSource : IContentSource
    {
        public MockContentSource()
        {
        }

        public Dictionary<string,  Dictionary<string, Dictionary<string, ContentItem>>> Data { get;  }
            = new Dictionary<string, Dictionary<string, Dictionary<string, ContentItem>>>();


        private readonly object _lockObject = new object();

        public ContentVersion ContentVersion { get; set; }
            
        public int GetAllContentItemsInvokeCount { get; set; }

        public void SetVersion(string version, DateTime modifiedDate)
        {
            lock(_lockObject)
            { 
                ContentVersion = new ContentVersion {  Version = version, ReleaseDate = modifiedDate };
            }
        }

        public void SetData(string cultureCode, Dictionary<string, string> values)
        {
            lock(_lockObject)
            { 
                var dict = new Dictionary<string, ContentItem>();

                foreach (var kv in values)
                {
                    dict.Add(kv.Key, new ContentItem { Name = kv.Key, Value =  kv.Value, Enabled = true });
                }

                var key = $"{ContentVersion?.ReleaseDate}|{ContentVersion?.Version}";


                if (!Data.ContainsKey(key))
                { 
                    Data[key] = new Dictionary<string, Dictionary<string, ContentItem>>();

                }

                Data[key][cultureCode] = dict;
            }
        }

        public IContentSource NextSource {get; set; }

        public ContentItem GetContentItem(string name, string cultureCode)
        {
            throw new Exception("Not intended to be called");
        }

        public Task<IEnumerable<ContentItem>> GetAllContentItemsAsync(string cultureCode, ContentVersion prevVersion = null)
        {
            var key = $"{ContentVersion?.ReleaseDate}|{ContentVersion?.Version}";

            lock(_lockObject)
            { 
                GetAllContentItemsInvokeCount++;
                
                if (!Data.ContainsKey(key))
                { 
                    Data[key] = new Dictionary<string, Dictionary<string, ContentItem>>();
                }

                if (!Data[key].ContainsKey(cultureCode))
                    Data[key][cultureCode] = new Dictionary<string, ContentItem>();  
            }
            
            return Task.FromResult(Data[key][cultureCode].Values.AsEnumerable());       
            
        }

        public Task SaveAllContentItemsAsync(string cultureCode, IEnumerable<ContentItem> items)
        {
            throw new Exception("Not intended to be called");
        }

        public Task<ContentVersion> GetVersionAsync()
        {
            return Task.FromResult(ContentVersion);
        }

        public Task<ContentVersion> CheckForChangesAsync(ContentVersion prevVersion = null, CancellationToken token=default)
        {
            return GetVersionAsync();
        }

        public Task<IEnumerable<string>> GetCultureCodesAsync(ContentVersion prevVersion = null)
        {
            var key = $"{ContentVersion?.ReleaseDate}|{ContentVersion?.Version}";

            return Task.FromResult(Data[key].Keys.AsEnumerable());
        }
    }
}

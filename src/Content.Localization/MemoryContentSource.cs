using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Localization
{

    //In this first pass we will load the full list (for the culture requested)


    public class MemoryContentSource : IContentSource
    {


        //Using lazy will ensure the initialization is thread safe
        readonly ConcurrentDictionary<string, AtomicLazy<ConcurrentDictionary<string, ContentItem>>> _cache;

        ContentVersion _version;

        public IContentSource NextSource { get; }

        public MemoryContentSource(IContentSource next)
        {
            _version            = new ContentVersion();
            _cache              = new ConcurrentDictionary<string, AtomicLazy<ConcurrentDictionary<string, ContentItem>>>();
            NextSource = next;
        }


        //This will get hit with thousands of hits before things are ready
        public ContentItem GetContentItem(string name, string cultureCode)
        {             
            // Model for 1000 resources usage on single page
            // Model for 100 threads trying to get it all at once

            //By using Lazy operator we are ensuring the cache is initialed once and other threads
            //are blocked while it is initializing

            //in addition if it errors out while initializing, you can try again

            GetCacheForCulture(cultureCode).TryGetValue(name, out var value);

            return value;
        }

        private ConcurrentDictionary<string, ContentItem> GetCacheForCulture(string cultureCode)
        {
            return _cache.GetOrAdd(cultureCode, (_) => new AtomicLazy<ConcurrentDictionary<string, ContentItem>>( 
                () =>
                {
                    //If this blocks I can move into a Task.Run                                
                    var items   = NextSource.GetAllContentItemsAsync(cultureCode)
                                        .ConfigureAwait(false)
                                        .GetAwaiter()
                                        .GetResult(); 

                    var dict = new ConcurrentDictionary<string, ContentItem>();
                    foreach (var item in items)
                    {
                        dict.TryAdd(item.Name, item);
                    }
                    return dict;                

            })).Value;
        }


        public Task<IEnumerable<ContentItem>> GetAllContentItemsAsync(string cultureCode, ContentVersion currentVersion=null)
        {
            return Task.FromResult(GetCacheForCulture(cultureCode).Values.AsEnumerable());
        }

        public  Task<ContentVersion> GetVersionAsync()
        {
            return Task.FromResult(_version);
        }

        
        public async Task<ContentVersion> CheckForChangesAsync(ContentVersion prevVersion = null)
        {
            var myVersion   = await GetVersionAsync().ConfigureAwait(false);
            var nextVersion = await NextSource.CheckForChangesAsync(myVersion).ConfigureAwait(false);

            if (myVersion.Version != nextVersion.Version || myVersion.ReleaseDate != nextVersion.ReleaseDate)
            {
                foreach (var cultureCode in await NextSource.GetCultureCodesAsync().ConfigureAwait(false))
                {
                    var items = await NextSource.GetAllContentItemsAsync(cultureCode).ConfigureAwait(false);
                    
                    //updates the memory with new values
                    await SaveAllContentItemsAsync(cultureCode, items).ConfigureAwait(false);
                }
                
                _version = nextVersion;
            }

            return _version;
        }

        public Task SaveAllContentItemsAsync(string cultureCode, IEnumerable<ContentItem> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }
            //this updates the dictionary with an already initialized lazy loader
            var dict = new ConcurrentDictionary<string, ContentItem>();
            foreach (var item in items)
            {
                dict.TryAdd(item.Name, item);
            }
            var val = new AtomicLazy<ConcurrentDictionary<string, ContentItem>>(dict);
            _cache.AddOrUpdate(cultureCode, val, (key, oldValue) => val );
            return Task.CompletedTask;
        }

        public Task<IEnumerable<string>> GetCultureCodesAsync(ContentVersion currentVersion=null)
        {
            return Task.FromResult(_cache.Keys.AsEnumerable());
        }


        //We use this instead of lazy because lazy caches exceptions
        //this will allow retry's
        class AtomicLazy<T>
        {
            private readonly Func<T> _factory;
            private T _value;
            private bool _initialized;
            private object _lock;

            public AtomicLazy(Func<T> factory)
            {
                _factory = factory;
            }

            public AtomicLazy(T value)
            {
                _initialized = true;
                _value = value;
            }

            public T Value => LazyInitializer.EnsureInitialized(ref _value, ref _initialized, ref _lock, _factory);
        }
    }
}

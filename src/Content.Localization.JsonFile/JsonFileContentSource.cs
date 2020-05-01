using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Localization
{
    public class JsonFileContentSource : IContentSource
    {
        private readonly string _location;

        public IContentSource NextSource { get; }

        public JsonFileContentSource(string location, IContentSource next)
        {
            _location = location;
            NextSource = next;
        }

        public string GetCultureFileName(string cultureCode)
        {
            return Path.Combine(_location, cultureCode + ".json");
        }
        
        public string GetVersionFileName()
        {
            return Path.Combine(_location, "version.json");
        }
        

        public async Task<IEnumerable<ContentItem>> GetAllContentItemsAsync(string cultureCode, ContentVersion currentVersion=null)
        {
            string fileName     = GetCultureFileName(cultureCode);

            var tryCount = 100;
            
            for (int i = 0; i < tryCount; i++)
            {
                try
                {
                    if (!File.Exists(fileName) && NextSource !=null)
                    {
                        var items = await NextSource.GetAllContentItemsAsync(cultureCode).ConfigureAwait(false);
                        await SaveAllContentItemsAsync(cultureCode, items).ConfigureAwait(false);
                    }

                    using var file      = File.OpenRead(fileName);
                    using var sr        = new StreamReader(file);
                    using var reader    = new JsonTextReader(sr);
                    var serializer      = new JsonSerializer();

                    return serializer.Deserialize<IEnumerable<ContentItem>>(reader);
                }
                catch(IOException)
                {
                    if (i == (tryCount - 1))
                        throw;                            

                    await Task.Delay(100).ConfigureAwait(false);
                }
            }

            #pragma warning disable CA1303 // Do not pass literals as localized parameters
            throw new IOException("Error Getting File");
            #pragma warning restore CA1303 // Do not pass literals as localized parameters
        }

        public Task SaveAllContentItemsAsync(string cultureCode, IEnumerable<ContentItem> items)
        {
            try
            { 
                string fileName     = GetCultureFileName(cultureCode);
                using var file      = File.OpenWrite(fileName);
                using var sr        = new StreamWriter(file);
                using var writer    = new JsonTextWriter(sr);
                var serializer      = new JsonSerializer();

                serializer.Serialize(writer, items);
            }
            #pragma warning disable CA1031 // Do not catch general exception types
            catch (IOException)
            {
                //swallow write locking errors
            }
            #pragma warning restore CA1031 // Do not catch general exception types

            return Task.CompletedTask;
        }


        public async Task<ContentVersion> GetVersionAsync()
        {
            var tryCount = 100;
            
            for (int i = 0; i < tryCount; i++)
            {
                try
                {
                    var fi = new FileInfo(GetVersionFileName());
                    if (fi.Exists)
                    {
                
                        using var file      = fi.OpenRead();
                        using var sr        = new StreamReader(file);
                        using var reader    = new JsonTextReader(sr);
                        var serializer      = new JsonSerializer();

                        return serializer.Deserialize<ContentVersion>(reader);
                    }
                }
                catch(IOException)
                {
                    if (i == (tryCount - 1))
                        throw;                            

                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
            return new ContentVersion();
        }



        public async Task<ContentVersion> CheckForChangesAsync(ContentVersion currentVersion = null)
        {
            var myVersion    = await GetVersionAsync().ConfigureAwait(false);
            var nextVersion   = await NextSource.CheckForChangesAsync(myVersion).ConfigureAwait(false);

            if (myVersion.Version != nextVersion.Version || myVersion.ReleaseDate != nextVersion.ReleaseDate)
            {
                foreach (var cultureCode in await NextSource.GetCultureCodesAsync().ConfigureAwait(false))
                {
                    var items = await NextSource.GetAllContentItemsAsync(cultureCode).ConfigureAwait(false);

                    await SaveAllContentItemsAsync(cultureCode, items).ConfigureAwait(false);
                }

                //TODO: possibly clean up any cultures we've deleted from the next source? 
                
                await SaveVersionAsync(nextVersion).ConfigureAwait(false);
            }

            return nextVersion;
        }


        Task SaveVersionAsync(ContentVersion version)
        {
            try
            { 
                string fileName     = GetVersionFileName();
                using var file      = File.OpenWrite(fileName);
                using var sr        = new StreamWriter(file);
                using var writer    = new JsonTextWriter(sr);
                var serializer      = new JsonSerializer();

                serializer.Serialize(writer, version);
            }
            #pragma warning disable CA1031 // Do not catch general exception types
            catch (IOException)
            {
                //swallow write errors
            }
            #pragma warning restore CA1031 // Do not catch general exception types
            return Task.CompletedTask;
        }



        public Task<IEnumerable<string>> GetCultureCodesAsync(ContentVersion currentVersion=null)
        {
            var di = new DirectoryInfo(_location);
            var files = di.GetFiles("*.json");
            return Task.FromResult(
                files.Where(s=> s.Name!="version.json")
                    .Select( f=> f.Name.Substring( 0, f.Name.LastIndexOf(".", StringComparison.InvariantCulture)))
                );
        }

        public ContentItem GetContentItem(string name, string cultureCode)
        {
            //This is only for unit tests
            return GetAllContentItemsAsync(cultureCode)
                .GetAwaiter()
                .GetResult()
                .FirstOrDefault(i=>i.Name == name);
        }

    }
}

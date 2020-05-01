using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Localization
{
    public class ProtoFileContentSource : IContentSource
    {
        private const string _errorGettingFile = "Error Getting File";
        private readonly string _location;

        public IContentSource NextSource { get; }

        public ProtoFileContentSource(string location, IContentSource next)
        {
            _location = location;
            NextSource = next;
        }

        public string GetCultureFileName(string cultureCode)
        {
            return Path.Combine(_location, cultureCode + ".dat");
        }

        public string GetVersionFileName()
        {
            return Path.Combine(_location, "version.dat");
        }
        

        public async Task<IEnumerable<ContentItem>> GetAllContentItemsAsync(string cultureCode, ContentVersion currentVersion=null)
        {
            string fileName = GetCultureFileName(cultureCode);

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

                    using var file  = File.OpenRead(fileName);

                    return Serializer.Deserialize<IEnumerable<ContentItem>>(file);
                }
                catch(IOException)
                {
                    if (i == (tryCount - 1))
                        throw;                            

                    await Task.Delay(100).ConfigureAwait(false);
                }
            }

            #pragma warning disable CA1303 // Do not pass literals as localized parameters
            throw new IOException(_errorGettingFile);
            #pragma warning restore CA1303 // Do not pass literals as localized parameters
        }

        public Task SaveAllContentItemsAsync(string cultureCode, IEnumerable<ContentItem> items)
        {
            string fileName = GetCultureFileName(cultureCode);
            
            try
            { 
                using var file = File.Create(fileName);
                Serializer.Serialize(file, items);
            }
            #pragma warning disable CA1031 // Do not catch general exception types
            catch (IOException)
            {
                //The file *may* be locked from another process write, wan
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
                        using var file  = fi.OpenRead();

                        return Serializer.Deserialize<ContentVersion>(file);
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

        public async Task<ContentVersion> CheckForChangesAsync(ContentVersion version)
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
                using var file = File.Create(GetVersionFileName());
                Serializer.Serialize(file, version);
            }
            #pragma warning disable CA1031 // Do not catch general exception types
            catch (IOException)
            {
                //it will be rare but we may have multiple processes
                //attempt to write to this file at the same time
                //if that is the case there is a large chance it is 
                //all trying to write the same data
            }
            #pragma warning restore CA1031 // Do not catch general exception types
            return Task.CompletedTask;
        }

        public ContentItem GetContentItem(string name, string cultureCode)
        {
            //This is only for unit tests
            return GetAllContentItemsAsync(cultureCode)
                .GetAwaiter()
                .GetResult()
                .FirstOrDefault(i=>i.Name == name);
        }

        public Task<IEnumerable<string>> GetCultureCodesAsync(ContentVersion currentVersion=null)
        {
            var di = new DirectoryInfo(_location);
            var files = di.GetFiles("*.dat");
            return Task.FromResult(
                files.Where(s=> s.Name!="version.dat")
                    .Select( f=> f.Name.Substring( 0, f.Name.LastIndexOf(".", StringComparison.InvariantCulture)))
                );
        }
    }
}



using System.Collections.Generic;
using System.Threading.Tasks;

namespace Content.Localization
{
    public interface IContentClassGenerator
    { 
        Task GenerateAndSaveIfChangedAsync(ContentVersion version, IContentSource contentSource);

        string Generate(ContentVersion version, IEnumerable<ContentItem> items);
    }    
}
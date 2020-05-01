using Exigo.Api.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Content.Localization
{
    public class ApiContentSource : IContentSource
    {
        private readonly ApiContentSourceOptions _options;
        private readonly ExigoApiClient _apiClient;


        public ApiContentSource(HttpClient httpClient, ApiContentSourceOptions options  )
        {
            _options    = options ?? throw new ArgumentNullException(nameof(options));

            _apiClient  = new ExigoApiClient(
                        httpClient, 
                        _options.ApiUri, 
                        _options.Company, 
                        _options.LoginName, 
                        _options.Password);
        }

        public async Task<IEnumerable<string>> GetCultureCodesAsync(ContentVersion currentVersion = null)
        {
            var res = await _apiClient.GetResourceSetCulturesAsync(new GetResourceSetCulturesRequest
            {
                 SubscriptionKey    = _options.SubscriptionKey,
                 Version            = currentVersion?.Version
            }).ConfigureAwait(false);

            return res.Cultures.Select(c => c.CultureCode);
        }

        public async Task<IEnumerable<ContentItem>> GetAllContentItemsAsync(string cultureCode, ContentVersion currentVersion = null)
        {
            var req = new GetResourceSetItemsRequest
            {
                CultureCode         = (cultureCode == _options.DefaultCultureCode) ? null : cultureCode,
                SubscriptionKey     = _options.SubscriptionKey,
                Version             = currentVersion?.Version
            };

            var res     = await _apiClient.GetResourceSetItemsAsync(req).ConfigureAwait(false);

            return new List<ContentItem>( res.Items.Select(o=> new ContentItem
            {
                Name                    = o.ResourceName,
                Value                   = o.InvariantValue,
                Enabled                 = o.Enabled,
                EnabledStartDate        = o.EnabledStartDate,
                EnabledEndDate          = o.EnabledEndDate,
            }));
        }

        public async Task<ContentVersion> CheckForChangesAsync(ContentVersion currentVersion = null)
        {
            if (currentVersion is null)
            {
                throw new ArgumentNullException(nameof(currentVersion));
            }

            var req = new ResourceSetCheckInRequest
            {
                ComponentVersion        = this.GetType().Assembly.GetName().Version?.ToString(),
                EnvironmentCode         = _options.EnvironmentCode,
                HostAssemblyName        = _options.HostAssemblyName ?? Assembly.GetCallingAssembly()?.GetName().Name ?? "unknown",
                InstalledReleaseDate    = currentVersion.ReleaseDate,
                InstalledVersion        = currentVersion.Version,
                MachineName             = Environment.MachineName,
                SubscriptionKey         = _options.SubscriptionKey
            };

            var res = await _apiClient.ResourceSetCheckInAsync(req).ConfigureAwait(false);

            return new ContentVersion {  Version = res.Version, ReleaseDate = res.ReleaseDate };
        }


        public ContentItem GetContentItem(string key, string cultureCode)
        {
            throw new NotImplementedException();
        }

        public Task SaveAllContentItemsAsync(string cultureCode, IEnumerable<ContentItem> items)
        {
            return Task.CompletedTask;
        }

        public IContentSource NextSource => null;
    }


}

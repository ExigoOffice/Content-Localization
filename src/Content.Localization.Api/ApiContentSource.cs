using Exigo.Api.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Content.Localization
{
    public class ApiContentSource : IContentSource
    {
        private readonly Func<HttpClient>           _httpClientFactory;
        private readonly ApiContentSourceOptions    _options;
        private readonly IContentLogger _logger;

        public ApiContentSource(Func<HttpClient> httpClientFactory, ApiContentSourceOptions options, IContentLogger logger  )
        {
            _httpClientFactory = httpClientFactory;
            _options    = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }


        ExigoApiClient CreateApiClient()
        {
            return  new ExigoApiClient(
                        _httpClientFactory(), 
                        _options.ApiUri, 
                        _options.Company, 
                        _options.LoginName, 
                        _options.Password);
        }

        public async Task<IEnumerable<string>> GetCultureCodesAsync(ContentVersion requestedVersion)
        {
            if (requestedVersion == null)
            {
                requestedVersion = await CheckForChangesAsync()
                    .ConfigureAwait(false);
            }

            var res = await CreateApiClient().GetResourceSetCulturesAsync(new GetResourceSetCulturesRequest
            {
                 SubscriptionKey    = _options.SubscriptionKey,
                 Version            = requestedVersion.Version
            }).ConfigureAwait(false);


            var cultureList  = new List<string>(res.Cultures.Select(c => c.CultureCode));

            if (!cultureList.Contains(_options.DefaultCultureCode))
                cultureList.Add(_options.DefaultCultureCode);

            return cultureList;
        }

        public async Task<IEnumerable<ContentItem>> GetAllContentItemsAsync(string cultureCode, ContentVersion requestedVersion)
        {
            //if the configured version is null, we could be in a clean load on demand path, where we don't know the version yet
            var sw = Stopwatch.StartNew();
            if (requestedVersion == null)
            {
                requestedVersion = await CheckForChangesAsync()
                    .ConfigureAwait(false);
            }

            var req = new GetResourceSetItemsRequest
            {
                CultureCode         = (cultureCode == _options.DefaultCultureCode) ? null : cultureCode,
                SubscriptionKey     = _options.SubscriptionKey,
                Version             = requestedVersion.Version
            };

            var res     = await CreateApiClient().GetResourceSetItemsAsync(req).ConfigureAwait(false);

            _logger.LogInformation("Api GetResourceSetItems. Returned {Count} items in {Duration}", res.Items.Length, sw.Elapsed);

            return new List<ContentItem>( res.Items.Select(o=> new ContentItem
            {
                Name                    = o.ResourceName,
                Value                   = o.InvariantValue,
                Enabled                 = o.Enabled,
                EnabledStartDate        = o.EnabledStartDate,
                EnabledEndDate          = o.EnabledEndDate,
            }));
        }

        public async Task<ContentVersion> CheckForChangesAsync(ContentVersion currentVersion = null, CancellationToken token=default)
        {
            var sw = Stopwatch.StartNew();
            var req = new ResourceSetCheckInRequest
            {
                ComponentVersion        = this.GetType().Assembly.GetName().Version?.ToString(),
                EnvironmentCode         = _options.EnvironmentCode,
                HostAssemblyName        = _options.HostAssemblyName ?? Assembly.GetCallingAssembly()?.GetName().Name ?? "unknown",
                InstalledReleaseDate    = currentVersion?.ReleaseDate,
                InstalledVersion        = currentVersion?.Version,
                MachineName             = Environment.MachineName,
                SubscriptionKey         = _options.SubscriptionKey
            };

            var res = await CreateApiClient().ResourceSetCheckInAsync(req).ConfigureAwait(false);

            if (req.InstalledVersion != res.Version || req.InstalledReleaseDate != res.ReleaseDate)
            {
                _logger.LogInformation("Update started for {SubscriptionKey} - {Environment} Installed {InstalledVersion} {InstalledReleaseDate}, Server {ServerVersion} {ServerReleaseDate}",
                    req.SubscriptionKey,
                    req.EnvironmentCode,
                    req.InstalledVersion,
                    req.InstalledReleaseDate,
                    res.Version,
                    res.ReleaseDate);
            }
            else
            { 
                _logger.LogVerbose("Api Checkin Env {Environment} Installed {InstalledVersion} {InstalledReleaseDate}, Server {ServerVersion} {ServerReleaseDate}",
                    req.EnvironmentCode,
                    req.InstalledVersion,
                    req.InstalledReleaseDate,
                    res.Version,
                    res.ReleaseDate);
            }
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

        public IContentSource NextSource  { get; set;}
    }


}

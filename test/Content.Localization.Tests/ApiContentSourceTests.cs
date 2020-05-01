using Exigo.Api.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Content.Localization.Tests
{
    public sealed class ApiContentSourceTests : IDisposable
    {
        static readonly Uri _apiUri      = new Uri("https://exigodemov6-api.exigo.com/3.0/");
        const string _subscriptionKey    = "ContentTests";
        const string _environmentCode    = "dev";

        static readonly HttpClient _httpClient = new HttpClient();
        readonly IConfiguration _configuration;

        public ApiContentSourceTests()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<ApiContentSourceTests>();

            _configuration = builder.Build();
        }

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }


        [RequiredSecretsFact]
        public async Task Memory_Over_Api_Works()
        {
            //Arrange
            await SetupResourceSet();

            //Arrange
            var source = new MemoryContentSource(GetApiContentSource());

            //Assert
            Assert.Equal("ValA", source.GetContentItem("A", "en-US"));
            Assert.Equal("ValA-es", source.GetContentItem("A", "es"));
            Assert.Null(source.GetContentItem("A", "es-MX"));
        }

        [RequiredSecretsFact]
        public async Task CheckForChanges_Returns_ReleaseDate()
        {
            //Arrange
            await SetupResourceSet();

            var source = GetApiContentSource();

            //Act
            var res = await source.CheckForChangesAsync(new ContentVersion { });

            //Assert
            Assert.NotNull(res.ReleaseDate);
        }



        [RequiredSecretsFact]
        public async Task GetAllContentItems_ForDefault_CultureCode_Returns_ExpectedValue()
        {
            //Arrange
            await SetupResourceSet();
            var source = GetApiContentSource();

            //Act
            var items = await source.GetAllContentItemsAsync("en-US");

            //Assert
            Assert.Contains(items, i => i.Name == "A" );
            Assert.Contains(items, i => i.Value == "ValA" );
        }



        ExigoApiClient GetApiClient()
        {
            return new ExigoApiClient(
                        _httpClient, 
                        _apiUri, 
                        _configuration["ApiCompany"], 
                        _configuration["ApiLoginName"], 
                        _configuration["ApiPassword"]);  
        }

        ApiContentSource GetApiContentSource()
        {
            return new ApiContentSource(_httpClient, new ApiContentSourceOptions
            {
                 ApiUri             = _apiUri,
                 Company            = _configuration["ApiCompany"],
                 LoginName          = _configuration["ApiLoginName"],
                 Password           = _configuration["ApiPassword"],
                 SubscriptionKey    = _subscriptionKey,
                 EnvironmentCode    = _environmentCode
            });
        }


        private async Task SetupResourceSet()
        {
            var apiClient = GetApiClient();
            int resourceSetID;

            var existing = (await apiClient.GetResourceSetsAsync(new GetResourceSetsRequest()))
                .ResourceSets
                .FirstOrDefault(l=>l.IsDeleted == false && l.SubscriptionKey == _subscriptionKey);

            if (existing == null)
            { 
                var res = await apiClient.CreateResourceSetAsync( new CreateResourceSetRequest {
                     ClassName          = "TestClass",
                     Namespace          = "TestNameSpace",
                     SubscriptionKey    = _subscriptionKey, 
                     Description        = "TestDescription"
                });

                resourceSetID = res.ResourceSetID;
            }
            else
            { 
                resourceSetID = existing.ResourceSetID;
            }

            var cultureCodes = await apiClient.GetResourceSetCulturesAsync(new GetResourceSetCulturesRequest
            {
                 ResourceSetID = resourceSetID,
            });

            if (!cultureCodes.Cultures.Any(c => c.CultureCode == "es"))
            {
                await apiClient.CreateResourceSetCultureAsync(new CreateResourceSetCultureRequest
                {
                     CultureCode = "es",
                     ResourceSetID = resourceSetID
                });
            }

            if (!cultureCodes.Cultures.Any(c => c.CultureCode == "es-MX"))
            {
                await apiClient.CreateResourceSetCultureAsync(new CreateResourceSetCultureRequest
                {
                     CultureCode = "es-MX",
                     ResourceSetID = resourceSetID
                });
            }



            var rsdefault = await apiClient.GetResourceSetItemsAsync(new  GetResourceSetItemsRequest
            {
                ResourceSetID = resourceSetID
            });

            if (!rsdefault.Items.Any(r=>r.ResourceName == "A"))
            {
                await apiClient.CreateResourceSetTextItemAsync(new CreateResourceSetTextItemRequest
                {
                     ResourceSetID  = resourceSetID,
                     Enabled        = true,
                     ResourceName   = "A", 
                     Text           = "ValA"
                });
            }


            var rses = await apiClient.GetResourceSetItemsAsync(new  GetResourceSetItemsRequest
            {
                ResourceSetID = resourceSetID,
                CultureCode = "es"
            });

            if (!rses.Items.Any(r=>r.ResourceName == "A"))
            {
                await apiClient.CreateResourceSetTextItemAsync(new CreateResourceSetTextItemRequest
                {
                     ResourceSetID  = resourceSetID,
                     CultureCode    = "es", 
                     Enabled        = true,
                     ResourceName   = "A", 
                     Text           = "ValA-es"
                });
            }

        }

        private Task DisposeAsync()
        {
            //delete is not working right now so we will exit
            return Task.CompletedTask;
            /*
            var list = await _apiClient.GetResourceSetsAsync(new GetResourceSetsRequest());

            foreach (var item in list.ResourceSets.Where(l=>l.IsDeleted == false && l.SubscriptionKey == SubscriptionKey))
            {
                await _apiClient.RetireResourceSetAsync(new RetireResourceSetRequest {
                     ResourceSetID = item.ResourceSetID
                });
            }
            */
            
        }


        



    }
}

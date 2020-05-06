using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Content.Localization.Tests
{
    public sealed class RequiredSecretsFact : FactAttribute
    {
        public RequiredSecretsFact()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<RequiredSecretsFact>();
            var configuration = builder.Build();
            if (configuration["ApiLoginName"]==null)
            {
                Skip = "To run this test add secrets: ApiLoginName, ApiPassword and ApiCompany";
            }
        }
    }
}

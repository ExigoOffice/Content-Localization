using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Localization
{
    public class ApiContentSourceOptions
    {
        public Uri ApiUri { get; set; } 
        public string Company { get; set; }
        public string LoginName { get; set; }
        public string Password { get; set; }

        public string SubscriptionKey { get; set; }        
        public string EnvironmentCode { get; set; } = "prod";
        public string HostAssemblyName { get; set; }
        public string DefaultCultureCode { get; set; } = "en-US";
    }
}

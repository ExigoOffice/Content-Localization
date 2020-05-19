using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Localization
{
    public class ClassGeneratorOptions
    {
        public string Namespace { get; set; }   = "Resources";
        public string ClassName { get; set; }   = "Common";
        public string Location { get; set; }
        public string DefaultCultureCode { get; set; } = "en-US";
    }
}

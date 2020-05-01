using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Content.Localization
{
    [DataContract]
    public class ContentVersion
    {
        [DataMember(Order=1)]
        public string Version { get; set; }

        [DataMember(Order=2)]
        public DateTime? ReleaseDate { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Content.Localization
{
    [DataContract]
    public class ContentItem
    {
        [DataMember(Order=1)]
        public string Name { get; set; }
        [DataMember(Order=2)]
        public string Value { get; set; }
        [DataMember(Order=3)]
        public bool Enabled { get; set; }
        [DataMember(Order=4)]
        public DateTime? EnabledStartDate { get; set; }
        [DataMember(Order=5)]
        public DateTime? EnabledEndDate { get; set; }
        /// <summary>
        /// Implicitly converts the <see cref="ContentItem"/> to a <see cref="string"/>.
        /// </summary>
        /// <param name="resourceItem">The string to be implicitly converted.</param>
        public static implicit operator string(ContentItem resourceItem)
        {
            return resourceItem?.Value;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}

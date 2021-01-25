using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebSite.AspNetFramework.Models
{
    public class SomeModel
    {
        public List<Language> Languages { get; set; }
    }


    public class Language
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }
}
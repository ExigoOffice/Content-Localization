using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Content.Localization
{
    public class ContentUpdaterOptions
    {
        static readonly Random _r = new Random();

        public TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(_r.Next(5,45));
        public TimeSpan Frequency { get; set; }
    }
}

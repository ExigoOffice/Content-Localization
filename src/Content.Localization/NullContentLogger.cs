using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Localization
{
    public class NullContentLogger : IContentLogger
    {
        public void LogError(Exception ex, string format, params object[] args)
        {
            
        }

        public void LogVerbose(string format, params object[] args)
        {
            
        }
    }
}

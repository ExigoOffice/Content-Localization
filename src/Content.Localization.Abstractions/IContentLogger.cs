using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Localization
{
    public interface IContentLogger
    {
        public void LogInformation(string format, params object[] args);
        public void LogVerbose(string format, params object[] args);
        public void LogError(Exception ex, string format, params object[] args);
    }
}

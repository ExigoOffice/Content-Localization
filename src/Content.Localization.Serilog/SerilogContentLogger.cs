using System;
using Serilog;

namespace Content.Localization.Serilog
{
    public class SerilogContentLogger : IContentLogger
    {
        private readonly ILogger _logger;

        public SerilogContentLogger(ILogger logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger.ForContext("Context", "ContentLocalization");
        }

        public void LogError(Exception ex, string format, params object[] args)
        {
            _logger.Error(ex, format, args);
        }

        public void LogInformation(string format, params object[] args)
        {
            _logger.Information(format, args);
        }

        public void LogVerbose(string format, params object[] args)
        {
            _logger.Verbose(format, args);
        }
    }
}

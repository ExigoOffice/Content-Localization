using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Localization
{
    public sealed class ContentUpdater : IDisposable
    {
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        private readonly IContentSource _contentSource;
        private readonly TimeSpan _startupDelay;
        private readonly TimeSpan _interval;
        private readonly IContentLogger _logger;
        private readonly IContentClassGenerator _classGenerator;
        public ContentUpdater(
            IContentSource contentSource, 
            TimeSpan startupDelay,
            TimeSpan interval, 
            IContentLogger logger, 
            IContentClassGenerator classGenerator
            )
        {
            _contentSource = contentSource;
            _startupDelay = startupDelay;
            _interval = interval;
            _logger = logger;
            _classGenerator = classGenerator;
        }

        public Task UpdaterLoopTask { get; set; }
        public Task StartAsync()
        {
            UpdaterLoopTask = ProcessLoopAsync();
            return Task.CompletedTask;
        }

        async Task ProcessLoopAsync()
        {
            await Task.Delay(_startupDelay)
                .ConfigureAwait(false);
             
            while (!_stoppingCts.IsCancellationRequested)
            {
                try
                {
                    var version = await _contentSource.CheckForChangesAsync()
                        .ConfigureAwait(false);    
                                             
                    if (_classGenerator!=null)
                        await _classGenerator.GenerateAndSaveIfChangedAsync(version, _contentSource)
                            .ConfigureAwait(false);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Error updating content");
                }

                await Task.Delay(_interval)
                    .ConfigureAwait(false);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken=default)
        {
            try
            {
                _stoppingCts.Cancel();
            }
            finally
            {
                await Task.WhenAny(UpdaterLoopTask, Task.Delay(TimeSpan.FromMinutes(2), cancellationToken))
                    .ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _stoppingCts.Cancel();
            _stoppingCts.Dispose();
        }
    }
}

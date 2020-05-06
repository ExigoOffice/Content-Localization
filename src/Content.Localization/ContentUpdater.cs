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
        private readonly TimeSpan _interval;
        private readonly IContentLogger _logger;

        public ContentUpdater(IContentSource contentSource, TimeSpan interval, IContentLogger logger)
        {
            _contentSource = contentSource;
            _interval = interval;
            _logger = logger;
        }

        public Task UpdaterLoopTask { get; set; }
        public Task StartAsync()
        {
            UpdaterLoopTask = ProcessLoopAsync();
            return Task.CompletedTask;
        }

        async Task ProcessLoopAsync()
        {
            var r = new Random();
            await Task.Delay(TimeSpan.FromSeconds(r.Next(5, 45)))
                .ConfigureAwait(false);

            while (!_stoppingCts.IsCancellationRequested)
            {
                try
                {
                    await _contentSource.CheckForChangesAsync()
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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Producer.Services
{
    public abstract class BaseService : IHostedService
    {
        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            await DoAsync();
        }

        public virtual Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public abstract Task DoAsync();
    }
}

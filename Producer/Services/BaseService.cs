﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Producer.Services
{
    public abstract class BaseService : IHostedService
    {
        private Timer _timer;
        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            return Task.CompletedTask;
        }

        public virtual Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public abstract void DoAsync(object state);
    }
}

/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Threading;
using System.Threading.Tasks;

using MassTransit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Services
{
    public abstract class DocIntelHostedService : IHostedService
    {
        protected virtual string WorkerName => "";
        
        protected readonly IServiceProvider _serviceProvider;
        protected readonly ILogger<DocIntelHostedService> _logger;
        private readonly IHostApplicationLifetime _appLifetime;

        public DocIntelHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<DocIntelHostedService>>();
            _appLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
            serviceProvider.GetRequiredService<ILoggerFactory>();
        }
        
        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            await Init();

            try
            {
                var bus = _serviceProvider.GetRequiredService<IBusControl>();
                await bus.StartAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            _appLifetime.ApplicationStarted.Register(() =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await Run(cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.LogInformation("Task was cancelled, exiting.");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Could not run the service (" + e.GetType().Name + ": " + e.Message + ")");
                        _logger.LogError(e.ToString());
                    }
                    finally
                    {
                        // Stop the application once the work is done
                        _appLifetime.StopApplication();
                    }
                }, cancellationToken);
            });
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                var bus = _serviceProvider.GetRequiredService<IBusControl>();
                await bus.StopAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }

        protected virtual Task Init()
        {
            return Task.CompletedTask;
        }

        protected abstract Task Run(CancellationToken cancellationToken);
    }
}
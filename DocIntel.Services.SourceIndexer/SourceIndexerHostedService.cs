// 

using System;
using System.Threading;
using System.Threading.Tasks;

using DocIntel.Core.Services;

using Microsoft.Extensions.DependencyInjection;

namespace DocIntel.RabbitMQSourceIndexer
{
    internal class SourceIndexerHostedService : DocIntelHostedService
    {
        public SourceIndexerHostedService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override string WorkerName => "Source Indexer";

        protected override Task Run(CancellationToken cancellationToken)
        {
            var runner = _serviceProvider.GetService<SourceIndexer>();
            if (runner == null)
                throw new InvalidOperationException("Could not create instance of 'SourceIndexer'");
            return Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }
}
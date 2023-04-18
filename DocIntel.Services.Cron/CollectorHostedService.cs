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
using DocIntel.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DocIntel.Services.Cron
{
    class CollectorHostedService : DocIntelHostedService
    {
        protected override string WorkerName => "Document Collector";

        public CollectorHostedService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override async Task Run(CancellationToken cancellationToken)
        {
            var worker = _serviceProvider.GetService<CollectorWorker>();
            try
            {
                if (worker != null) await worker.RunAsync(cancellationToken);
                else throw new InvalidOperationException("Could not create instance of consumer");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }
}
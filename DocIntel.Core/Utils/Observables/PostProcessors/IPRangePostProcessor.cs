using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Synsharp;
using Synsharp.Forms;

namespace DocIntel.Core.Utils.Observables.PostProcessors;

public class IPRangePostProcessor : IPostProcessor
{
    private readonly IServiceProvider _serviceProvider;

    public IPRangePostProcessor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task Process(IEnumerable<SynapseObject> objects)
    {
        var synapseRepository = _serviceProvider.GetRequiredService<ISynapseRepository>();
        var ranges = await synapseRepository.GetAll<InetCidr4>().ToArrayAsync();
        
        foreach (var o in objects)
        {
            if (o is InetIPv4 inetIpV4)
                Process(inetIpV4, ranges);
        }
    }
    
    private void Process(InetIPv4 ipV4, InetCidr4[] ranges)
    {
        foreach (var range in ranges)
        {
            if (((IPNetwork)range.Value).Contains((IPAddress)ipV4.Value))
            {
                ipV4.Tags.Add(range.Tags.ToArray());
            }
        }
    }
}
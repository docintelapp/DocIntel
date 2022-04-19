using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Synsharp;
using Synsharp.Forms;

namespace DocIntel.Core.Utils.Observables.PostProcessors;

public class PrivateIpPostProcessor : IPostProcessor
{
    public Task Process(IEnumerable<SynapseObject> objects)
    {
        foreach (var o in objects)
        {
            if (o is InetIPv4 inetIpV4)
                Process(inetIpV4);
        }
        return Task.CompletedTask;
    }
    
    private static SynapseObject Process(InetIPv4 ipV4)
    {
        var privateInternets = new[]
        {
            IPNetwork.Parse("10.0.0.0/8"),
            IPNetwork.Parse("172.16.0.0/12"),
            IPNetwork.Parse("192.168.0.0/16")
        };
        
        if (privateInternets.Any(_ => _.Contains(ipV4.Value)))
        {
            ipV4.Tags.Add("net.priv");
        }

        return ipV4;
    }
}
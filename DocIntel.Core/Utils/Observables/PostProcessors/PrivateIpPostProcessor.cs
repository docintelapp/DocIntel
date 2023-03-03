using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Synsharp.Telepath.Messages;

namespace DocIntel.Core.Utils.Observables.PostProcessors;

public class PrivateIpPostProcessor : IPostProcessor
{
    public Task Process(IEnumerable<SynapseNode> objects)
    {
        foreach (var o in objects.Where(node => node.Form == "inet:ipv4"))
        {
            Process(o);
        }
        return Task.CompletedTask;
    }

    private static SynapseNode Process(SynapseNode ipV4)
    {
        var privateInternets = new[]
        {
            IPNetwork.Parse("10.0.0.0/8"),
            IPNetwork.Parse("172.16.0.0/12"),
            IPNetwork.Parse("192.168.0.0/16")
        };

        var valu = IPAddress.Parse(ipV4.Valu);
        if (privateInternets.Any(_ => _.Contains(valu)))
        {
            ipV4.Tags.Add("net.priv", new long?[]{});
        }

        return ipV4;
    }
}
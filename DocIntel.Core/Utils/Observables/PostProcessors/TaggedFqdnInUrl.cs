using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Synsharp;
using Synsharp.Forms;

namespace DocIntel.Core.Utils.Observables.PostProcessors;

public class TaggedFqdnInUrl : IPostProcessor
{
    private readonly IServiceProvider _serviceProvider;

    public TaggedFqdnInUrl(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task Process(IEnumerable<SynapseObject> objects)
    {
        var client = _serviceProvider.GetRequiredService<SynapseClient>();
        foreach (var o in objects)
        {
            if (o is InetUrl url)
                await Process(client, url);
        }
    }
    
    private async Task<SynapseObject> Process(SynapseClient synapseClient, InetUrl url)
    {
        var host = new Uri(url.Value.ToString()).Host;
        
        // If the hostname ends with a number, it is most likely an IP address
        if (Regex.Match(host, "[0-9]+$").Success) return url;
        
        var fqdn = await synapseClient.StormAsync<InetFqdn>($"inet:fqdn={host}").SingleOrDefaultAsync();
        if (fqdn != null)
        {
            foreach (var t in fqdn.Tags)
            {
                if (t.StartsWith("_di"))
                {
                    url.Tags.Add(t);
                } else if (t.StartsWith("misp"))
                {
                    url.Tags.Add(t);
                } 
            }
        }
        return url;
    }
}
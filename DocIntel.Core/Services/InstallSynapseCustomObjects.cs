using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunMethodsSequentially;
using Synsharp.Telepath;

namespace DocIntel.Core.Services;

public class InstallSynapseCustomObjects : IStartupServiceToRunSequentially
{
    public async ValueTask ApplyYourChangeAsync(IServiceProvider scopedServices)
    {
        var logger = scopedServices
            .GetRequiredService<ILogger<InstallSynapseCustomObjects>>();
            
        var synapseClient = scopedServices.GetRequiredService<TelepathClient>();

        if (logger != null) logger.LogDebug("Install custom type in Synapse");

        var proxy = await synapseClient.GetProxyAsync();
        if (logger != null) logger.LogDebug("Connected to Synapse");
        
        if (await proxy.Count("syn:form=_di:document") == 0)
        {
            // Add the custom type to synapse
            var command = @"$typeinfo = $lib.dict()
                $forminfo = $lib.dict(doc=""DocIntel Document"") 
                $lib.model.ext.addForm(_di:document, str, $typeinfo, $forminfo)";
            await proxy.CallStormAsync(command);
        }
        
        if (logger != null) logger.LogDebug("Custom type in Synapse installed");
    }

    public int OrderNum { get; }
}
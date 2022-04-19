using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunMethodsSequentially;
using Synsharp;

namespace DocIntel.Core.Services;

public class InstallSynapseCustomObjects : IStartupServiceToRunSequentially
{
    public async ValueTask ApplyYourChangeAsync(IServiceProvider scopedServices)
    {
        var logger = scopedServices
            .GetRequiredService<ILogger<InstallSynapseCustomObjects>>();
            
        var synapseClient = scopedServices.GetRequiredService<SynapseClient>();

        if (logger != null) logger.LogDebug("Install custom type in Synapse");

        if (await synapseClient.StormAsync<SynapseObject>("syn:form=_di:document").CountAsync() == 0)
        {
            // Add the custom type to synapse
            var command = @"$typeinfo = $lib.dict()
                $forminfo = $lib.dict(doc=""DocIntel Document"") 
                $lib.model.ext.addForm(_di:document, str, $typeinfo, $forminfo)";   
            await synapseClient!.StormCallAsync(command);
        }
    }

    public int OrderNum { get; }
}
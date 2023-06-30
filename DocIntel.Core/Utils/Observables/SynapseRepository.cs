using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Models;
using Microsoft.Extensions.Logging;
using Synsharp.Telepath;
using Synsharp.Telepath.Helpers;
using Synsharp.Telepath.Messages;

namespace DocIntel.Core.Utils.Observables;

public class SynapseRepository : ISynapseRepository
{
    private readonly TelepathClient _client;
    private readonly ILogger<SynapseRepository> _logger;

    private readonly NodeHelper _nodeHelper;
    private readonly ViewHelper _viewHelper;

    public SynapseRepository(TelepathClient client, ILoggerFactory loggerFactory)
    {
        _client = client;
        _nodeHelper = new NodeHelper(_client, loggerFactory.CreateLogger<NodeHelper>());
        _viewHelper = new ViewHelper(_client, loggerFactory.CreateLogger<ViewHelper>());
        _logger = loggerFactory.CreateLogger<SynapseRepository>();
    }

    public Task<SynapseNode> Add(SynapseNode synapseNode)
    {
        return _nodeHelper.AddAsync(synapseNode);
    }

    public async Task Add(IEnumerable<SynapseNode> observables, Document document,
        SynapseView view = null)
    {
        
        var stormOps = new StormOps() { View = view?.Iden, Repr = true };

        var docNode = new SynapseNode()
        {
            Form = "_di:document",
            Valu = document.DocumentId.ToString()
        };
        docNode = await _nodeHelper.AddAsync(docNode, stormOps);
        _logger.LogDebug($"Adding node {docNode.Form}={docNode.Valu} to {stormOps.View ?? ""}");
        
        var nodes = observables.Where(_ => !_.Tags.ContainsKey("_di.workflow.ignore")).ToList();

        if (nodes.Any())
        {
            SynapseNode[] addedNodes = await _nodeHelper.AddAsync(nodes, stormOps).ToArrayAsync();
            var connected = await _nodeHelper.AddLightEdgeAsync(addedNodes, "refs", docNode, stormOps).ToListAsync();
        }
        else
        {
            _logger.LogDebug("No observables to add");
        }
    }

    public Task Add(SynapseNode synapseNode, Document document, SynapseView view = null)
    {
        return Add(new[] { synapseNode }, document, view);
    }

    public async Task Remove(Guid documentId, SynapseView view = null)
    {   
        var stormOps = new StormOps() { View = view?.Iden, Repr = true };

        var docNode = new SynapseNode()
        {
            Form = "_di:document",
            Valu = documentId
        };
        await _nodeHelper.DeleteAsync(docNode, stormOps);
    }

    public async Task<SynapseView> CreateView(Document document)
    {
        var name = GetViewName(document);
        var view = ((await _viewHelper.List()) ?? Array.Empty<SynapseView>()).FirstOrDefault(_ => _.Name == name);
        
        if (view != null)
            return view;
        return await _viewHelper.Fork("", name);
    }

    public async Task RemoveView(Document document)
    {
        var name = GetViewName(document);
        var view = ((await _viewHelper.List()) ?? Array.Empty<SynapseView>()).FirstOrDefault(_ => _.Name == name);
        
        if (view != null)
            await _viewHelper.Delete(view.Iden);
    }

    public async IAsyncEnumerable<SynapseNode> GetObservables(Document document, bool unmerged = false, bool includeIgnore = true)
    {
        var view = await GetViewAsync(document, unmerged);
        var filter = "";
        if (!includeIgnore)
            filter = " -#_di.workflow.ignore ";
        
        var stormOps = new StormOps
        {
            View = view?.Iden, 
            Repr = true,
            Vars = new Dictionary<string, dynamic> { { "documentId", document.DocumentId.ToString() } }
        };

        var proxy = await _client.GetProxyAsync();
        await foreach (var element in proxy.Storm($"_di:document=$documentId -(refs)> * {filter}", stormOps).OfType<SynapseNode>())
            yield return element;
    }

    public async Task Remove(Document document, string iden, bool unmerged = false, bool softDelete = false)
    {
        var view = await GetViewAsync(document, unmerged);
        var stormOps = new StormOps() { View = view?.Iden, Repr = true };
        
        var docNode = new SynapseNode()
        {
            Form = "_di:document",
            Valu = document.DocumentId.ToString()
        };
        docNode = await _nodeHelper.AddAsync(docNode, stormOps);

        if (docNode != null) 
            await _nodeHelper.RemoveLightEdgeAsync(new []{iden}, "refs", docNode, stormOps);
        if (!softDelete)
            await _nodeHelper.DeleteAsync(iden, stormOps);
    }

    public async Task AddTag(Document document, string iden, string tagName, bool unmerged = false)
    {
        var view = await GetViewAsync(document, unmerged);
        var stormOps = new StormOps() { View = view?.Iden, Repr = true };
        
        await _nodeHelper.AddTag(iden, tagName, stormOps);
    }

    public async Task RemoveTag(Document document, string iden, string tagName, bool unmerged = false)
    {
        var view = await GetViewAsync(document, unmerged);
        var stormOps = new StormOps() { View = view?.Iden, Repr = true };
        
        await _nodeHelper.RemoveTag(iden, tagName, stormOps);
    }

    public async Task<SynapseNode> GetObservableByIden(string iden)
    {
        var stormOps = new StormOps() { Repr = true };
        return await _nodeHelper.GetAsync(iden, stormOps);
    }

    public async Task<SynapseNode> GetObservableByIden(Document document, string iden, bool unmerged = false)
    {
        var view = await GetViewAsync(document, unmerged);
        var stormOps = new StormOps() { View = view?.Iden, Repr = true };
        return await _nodeHelper.GetAsync(iden, stormOps);
    }

    public async Task Merge(Document document, bool delete = true)
    {
        var view = await GetViewAsync(document, true);
        if (view != null)
        {
            _logger.LogInformation("Document view merged");
            await _viewHelper.Merge(view.Iden);
            if (delete)
                await _viewHelper.Delete(view.Iden);
        }
        else
        {
            _logger.LogInformation("Document view merged");   
        }
    }

    public async Task RemoveRefDataWithProperty(Document document, string property, object value,
        bool unmerged = false)
    {
        var view = await GetViewAsync(document, unmerged);
        var stormOps = new StormOps()
        {
            View = view?.Iden,
            Repr = true,
            Vars = new Dictionary<string, dynamic>()
            {
                { "documentId", document.DocumentId },
                { "property", property },
                { "$value", value }
            }
        };
        
        var proxy = await _client.GetProxyAsync();
        await proxy.Storm($"_di:document=$documentId -(refs)> * +property=$value | delnode", stormOps).ToListAsync();
    }

    public async Task<string[]> GetSimpleForms()
    {
        var stormOps = new StormOps();
        var proxy = await _client.GetProxyAsync();
        return (await proxy.Storm("syn:type:ctor = synapse.lib.types.Int "
                          + "syn:type:ctor = synapse.lib.types.Str "
                          + "syn:type:ctor = synapse.lib.types.Bool "
                          + "syn:type:ctor = synapse.lib.types.Hex "
                          + "syn:type:ctor = synapse.lib.types.HugeNum "
                          + "syn:type:ctor = synapse.lib.types.Float "
                          + "syn:type:ctor = synapse.lib.types.Loc "
                          + "syn:type:ctor = synapse.lib.types.Taxon "
                          + "syn:type:ctor = synapse.lib.types.Taxonomy "
                          + "syn:type:ctor = synapse.lib.types.Velocity "
                          + "syn:type:ctor = synapse.lib.types.Duration "
                          + "syn:type:ctor = synapse.models.geospace.Area "
                          + "syn:type:ctor = synapse.models.geospace.Dist " 
                          + "syn:type:ctor = synapse.models.geospace.LatLong "
                          + "syn:type:ctor = synapse.models.inet.Addr "
                          + "syn:type:ctor = synapse.models.inet.Cidr4 "
                          + "syn:type:ctor = synapse.models.inet.Cidr6 "
                          + "syn:type:ctor = synapse.models.dns.DnsName "
                          + "syn:type:ctor = synapse.models.inet.Email "
                          + "syn:type:ctor = synapse.models.inet.Fqdn "
                          + "syn:type:ctor = synapse.models.inet.IPv4 "
                          + "syn:type:ctor = synapse.models.inet.IPv4Range "
                          + "syn:type:ctor = synapse.models.inet.IPv6 "
                          + "syn:type:ctor = synapse.models.inet.IPv6Range "
                          + "syn:type:ctor = synapse.models.inet.Rfc2822Addr "
                          + "syn:type:ctor = synapse.models.inet.Url "
                          + "syn:type:ctor = synapse.models.infotech.Cpe23Str "
                          + "syn:type:ctor = synapse.models.infotech.Cpe22Str "
                          + "syn:type:ctor = synapse.models.infotech.SemVer "
                          + "syn:type:ctor = synapse.models.telco.Imei "
                          + "syn:type:ctor = synapse.models.telco.Imsi "
                          + "syn:type:ctor = synapse.models.telco.Phone " 
                          + " -> syn:form:type -:type^=_", stormOps)
            .ToListAsync())
            .OfType<SynapseNode>()
            .Select(_ => (string) _?.Valu?.ToString() ?? null)
            .Where(_ => _ != null)
            .ToArray();
    }

    public async Task RemoveRefs(Guid documentId, SynapseView view = null)
    {
        var stormOps = new StormOps() { View = view?.Iden, Repr = true };
        // TOOO Does not really delete the observables, just the link.
        // TODO What needs to be deleted? Because an observable might linked or edited manually.
        
        var docNode = new SynapseNode()
        {
            Form = "_di:document",
            Valu = documentId.ToString()
        };
        await _nodeHelper.DeleteAsync(docNode, stormOps);
    }

    private string GetViewName(Document document) => "document-" + document.DocumentId.ToString();

    private async Task<SynapseView> GetViewAsync(Document document, bool unmerged = false)
    {
        var viewName = unmerged ? GetViewName(document) : null;
        var view = (await _viewHelper.List() ?? Array.Empty<SynapseView>()).FirstOrDefault(_ => _.Name == viewName);
        return view;
    }
}

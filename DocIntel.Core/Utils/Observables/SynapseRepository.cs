using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocIntel.Core.Models;
using DocIntel.Core.Utils.Observables.CustomObjects;
using Microsoft.Extensions.Logging;
using Synsharp;

namespace DocIntel.Core.Utils.Observables;

public class SynapseRepository : ISynapseRepository
{
    private readonly SynapseClient _client;
    private bool _connected = false;
    private readonly ILogger<SynapseRepository> _logger;

    public SynapseRepository(SynapseClient client, ILogger<SynapseRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public Task<SynapseObject> Add(SynapseObject synapseObject)
    {
        return _client.Nodes.Add(synapseObject);
    }

    public async Task Add(IEnumerable<SynapseObject> observables, Document document, DocumentFile file,
        SynapseView view = null)
    {
        await ensureLoggedIn();
        var doc = (await _client.StormAsync<DIDocumentSynapseObject>(
            $"[ _di:document={Synsharp.StringHelpers.Escape(document.DocumentId.ToString())} ]",
            new ApiStormQueryOpts() { View = view?.Iden })
            .ToListAsync()).FirstOrDefault();

        var synapseObject = observables.Where(_ => !_.Tags.Contains("_di.workflow.ignore")).ToList();
        
        await _client.Nodes.Add(synapseObject, view?.Iden).ToListAsync();
        await _client.Nodes.AddLightEdge(synapseObject.Select(_ => new SynapseLightEdge(doc, _, "refs")), view?.Iden);
    }
    
    public async Task Add(SynapseObject synapseObject, Document document, SynapseView view = null)
    {
        await ensureLoggedIn();
        var doc = (await _client.StormAsync<DIDocumentSynapseObject>(
                $"[ _di:document={Synsharp.StringHelpers.Escape(document.DocumentId.ToString())} ]",
                new ApiStormQueryOpts() { View = view?.Iden })
            .ToListAsync()).FirstOrDefault();

        await _client.Nodes.Add(synapseObject, view?.Iden);
        await _client.Nodes.AddLightEdge(new SynapseLightEdge(doc, synapseObject, "refs"), view?.Iden);
    }
    
    public async Task Remove(string iden, Document document, SynapseView view = null)
    {
        await ensureLoggedIn();
        var doc = (await _client.StormAsync<DIDocumentSynapseObject>(
                $"[ _di:document={Synsharp.StringHelpers.Escape(document.DocumentId.ToString())} ]",
                new ApiStormQueryOpts() { View = view?.Iden })
            .ToListAsync()).FirstOrDefault();

        var node = await _client.Nodes.GetAsync<SynapseObject>(iden, view?.Iden);
        if (node != null)
        {
            await _client.Nodes.RemoveLightEdge(new SynapseLightEdge(doc, node, "refs"), view?.Iden);   
        }
    }

    public async Task RemoveRefs(Guid documentId)
    {
        // TOOO Does not really delete the observables, just the link.
        // TODO What needs to be deleted? Because an observable might linked or edited manually.
        await ensureLoggedIn();
        var doc = (await _client.StormAsync<DIDocumentSynapseObject>(
                $" _di:document={Synsharp.StringHelpers.Escape(documentId.ToString())} | delnode")
            .ToListAsync());
    }

    private string GetViewName(Document document) => "document-" + document.DocumentId.ToString();

    public async Task<SynapseView> CreateView(Document document)
    {
        var name = GetViewName(document);
        var view = (await _client.View.List()).FirstOrDefault(_ => _.Name == name);
        
        if (view != null)
            return view;
        return await _client.View.Fork("", name);
    }
    public async Task RemoveView(Document document)
    {
        var name = GetViewName(document);
        var view = (await _client.View.List()).FirstOrDefault(_ => _.Name == name);
        
        if (view != null)
            await _client.View.Delete(view.Iden);
    }

    public IAsyncEnumerable<SynapseObject> GetObservables(Document document, bool unmerged = false, bool includeIgnore = true)
    {
        var view = GetView(document, unmerged);
        var filter = "";
        if (!includeIgnore)
            filter = " -#_di.workflow.ignore ";
        return _client.StormAsync<SynapseObject>(
            $"_di:document={Synsharp.StringHelpers.Escape(document.DocumentId.ToString())} -(refs)> * {filter}", 
            new ApiStormQueryOpts() { View = view?.Iden });
    }

    private SynapseView GetView(Document document, bool unmerged = false)
    {
        string viewName = unmerged ? GetViewName(document) : null;
        var view = _client.View.List().Result.FirstOrDefault(_ => _.Name == viewName);
        return view;
    }

    public async Task Remove(Document document, string iden, bool unmerged = false, bool softDelete = false)
    {
        var view = GetView(document, unmerged);
        
        var doc = (await _client.StormAsync<DIDocumentSynapseObject>(
                $"[ _di:document={Synsharp.StringHelpers.Escape(document.DocumentId.ToString())} ]",
                new ApiStormQueryOpts() { View = view?.Iden })
            .ToListAsync()).FirstOrDefault();

        var obs = await _client.Nodes.GetAsync<SynapseObject>(iden, view?.Iden);
        if (doc != null && obs != null) 
            await _client.Nodes.RemoveLightEdge(doc, obs, "refs", view?.Iden);
        if (!softDelete)
            await _client.Nodes.Remove(iden, view?.Iden);
    }

    public async Task AddTag(Document document, string iden, string tagName, bool unmerged = false)
    {
        var view = GetView(document, unmerged);
        await ensureLoggedIn();
        await _client.Nodes.AddTag(iden, tagName, view?.Iden);
    }

    public async Task RemoveTag(Document document, string iden, string tagName, bool unmerged = false)
    {
        var view = GetView(document, unmerged);
        await ensureLoggedIn();
        await _client.Nodes.RemoveTag(iden, tagName, view?.Iden);
    }

    public Task<SynapseObject> GetObservableByIden(string iden)
    {
        return _client.Nodes.GetAsync<SynapseObject>(iden);
    }

    public Task<T> GetObservableByIden<T>(Document document, string iden, bool unmerged = false)
    {
        var view = GetView(document, unmerged);
        return _client.Nodes.GetAsync<T>(iden, view?.Iden);
    }

    public IAsyncEnumerable<T> GetBySecondary<T>(Document document, string property, string coreValue, bool unmerged = false)
    {
        var view = GetView(document, unmerged);
        return _client.Nodes.GetAsyncByProperty<T>(new Dictionary<string, string>() { {property, coreValue}}, view?.Iden);
    }

    public async Task Merge(Document document, bool delete = true)
    {
        var view = GetView(document, true);
        if (view != null)
        {
            _logger.LogInformation("Document view merged");
            await _client.View.Merge(view.Iden);
            if (delete)
                await _client.View.Delete(view.Iden);
        }
        else
        {
            _logger.LogInformation("Document view merged");   
        }
    }

    private async Task ensureLoggedIn()
    {
        if (!_connected)
        {
            await _client.LoginAsync("user", "password");
            _connected = true;
        }
    }

    public IAsyncEnumerable<T> GetAll<T>() where T: SynapseObject
    {
        return _client.Nodes.GetAllAsync<T>();
    }
}
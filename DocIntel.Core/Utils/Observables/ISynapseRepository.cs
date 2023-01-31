using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DocIntel.Core.Models;
using Synsharp.Telepath.Helpers;
using Synsharp.Telepath.Messages;

namespace DocIntel.Core.Utils.Observables;

public interface ISynapseRepository
{
    Task<SynapseNode> Add(SynapseNode observable);
    Task Add(IEnumerable<SynapseNode> observables, Document document, SynapseView view = null);
    Task Add(SynapseNode synapseNode, Document document, SynapseView view = null);
    Task Remove(Guid documentId, SynapseView view = null);
    Task Remove(Document document, string iden, bool unmerged = false, bool softDelete = false);
    Task RemoveView(Document document);
    Task<SynapseView> CreateView(Document document);
    IAsyncEnumerable<SynapseNode> GetObservables(Document document, bool unmerged = false, bool includeIgnore = true);
    Task AddTag(Document document, string iden, string tagName, bool unmerged = false);
    Task RemoveTag(Document document, string iden, string tagName, bool unmerged = false);
    Task<SynapseNode> GetObservableByIden(string iden);
    Task<SynapseNode> GetObservableByIden(Document document, string iden, bool unmerged = false);
    Task Merge(Document document, bool delete = true);
    Task RemoveRefDataWithProperty(Document document, string property, object value,
        bool unmerged = false);
    Task<string[]> GetSimpleForms();
}

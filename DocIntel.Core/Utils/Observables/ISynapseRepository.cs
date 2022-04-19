using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DocIntel.Core.Models;
using Synsharp;
using Synsharp.Types;

namespace DocIntel.Core.Utils.Observables;

public interface ISynapseRepository
{
    Task Add(SynapseObject observable);
    Task Add(IEnumerable<SynapseObject> observables, Document document, DocumentFile file, SynapseView view = null);
    Task RemoveRefs(Guid document);
    Task RemoveView(Document document);
    Task<SynapseView> CreateView(Document document);
    IAsyncEnumerable<SynapseObject> GetObservables(Document document, bool unmerged = false, bool includeIgnore = true);
    Task Remove(Document document, string iden, bool unmerged = false, bool softDelete = false);
    Task AddTag(Document document, string iden, string tagName, bool unmerged = false);
    Task RemoveTag(Document document, string iden, string tagName, bool unmerged = false);
    Task<T> GetObservableByIden<T>(Document document, string iden, bool unmerged = false);
    IAsyncEnumerable<T> GetBySecondary<T>(Document document, string property, string coreValue, bool unmerged = false);
    Task Merge(Document document, bool delete = true);
    IAsyncEnumerable<T> GetAll<T>() where T : SynapseObject;
}

using System.Collections.Generic;
using System.Threading.Tasks;
using DocIntel.Core.Models;
using Synsharp;
using Synsharp.Telepath.Messages;

namespace DocIntel.WebApp.Areas.Synapse.Views.Node;

public class NodeDetailsViewModel
{
    public SynapseNode Root { get; set; }
    public List<Document> ReferencingDocs { get; set; }
    public int PageCount { get; set; }
    public int Page { get; set; }
    public int ReferencingDocsCount { get; set; }
}
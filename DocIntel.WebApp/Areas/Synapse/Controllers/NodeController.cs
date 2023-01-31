using System;
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Search.Documents;
using DocIntel.WebApp.Areas.Synapse.Views.Node;
using DocIntel.WebApp.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Synsharp.Telepath;
using Synsharp.Telepath.Helpers;
using Synsharp.Telepath.Messages;

namespace DocIntel.WebApp.Areas.Synapse.Controllers;

[Area("Synapse")]
[Route("Synapse/Node")]
public class NodeController : BaseController
{
    private readonly TelepathClient _synapseClient;
    private readonly IDocumentRepository _documentRepository;
    private readonly NodeHelper _nodeHelper;

    public NodeController(DocIntelContext context,
        AppUserManager userManager,
        ApplicationSettings configuration,
        IAuthorizationService authorizationService,
        TelepathClient synapseClient,
        IDocumentRepository documentRepository, ILoggerFactory loggerFactor) : base(context,
        userManager,
        configuration,
        authorizationService)
    {
        _synapseClient = synapseClient;
        _nodeHelper = new NodeHelper(_synapseClient, loggerFactor.CreateLogger<NodeHelper>());
        _documentRepository = documentRepository;
    }
    
    [HttpGet("Details/{iden}")]
    public async Task<IActionResult> Details(string iden, int page = 0)
    {
        var SynapseNode = await _nodeHelper.GetAsync(iden, new StormOps() { Repr = true });
        if (SynapseNode == null) return NotFound();

        var proxy = await _synapseClient.GetProxyAsync();
        var docIds = (await proxy.Storm("<(refs)- _di:document",
                    new StormOps
                    {
                        Idens = new[]
                        {
                            iden
                        }, 
                        Repr = true
                    })
                .ToListAsync())
            .OfType<SynapseNode>()
            .Where(_ => _.Form == "_di:document")
            .Select(_ => (Guid) Guid.Parse(_.Valu))
            .ToArray();
        
        DocumentQuery query = new()
        {
            DocumentIds = docIds,
            Page = page,
            Limit = 10,
            OrderBy = SortCriteria.DocumentDate
        };
        var docs = _documentRepository.GetAllAsync(AmbientContext,
                query,
                new[]
                {
                    "DocumentTags",
                    "DocumentTags.Tag",
                    "DocumentTags.Tag.Facet",
                    "Source"
                })
            .ToListAsync();
        
        var viewModel = new NodeDetailsViewModel
        {
            Root = SynapseNode,
            ReferencingDocs = await docs,
            ReferencingDocsCount = docIds.Length,
            Page = page,
            PageCount = (int)Math.Ceiling(docIds.Length / 10.0)
        };
        
        return View("Details", viewModel);
    }
}
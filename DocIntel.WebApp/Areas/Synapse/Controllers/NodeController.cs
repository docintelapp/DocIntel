using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Observables.CustomObjects;
using DocIntel.Core.Utils.Search.Documents;
using DocIntel.WebApp.Areas.Synapse.Views.Node;
using DocIntel.WebApp.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Synsharp;
using Synsharp.Attribute;

namespace DocIntel.WebApp.Areas.Synapse.Controllers;

[Area("Synapse")]
[Route("Synapse/Node")]
public class NodeController : BaseController
{
    private readonly SynapseClient _synapseClient;
    private readonly IDocumentRepository _documentRepository;

    public NodeController(DocIntelContext context, UserManager<AppUser> userManager, ApplicationSettings configuration, IAuthorizationService authorizationService, SynapseClient synapseClient, IDocumentRepository documentRepository) : base(context, userManager, configuration, authorizationService)
    {
        _synapseClient = synapseClient;
        _documentRepository = documentRepository;
    }
    
    [HttpGet("Details/{iden}")]
    public async Task<IActionResult> Details(string iden, int page = 0)
    {
        var synapseObject = await _synapseClient.Nodes.GetAsync<SynapseObject>(iden);
        if (synapseObject == null) return NotFound();
        
        var synapseType = synapseObject.GetType();
        var synapseFormAttribute = synapseType.GetCustomAttribute<SynapseFormAttribute>();
        if (synapseFormAttribute == null) return NotFound();
        
        var docIds = (await _synapseClient.StormAsync<DIDocumentSynapseObject>("<(refs)- _di:document",
                    new ApiStormQueryOpts()
                    {
                        Idens = new[]
                        {
                            iden
                        }
                    })
                .ToListAsync())
            .Select(_ => Guid.Parse(_.Value))
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
            Root = synapseObject,
            ReferencingDocs = await docs,
            ReferencingDocsCount = docIds.Length,
            Page = page,
            PageCount = (int)Math.Ceiling(docIds.Length / 10.0)
        };
        
        return View("Details", viewModel);
    }
}
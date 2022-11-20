/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau, Kevin Menten
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using AutoMapper;

using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Helpers;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Utils;
using DocIntel.Core.Utils.Observables;
using DocIntel.Core.Utils.Search.Documents;
using DocIntel.WebApp.Areas.API.Models;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json.Linq;

namespace DocIntel.WebApp.Areas.API.Controllers
{
    [Area("API")]
    [Route("API/Document")]
    [ApiController]
    public class DocumentController : DocIntelAPIControllerBase
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAppAuthorizationService _appAuthorizationService;
        private readonly IClassificationRepository _classificationRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly TagUtility _tagUtility;

        private readonly IDocumentRepository _documentRepository;
        private readonly ITagFacetRepository _facetRepository;
        private readonly ILogger<DocumentController> _logger;
        private readonly IMapper _mapper;
        private readonly ISynapseRepository _synapseRepository;
        private readonly ISourceRepository _sourceRepository;
        private readonly ITagRepository _tagRepository;

        public DocumentController(UserManager<AppUser> userManager,
            DocIntelContext context,
            IOptions<IdentityOptions> identityOptions,
            IAppAuthorizationService appAuthorizationService,
            IDocumentRepository documentRepository,
            ILogger<DocumentController> logger,
            IMapper mapper,
            IHttpContextAccessor accessor,
            ITagRepository tagRepository,
            ISourceRepository sourceRepository,
            ITagFacetRepository facetRepository,
            ISynapseRepository synapseRepository,
            IClassificationRepository classificationRepository,
            ICommentRepository commentRepository, IHttpContextAccessor httpContextAccessor, TagUtility tagUtility)
            : base(userManager, context)
        {
            _appAuthorizationService = appAuthorizationService;
            _documentRepository = documentRepository;
            _logger = logger;
            _mapper = mapper;
            _accessor = accessor;
            _tagRepository = tagRepository;
            _facetRepository = facetRepository;
            _commentRepository = commentRepository;
            _tagUtility = tagUtility;
            _synapseRepository = synapseRepository;
            _sourceRepository = sourceRepository;
            _classificationRepository = classificationRepository;
        }

        // TODO How can we upload multiple files to a document?
        // TODO Call should be split in 02: One for creating a document and one to upload a file. 
        [HttpPost("")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIDocument))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ModelStateDictionary))]
        [Produces("application/json", "application/xml")]
        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
        [RequestSizeLimit(209715200)]
        public async Task<IActionResult> Upload([FromForm] APIDocumentUpload submittedDocument,
            [Bind(Prefix = "file")] IFormFile file)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                if (file == null)
                    ModelState.AddModelError("file", "Submit a file.");

                if (!ModelState.IsValid) throw new InvalidArgumentException(ModelState);

                Debug.Assert(file != null, nameof(file) + " != null");
                var doc = new Document
                {
                    Title = submittedDocument is null || submittedDocument.Title.IsNullOrEmpty()
                        ? file.FileName.EndsWith(".pdf")
                            ? file.FileName.Remove(file.FileName.Length - 4)
                            : file.FileName
                        : submittedDocument.Title,
                    Status = DocumentStatus.Submitted
                };
                if (submittedDocument != null)
                {
                    if (submittedDocument.SkipObservables)
                    {
                        doc.MetaData ??= new JObject();
                        doc.MetaData["ExtractObservables"] = false;
                    }

                    doc.ExternalReference = submittedDocument.ExternalReference;
                    doc.ShortDescription = submittedDocument.ShortDescription;
                    if (!string.IsNullOrEmpty(submittedDocument.SourceName))
                        try
                        {
                            var result = await _sourceRepository.GetAsync(AmbientContext, submittedDocument.SourceName);
                            doc.Source = result;
                        }
                        catch (NotFoundEntityException)
                        {
                            var source = await _sourceRepository.CreateAsync(AmbientContext,
                                new Source {Title = submittedDocument.SourceName});
                            _logger.Log(LogLevel.Information,
                                EventIDs.APICreateSourceSuccessful,
                                new LogEvent($"User '{currentUser.UserName}' successfully created a new source.")
                                    .AddUser(currentUser)
                                    .AddHttpContext(_accessor.HttpContext)
                                    .AddSource(source),
                                null,
                                LogEvent.Formatter);
                            doc.Source = source;
                        }

                    if (submittedDocument.SourceId != Guid.Empty)
                        doc.SourceId = submittedDocument.SourceId;
                    doc.SourceUrl = submittedDocument.SourceUrl;
                    if (submittedDocument.ClassificationId != Guid.Empty)
                    {
                        var classification = await _classificationRepository.GetAsync(AmbientContext,
                            submittedDocument.ClassificationId);
                        if (classification != null)
                            doc.Classification = classification;
                    }

                    doc.Note = submittedDocument.Note;
                    doc.RegistrationDate = submittedDocument.RegistrationDate == DateTime.MinValue
                        ? DateTime.UtcNow
                        : submittedDocument.RegistrationDate;
                    doc.ModificationDate = submittedDocument.ModificationDate == DateTime.MinValue
                        ? DateTime.UtcNow
                        : submittedDocument.ModificationDate;
                    doc.DocumentDate = submittedDocument.DocumentDate == DateTime.MinValue
                        ? DateTime.UtcNow
                        : submittedDocument.DocumentDate;
                    doc.Status = (DocumentStatus) submittedDocument.Status;
                }

                ISet<Tag> filteredTags = new SortedSet<Tag>();
                if (submittedDocument is {Tags: { }} && submittedDocument.Tags
                    .Where(documentTag => !documentTag.IsNullOrEmpty()).ToArray().Length > 0)
                    filteredTags = await GetTags(submittedDocument.Tags.Where(u => !u.IsNullOrEmpty()).ToArray(),
                        currentUser);

                await using var stream = file.OpenReadStream();

                var document = await _documentRepository.AddAsync(AmbientContext, doc, filteredTags.ToArray());
                await _documentRepository.AddFile(
                    AmbientContext,
                    new DocumentFile
                    {
                        Filename = file.FileName,
                        Title = file.Name, //TODO correct?
                        Document = document,
                        DocumentId = document.DocumentId,
                        DocumentDate = document.DocumentDate,
                        Classification = document.Classification,
                        SourceUrl = document.SourceUrl, //TODO this doesn't seem ok?
                        Preview = true,
                        Visible = true
                    },
                    stream
                );

                await _context.SaveChangesAsync();
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIUploadDocumentSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully uploaded document '{document.DocumentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", document.DocumentId),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APIDocument>(document));
            }

            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIRegisterDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to register a document without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (FileAlreadyKnownException e)
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.APIRegisterDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to register a document that was already known.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.reference", e.ExistingReference),
                    null,
                    LogEvent.Formatter);
                ModelState.AddModelError("error", "This document is already known");
                return BadRequest(ModelState);
            }
            catch (TitleAlreadyExistsException e)
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.APIRegisterDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to register a document with a title already known.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.reference", e.ExistingReference),
                    null,
                    LogEvent.Formatter);
                ModelState.AddModelError("error", "A document with this title already exists");
                return BadRequest(ModelState);
            }
            catch (InvalidArgumentException e)
            {
                ModelState.Clear();
                foreach (var kv in e.Errors)
                foreach (var errorMessage in kv.Value)
                    ModelState.AddModelError(kv.Key, errorMessage);
                ModelState.AddModelError("error", "Invalid fields");
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIRegisterDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to register a document with invalid fields.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return BadRequest(ModelState);
            }
        }

        [HttpGet("Pending")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Pending(int page = 1, int pageSize = 20)
        {
            var currentUser = await GetCurrentUser();
            var pendingDocuments = _documentRepository.GetAllAsync(AmbientContext, new DocumentQuery
                {
                    Statuses = new[] {DocumentStatus.Submitted, DocumentStatus.Analyzed}.ToHashSet(),
                    Page = page,
                    Limit = pageSize,
                    OrderBy = SortCriteria.RegistrationDate
                },
                new[] {nameof(Document.RegisteredBy), nameof(Document.LastModifiedBy)}).ToEnumerable();

            _logger.Log(LogLevel.Information,
                EventIDs.APIPendingDocumentSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully listed pending documents.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Ok(new
            {
                page,
                documents = _mapper.Map<IEnumerable<APIDocument>>(pendingDocuments)
            });
        }

        [HttpPost("Discard")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Discard(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var document = await _documentRepository.GetAsync(AmbientContext, id);
                if ((document.Status != DocumentStatus.Analyzed) & (document.Status != DocumentStatus.Submitted))
                    return Unauthorized();

                await _documentRepository.RemoveAsync(AmbientContext, document.DocumentId);
                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.APIDiscardDocumentSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully discarded document '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddDocument(document),
                    null,
                    LogEvent.Formatter);

                return Ok();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDiscardDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to discard document '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDiscardDocumentFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to discard a non-existing document '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("Register")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIDocument))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ModelStateDictionary))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Register([FromBody] APIDocument submittedDocument,
            [FromQuery] [Bind(Prefix = "tags")] string[] tagLabels)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var document = await _documentRepository.GetAsync(AmbientContext,
                    submittedDocument.DocumentId);

                if (submittedDocument.DocumentDate.Year == 1
                    && submittedDocument.DocumentDate.Month == 1
                    && submittedDocument.DocumentDate.Day == 1)
                    ModelState.AddModelError("DocumentDate", "The date looks suspicious.");

                var filteredTags = await GetTags(tagLabels, currentUser);
                document.SourceId = submittedDocument.Source.SourceId;

                document.Title = submittedDocument.Title;
                document.ExternalReference = submittedDocument.ExternalReference;
                document.ShortDescription = submittedDocument.ShortDescription;
                document.Note = submittedDocument.Note;
                document.Status = DocumentStatus.Registered;

                foreach (var f in document.Files)
                {
                    f.DocumentDate = submittedDocument.DocumentDate;
                    f.Classification = submittedDocument.Classification;
                    f.SourceUrl = submittedDocument.SourceUrl;
                }

                if (ModelState.IsValid)
                {
                    var updatedDoc = await _documentRepository.UpdateAsync(AmbientContext, document, filteredTags);

                    await _context.SaveChangesAsync();

                    _logger.Log(LogLevel.Warning,
                        EventIDs.APIRegisterDocumentSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully registered document '{submittedDocument.DocumentId}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddProperty("document.id", submittedDocument.DocumentId),
                        null,
                        LogEvent.Formatter);

                    return Ok(_mapper.Map<APIDocument>(updatedDoc));
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIRegisterDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to register a document '{submittedDocument.DocumentId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", submittedDocument.DocumentId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIRegisterDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to register a non-existing document '{submittedDocument.DocumentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", submittedDocument.DocumentId),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
            catch (FileAlreadyKnownException e)
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.APIRegisterDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to register a document that was already known.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.reference", e.ExistingReference),
                    null,
                    LogEvent.Formatter);
                return BadRequest();
            }
            catch (TitleAlreadyExistsException e)
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.APIRegisterDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to register a document with a title already known.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.reference", e.ExistingReference),
                    null,
                    LogEvent.Formatter);
                return BadRequest();
            }
            catch (InvalidArgumentException e)
            {
                ModelState.Clear();
                foreach (var kv in e.Errors)
                foreach (var errorMessage in kv.Value)
                    ModelState.AddModelError(kv.Key, errorMessage);

                _logger.Log(LogLevel.Warning,
                    EventIDs.APIRegisterDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to register a document '{submittedDocument.DocumentId}' with invalid fields.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", submittedDocument.DocumentId),
                    null,
                    LogEvent.Formatter);

                return BadRequest(ModelState);
            }
        }

        [HttpPut("Subscribe")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIDocument))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Subscribe(Guid id, bool notification = false)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var document = await _documentRepository.GetAsync(AmbientContext, new DocumentQuery {DocumentId = id});

                await _documentRepository.SubscribeAsync(AmbientContext, document.DocumentId, notification);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.APISubscribeDocumentSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully subscribed to document '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddDocument(document),
                    null,
                    LogEvent.Formatter);

                return Ok();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APISubscribeDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to subscribe to document '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APISubscribeDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to subscribe to a non-existing document '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPut("Unsubscribe")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIDocument))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Unsubscribe(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var document = await _documentRepository.GetAsync(AmbientContext, new DocumentQuery {DocumentId = id});

                await _documentRepository.UnsubscribeAsync(AmbientContext, document.DocumentId);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.APIUnsubscribeDocumentSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully unsubscribed from document '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddDocument(document),
                    null,
                    LogEvent.Formatter);

                return Ok();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIUnsubscribeDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to unsubscribe from document '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIUnsubscribeDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to unsubscribe from a non-existing document '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("Details")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Details(Guid documentId)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var query = new DocumentQuery {DocumentId = documentId};
                var document = await _documentRepository.GetAsync(
                    AmbientContext,
                    query,
                    new[]
                    {
                        nameof(Document.DocumentTags),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet),
                        nameof(Document.Source),
                        nameof(Document.Comments)
                    }
                );

                var value = new
                {
                    document = _mapper.Map<APIDocument>(document),
                    subcribed = await _documentRepository.IsSubscribedAsync(AmbientContext, document.DocumentId),
                    comments = _mapper.Map<IEnumerable<APIComment>>(
                        await _commentRepository
                            .GetAllAsync(AmbientContext, new CommentQuery {DocumentId = document.DocumentId})
                            .ToListAsync()
                    )
                };

                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDetailsDocumentSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully viewed document '{documentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return base.Ok(
                    value
                );
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDetailsDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view document '{documentId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDetailsDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view a non-existing document '{documentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        // TODO What is the use case for that API call?
        [HttpGet("Export")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Export(Guid documentId)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var query = new DocumentQuery {DocumentId = documentId};
                var document = await _documentRepository.GetAsync(
                    AmbientContext,
                    query,
                    new[]
                    {
                        nameof(Document.DocumentTags),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet),
                        nameof(Document.Source),
                        nameof(Document.Comments)
                    }
                );

                var value = new
                {
                    document = _mapper.Map<APIDocumentExport>(document),
                    subcribed = await _documentRepository.IsSubscribedAsync(AmbientContext, document.DocumentId),
                    comments = _mapper.Map<IEnumerable<APIComment>>(
                        await _commentRepository
                            .GetAllAsync(AmbientContext, new CommentQuery {DocumentId = document.DocumentId})
                            .ToListAsync()
                    )
                };

                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDetailsDocumentSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully viewed document '{documentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return base.Ok(
                    value
                );
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDetailsDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view document '{documentId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDetailsDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view a non-existing document '{documentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        // TODO How to update a document file. This call does not make sense as a document contains multiple files.
        [HttpPatch("")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIDocument))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ModelStateDictionary))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Update([FromBody] APIDocument submittedDocument,
            [FromQuery] [Bind(Prefix = "tags")] string[] tagLabels,
            [FromQuery] [Bind(Prefix = "file")] IFormFile file)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var document = await _documentRepository.GetAsync(AmbientContext,
                    submittedDocument.DocumentId);

                if (submittedDocument.DocumentDate.Year == 1
                    && submittedDocument.DocumentDate.Month == 1
                    && submittedDocument.DocumentDate.Day == 1)
                    ModelState.AddModelError("DocumentDate", "The date looks suspicious.");

                var filteredTags = await GetTags(tagLabels, currentUser);
                document.SourceId = submittedDocument.Source.SourceId;

                document.Title = submittedDocument.Title;
                document.ExternalReference = submittedDocument.ExternalReference;
                document.ShortDescription = submittedDocument.ShortDescription;
                document.Note = submittedDocument.Note;

                foreach (var f in document.Files)
                {
                    f.DocumentDate = submittedDocument.DocumentDate;
                    f.Classification = submittedDocument.Classification;
                    f.SourceUrl = submittedDocument.SourceUrl;
                }

                if (ModelState.IsValid)
                {
                    Document updatedDoc = null;
                    if (file != null)
                    {
                        using var stream = file.OpenReadStream();
                        updatedDoc = await _documentRepository.UpdateAsync(AmbientContext, document, filteredTags);
                        await _documentRepository.UpdateFile(
                            AmbientContext,
                            document.Files.Single(),
                            stream
                        );
                    }
                    else
                    {
                        updatedDoc = await _documentRepository.UpdateAsync(AmbientContext, document, filteredTags);
                    }

                    await _context.SaveChangesAsync();

                    _logger.Log(LogLevel.Warning,
                        EventIDs.APIUpdateDocumentSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully updated document '{submittedDocument.DocumentId}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddProperty("document.id", submittedDocument.DocumentId),
                        null,
                        LogEvent.Formatter);

                    return Ok(_mapper.Map<APIDocument>(updatedDoc));
                }

                throw new InvalidArgumentException(ModelState);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIUpdateDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to update a document '{submittedDocument.DocumentId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", submittedDocument.DocumentId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIUpdateDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to update a non-existing document '{submittedDocument.DocumentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", submittedDocument.DocumentId),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
            catch (FileAlreadyKnownException e)
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.APIUpdateDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to upload a document that was already known.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.reference", e.ExistingReference),
                    null,
                    LogEvent.Formatter);
                return BadRequest();
            }
            catch (TitleAlreadyExistsException e)
            {
                _logger.Log(LogLevel.Information,
                    EventIDs.APIUpdateDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to upload a document with a title already known.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.reference", e.ExistingReference),
                    null,
                    LogEvent.Formatter);
                return BadRequest();
            }
            catch (InvalidArgumentException e)
            {
                ModelState.Clear();
                foreach (var kv in e.Errors)
                foreach (var errorMessage in kv.Value)
                    ModelState.AddModelError(kv.Key, errorMessage);

                _logger.Log(LogLevel.Warning,
                    EventIDs.APIUpdateDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to update a document '{submittedDocument.DocumentId}' with invalid fields.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", submittedDocument.DocumentId),
                    null,
                    LogEvent.Formatter);

                return BadRequest(ModelState);
            }
        }

        [HttpDelete("")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIDocument))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> Delete(Guid documentId)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var trackingEntity = await _documentRepository.RemoveAsync(AmbientContext, documentId);
                await _synapseRepository.RemoveRefs(documentId);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.APIDeleteDocumentSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully deleted document '{documentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return Ok();
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDeleteDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to update a document '{documentId}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDeleteDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to update a non-existing document '{documentId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", documentId),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("List")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<APIDocument>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [Produces("application/json", "application/xml")]
        public async Task<IActionResult> List(int page = 1)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var documents = await _documentRepository.GetAllAsync(AmbientContext, new DocumentQuery
                {
                    OrderBy = SortCriteria.ModificationDate,
                    Page = page,
                    Limit = 150
                }, new[] {"Files"}).ToListAsync();
                return base.Ok(
                    _mapper.Map<IEnumerable<APIDocument>>(documents)
                );
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.APIDeleteDocumentFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to list documents without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }

        #region Helpers

        // REFACTOR to avoid code duplication with non-API controller.

        private async Task<ISet<Tag>> GetTags(string[] tags, AppUser currentUser)
        {
            var filteredTags = _tagUtility.GetOrCreateTags(AmbientContext, tags);

            // check that there is a tag for all mandatory facets
            var mandatoryFacets = _facetRepository.GetAllAsync(AmbientContext, new FacetQuery
            {
                Mandatory = true
            }).ToEnumerable();
            var mandatoryIds = mandatoryFacets.Select(_ => _.FacetId).ToHashSet();
            var facetsPresents = filteredTags.Select(_ => _.FacetId).ToHashSet();

            if (mandatoryIds.Except(facetsPresents).Any())
            {
                var values = mandatoryFacets.Where(_ => !facetsPresents.Contains(_.FacetId)).Select(_ => _.Title);
                var str = (values.Count() > 1 ? "Facets " : "The facet ") + string.Join(", ", values) +
                          (values.Count() > 1 ? " are " : " is ") + " mandatory.";
                ModelState.AddModelError("Tags", str);
            }

            return filteredTags.ToHashSet();
        }

        #endregion
    }
}
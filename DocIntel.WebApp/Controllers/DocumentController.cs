/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau
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
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

using AutoMapper;

using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Helpers;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Observables;
using DocIntel.Core.Utils.Search.Documents;
using DocIntel.WebApp.Helpers;
using DocIntel.WebApp.ViewModels.DocumentViewModel;

using MassTransit;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace DocIntel.WebApp.Controllers
{
    public class DocumentController : BaseController
    {
        private readonly IAppAuthorizationService _appAuthorizationService;

        private readonly ApplicationSettings _appSettings;
        private readonly IPublishEndpoint _busClient;
        private readonly ICommentRepository _commentRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly ITagFacetRepository _facetRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly ILogger<DocumentController> _logger;
        private readonly IMapper _mapper;
        private readonly IObservableRepository _observableRepository;
        private readonly IObservablesUtility _observablesUtility;
        private readonly IObservableWhitelistUtility _observableWhitelistUtility;
        private readonly ISourceRepository _sourceRepository;
        private readonly ITagRepository _tagRepository;
        public readonly IClassificationRepository _classificationRepository;

        public DocumentController(DocIntelContext context,
            ILogger<DocumentController> logger,
            ApplicationSettings configuration,
            IAppAuthorizationService appAuthorizationService,
            UserManager<AppUser> userManager,
            IAuthorizationService authorizationService,
            ICommentRepository commentRepository,
            IDocumentRepository documentRepository,
            IObservableRepository observableRepository,
            ISourceRepository sourceRepository,
            ITagRepository tagRepository,
            ITagFacetRepository facetRepository,
            IMapper mapper, IPublishEndpoint busClient,
            IGroupRepository groupRepository,
            IObservablesUtility observablesUtility,
            IObservableWhitelistUtility observableWhitelistUtility, ApplicationSettings appSettings, IClassificationRepository classificationRepository)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _appAuthorizationService = appAuthorizationService;
            _documentRepository = documentRepository;
            _sourceRepository = sourceRepository;
            _commentRepository = commentRepository;
            _tagRepository = tagRepository;
            _facetRepository = facetRepository;
            _mapper = mapper;
            _busClient = busClient;
            _groupRepository = groupRepository;
            _observableRepository = observableRepository;
            _observablesUtility = observablesUtility;
            _observableWhitelistUtility = observableWhitelistUtility;
            _appSettings = appSettings;
            _classificationRepository = classificationRepository;
        }

        [HttpGet("Document")]
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Search");
        }

        [HttpGet("Document/Pending/{page?}")]
        public async Task<IActionResult> Pending(int page = 1, int opage = 1)
        {
            var currentUser = await GetCurrentUser();

            var count = await _documentRepository.CountAsync(AmbientContext, new DocumentQuery
            {
                Statuses = new[] {DocumentStatus.Submitted, DocumentStatus.Analyzed}.ToHashSet()
            });

            var ownCount = await _documentRepository.CountAsync(AmbientContext, new DocumentQuery
            {
                RegisteredBy = currentUser.Id,
                Statuses = new[] {DocumentStatus.Submitted, DocumentStatus.Analyzed}.ToHashSet(),
                Page = opage,
                Limit = 20,
                OrderBy = SortCriteria.RegistrationDate
            });

            var pendingDocuments = _documentRepository.GetAllAsync(AmbientContext, new DocumentQuery
                    {
                        Statuses = new[] {DocumentStatus.Submitted, DocumentStatus.Analyzed}.ToHashSet(),
                        Page = page,
                        Limit = 20,
                        OrderBy = SortCriteria.RegistrationDate
                    },
                    new[]
                    {
                        nameof(Document.RegisteredBy),
                        nameof(Document.LastModifiedBy),
                        nameof(Document.DocumentTags),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet)
                    })
                .ToEnumerable();

            var pendingCount = pendingDocuments.Count();

            var viewModel = new InboxViewModel
            {
                Documents = pendingDocuments,
                DocumentCount = count,
                Page = page,
                PageCount = pendingCount == 0 ? 1 : (int) Math.Ceiling(pendingCount / 20.0),
                OwnPage = opage,
                OwnPageCount = ownCount == 0 ? 1 : (int) Math.Ceiling(ownCount / 20.0),
                SubmittedDocuments = _documentRepository.GetSubmittedDocuments(AmbientContext,
                        _ => _.Where(__ => __.Status == SubmissionStatus.Submitted))
                    .ToEnumerable()
            };

            // Adds the model errors so they can be displayed.
            if (TempData.ContainsKey("Errors"))
            {
                _logger.LogDebug("Received errors...");
                var errors = JsonConvert.DeserializeObject<List<ValidationError>>((string) TempData["Errors"]);
                if (errors != null)
                {
                    _logger.LogDebug("Deserialized... " + errors.Count());
                    foreach (var e in errors)
                        AddModelError(e);
                }
            }

            return View(viewModel);
        }

        [HttpGet("Document/Submitted/{page?}")]
        public async Task<IActionResult> Submitted(int page = 1)
        {
            ViewData.Add("SubmittedCount",
                await _documentRepository
                    .GetSubmittedDocuments(AmbientContext, _ => _.Where(__ => __.Status == SubmissionStatus.Submitted))
                    .CountAsync());
            ViewData.Add("ProcessedCount",
                await _documentRepository
                    .GetSubmittedDocuments(AmbientContext, _ => _.Where(__ => __.Status == SubmissionStatus.Processed))
                    .CountAsync());
            ViewData.Add("DiscardedCount",
                await _documentRepository
                    .GetSubmittedDocuments(AmbientContext, _ => _.Where(__ => __.Status == SubmissionStatus.Discarded))
                    .CountAsync());
            ViewData.Add("DuplicateCount",
                await _documentRepository
                    .GetSubmittedDocuments(AmbientContext, _ => _.Where(__ => __.Status == SubmissionStatus.Duplicate))
                    .CountAsync());

            return View(_documentRepository.GetSubmittedDocuments(AmbientContext,
                    _ => _.Include(__ => __.Classification)
                        .Include(__ => __.ReleasableTo)
                        .Include(__ => __.EyesOnly),
                    page)
                .ToEnumerable());
        }

        [HttpGet("Document/Details/{url}")]
        public async Task<IActionResult> Details(string url)
        {
            var query = new DocumentQuery {URL = url};
            if (Guid.TryParse(url, out var guid))
            {
                query = new DocumentQuery {DocumentId = guid};
                var document = await _documentRepository.GetAsync(
                    AmbientContext,
                    query);
                return RedirectToAction("Details", new {document.URL});
            }

            if (url.StartsWith(_configuration.DocumentPrefix))
            {
                query = new DocumentQuery {Reference = url};
                var document = await _documentRepository.GetAsync(
                    AmbientContext,
                    query);
                return RedirectToAction("Details", new {document.URL});
            }

            var currentUser = await GetCurrentUser();
            try
            {
                var document = await _documentRepository.GetAsync(
                    AmbientContext,
                    query,
                    new[]
                    {
                        nameof(Document.DocumentTags),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet),
                        nameof(Document.Source),
                        nameof(Document.Comments),
                        nameof(Document.Files),
                        nameof(Document.RegisteredBy),
                        nameof(Document.LastModifiedBy)
                    }
                );

                var comments = await _commentRepository
                    .GetAllAsync(AmbientContext, new CommentQuery {DocumentId = document.DocumentId}, new[] {"Author"})
                    .ToListAsync();

                _logger.LogDebug(string.Join(",", comments.Select(_ => _.Author.FriendlyName)));

                var viewModel = new DocumentDetailsViewModel
                {
                    Document = document,
                    Observables = await _observableRepository.GetObservables(document.DocumentId),
                    Subscribed = await _documentRepository.IsSubscribedAsync(AmbientContext, document.DocumentId),
                    Comments = comments,
                    Contributors = new[] {document.RegisteredBy, document.LastModifiedBy}
                        .Union(comments.Select(_ => _.Author)).ToHashSet()
                };

                return View(viewModel);
            }
            catch (UnauthorizedOperationException)
            {
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                return NotFound();
            }
        }

        [HttpGet("Document/Thumbnail/{id}")]
        public async Task<IActionResult> Thumbnail(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var placeHolderImage = Path.Combine(_appSettings.StaticFiles, "images", "thumbnail-placeholder.png");
                var document = await _documentRepository.GetAsync(AmbientContext, new DocumentQuery {DocumentId = id},
                    new[] {"Files"});

                // Check for image file
                DocumentFile documentFile;
                string imagePath;
                if ((documentFile = document.Thumbnail) != null)
                    imagePath = Path.Combine(_configuration.DocFolder, documentFile.Filepath);
                else
                    return await StreamImage(placeHolderImage);

                if (!System.IO.File.Exists(imagePath))
                    return await StreamImage(placeHolderImage);

                var memory = new MemoryStream();
                await using (var stream = new FileStream(
                    imagePath,
                    FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }

                memory.Position = 0;

                var contentDisposition = new ContentDisposition
                {
                    FileName = Path.GetFileName(document.Files.First().FileId + ".png"),
                    Inline = true
                };
                Response.Headers.Add("Content-Disposition", contentDisposition.ToString());
                Response.Headers.Add("X-Content-Type-Options", "nosniff");

                return File(memory, "image/png");
            }
            catch (UnauthorizedOperationException)
            {
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                return NotFound();
            }
        }

        [HttpGet("Document/Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var document = await _documentRepository.GetAsync(
                    AmbientContext,
                    new DocumentQuery {DocumentId = id},
                    new[]
                    {
                        nameof(Document.Files),
                        nameof(Document.DocumentTags),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet),
                        nameof(Document.Source),
                        nameof(Document.Comments)
                    }
                );
                await SetupViewBag(currentUser);

                return View(document);
            }
            catch (UnauthorizedOperationException)
            {
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                return NotFound();
            }
        }

        private async Task SetupViewBag(AppUser currentUser)
        {
            ViewBag.AllClassifications = AmbientContext.DatabaseContext.Classifications.ToList();
            var allGroups = await _groupRepository.GetAllAsync(AmbientContext).ToListAsync();
            ViewBag.AllGroups = allGroups;
            ViewBag.OwnGroups = allGroups.Where(_ =>
                currentUser.Memberships.Any(__ => __.GroupId == _.GroupId));
        }

        [HttpPost("Document/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            [Bind("DocumentId", "Title", "ExternalReference", "ShortDescription", "Note", "SourceUrl", "DocumentDate",
                "ClassificationId", "ThumbnailId")]
            Document submittedDocument,
            [Bind(Prefix = "Source.SourceId")] string sourceId,
            [Bind(Prefix = "tags")] string[] labels,
            [Bind(Prefix = "releasableTo")] Guid[] releasableTo,
            [Bind(Prefix = "eyesOnly")] Guid[] eyesOnly,
            [Bind(Prefix = "file")] IFormFile file)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var document = await _documentRepository.GetAsync(AmbientContext,
                    submittedDocument.DocumentId,
                    new[]
                    {
                        nameof(Document.Files),
                        nameof(Document.DocumentTags),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet),
                        nameof(Document.Source),
                        nameof(Document.Comments)
                    });
                try
                {
                    var updatedDocument = await SaveDocument(document,
                        submittedDocument,
                        sourceId,
                        labels, releasableTo, eyesOnly,
                        file,
                        DocumentStatus.Registered);
                    await _context.SaveChangesAsync();

                    if (updatedDocument != null)
                        return RedirectToAction(nameof(Details), new {updatedDocument.URL});
                    throw new InvalidOperationException();
                }
                catch (FileAlreadyKnownException e)
                {
                    ModelState.AddModelError("file",
                        "The file '" + file.FileName + "' is already known. See document " + e.Document.Title + ".");

                    document.Title = submittedDocument.Title;
                    document.DocumentDate = submittedDocument.DocumentDate;
                    document.ExternalReference = submittedDocument.ExternalReference;
                    document.ShortDescription = submittedDocument.ShortDescription;
                    document.Note = submittedDocument.Note;
                    document.ClassificationId = submittedDocument.ClassificationId;
                    await SetupViewBag(currentUser);
                    return View(document);
                }
                catch (TitleAlreadyExistsException e)
                {
                    ModelState.AddModelError("Title",
                        "A document already exists with the same title. See document " + e.ExistingReference + ".");

                    document.Title = submittedDocument.Title;
                    document.DocumentDate = submittedDocument.DocumentDate;
                    document.ExternalReference = submittedDocument.ExternalReference;
                    document.ShortDescription = submittedDocument.ShortDescription;
                    document.Note = submittedDocument.Note;
                    document.ClassificationId = submittedDocument.ClassificationId;
                    await SetupViewBag(currentUser);
                    return View(document);
                }
                catch (InvalidArgumentException e)
                {
                    ModelState.Clear();
                    foreach (var kv in e.Errors)
                    foreach (var errorMessage in kv.Value)
                        ModelState.AddModelError(kv.Key, errorMessage);

                    document.Title = submittedDocument.Title;
                    document.DocumentDate = submittedDocument.DocumentDate;
                    document.ExternalReference = submittedDocument.ExternalReference;
                    document.ShortDescription = submittedDocument.ShortDescription;
                    document.Note = submittedDocument.Note;
                    document.ClassificationId = submittedDocument.ClassificationId;
                    await SetupViewBag(currentUser);
                    return View(document);
                }
            }
            catch (UnauthorizedOperationException)
            {
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> PreviewURL(string url, string title = "", string description = "")
        {
            return View(new SubmittedDocument
            {
                Title = title,
                Description = description,
                URL = url
            });
        }

        [HttpPost("Document/PreviewURL")]
        public async Task<IActionResult> PreviewURL(SubmittedDocument document)
        {
            var currentUser = await GetCurrentUser();
            if (Uri.TryCreate(document.URL, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                var submission = await _documentRepository.SubmitDocument(AmbientContext, new SubmittedDocument
                {
                    SubmitterId = currentUser.Id,
                    URL = uriResult.ToString()
                });
                await AmbientContext.DatabaseContext.SaveChangesAsync();

                await _busClient.Publish(new URLSubmittedMessage
                {
                    SubmissionId = submission.SubmittedDocumentId
                });
            }

            return Redirect(document.URL);
        }

        [HttpPost("Document/SubmitURL")]
        public async Task<IActionResult> SubmitURL(string url)
        {
            try
            {
                var currentUser = await GetCurrentUser();

                foreach (var urlLine in url.Split(new[] {"\r\n", "\r", "\n"},
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    if (Uri.TryCreate(urlLine, UriKind.Absolute, out var uriResult)
                        && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                    {
                        var submission = await _documentRepository.SubmitDocument(AmbientContext, new SubmittedDocument
                        {
                            SubmitterId = currentUser.Id,
                            URL = uriResult.ToString()
                        });
                        await AmbientContext.DatabaseContext.SaveChangesAsync();

                        await _busClient.Publish(new URLSubmittedMessage
                        {
                            SubmissionId = submission.SubmittedDocumentId
                        });
                    }

                return RedirectToAction("Pending");
            }
            catch (UnauthorizedOperationException)
            {
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                return NotFound();
            }
        }

        /// <summary>
        ///     Returns the list of 5 documents whose reference starts with the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix of document references</param>
        /// <returns>A list of JSON objects with a reference and a display name</returns>
        [HttpGet("Document/SearchByReference/{prefix}")]
        public async Task<JsonResult> SearchByReferenceAsync(string prefix)
        {
            var currentUser = await GetCurrentUser();
            var documents = _documentRepository.GetAllAsync(AmbientContext, new DocumentQuery
                {
                    ReferencePrefix = prefix
                })
                .Select(x =>
                    new
                    {
                        x.Reference,
                        DisplayName = $"{x.Reference} <span class=\"text-muted\">({x.Title})</span>"
                    }
                );
            return base.Json(documents);
        }

        // TODO Avoid code duplication with API
        private async Task<Tag> CreateOrAddTag(AppUser user, Dictionary<string, TagFacet> facetCache, string label)
        {
            var facetName = "";
            var tagName = label;
            if (tagName.IndexOf(':') > 0)
            {
                facetName = label.Split(':', 2)[0];
                tagName = label.Split(':', 2)[1];
            }

            TagFacet facet;
            if (facetCache.ContainsKey(facetName))
                facet = facetCache[facetName];
            else
                try
                {
                    facet = await _facetRepository.GetAsync(AmbientContext, facetName);
                    facetCache[facet.Prefix ?? ""] = facet;
                }
                catch (NotFoundEntityException)
                {
                    facet = await _facetRepository.AddAsync(AmbientContext,
                        new TagFacet
                            {Prefix = facetName, Title = string.IsNullOrEmpty(facetName) ? "Other" : facetName});
                    facetCache[facet.Prefix ?? ""] = facet;
                }

            Tag tag;
            try
            {
                tag = await _tagRepository.GetAsync(AmbientContext, facet.Id, tagName);
            }
            catch (NotFoundEntityException)
            {
                tag = await _tagRepository.CreateAsync(AmbientContext, new Tag {FacetId = facet.Id, Label = tagName});
            }

            return tag;
        }


        [HttpGet("Document/Upload")]
        public async Task<IActionResult> Upload()
        {
            if (!await _appAuthorizationService.CanCreateDocument(User, null))
                return Unauthorized();

            return View();
        }

        [HttpPost("Document/Upload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload([Bind(Prefix = "file")] List<IFormFile> files)
        {
            if (!await _appAuthorizationService.CanCreateDocument(User, null))
                return Unauthorized();

            if (files == null || !files.Any())
            {
                ModelState.AddModelError("file", "You must provide at least a file.");
                return View();
            }

            await GetCurrentUser();

            foreach (var file in files)
            {
                var title = file.FileName;
                if (title.EndsWith(".pdf")) title = title.Remove(title.Length - 4);

                await using var stream = file.OpenReadStream();
                Document document = null;
                try
                {
                    document = await _documentRepository.AddAsync(AmbientContext, new Document
                    {
                        Title = title,
                        Status = DocumentStatus.Submitted,
                        ClassificationId = _classificationRepository.GetDefault(AmbientContext).ClassificationId,
                    });
                    await _documentRepository.AddFile(
                        AmbientContext,
                        new DocumentFile
                        {
                            Filename = file.FileName,
                            Title = file.Name,
                            Document = document,
                            DocumentId = document.DocumentId,
                            Preview = true,
                            Visible = true
                        },
                        stream
                    );
                }
                catch (FileAlreadyKnownException e)
                {
                    var htmlMessage = "The file '" + file.FileName + "' is already uploaded. Register from the list below.";

                    if (e.Document.Status == DocumentStatus.Analyzed)
                        htmlMessage = "The file '" + file.FileName + "' is already uploaded. Register <a href=\"" +
                                      Url.Action("Create", "Document", new {id = e.Document.DocumentId}) + "\">"
                                      + e.Document.Title + "</a> from the list below.";
                    if (e.Document.Status == DocumentStatus.Registered)
                        htmlMessage = "The file '" + file.FileName + "' is already known. See document <a href=\"" +
                                      Url.Action("Details", "Document", new {id = e.Document.DocumentId}) + "\">"
                                      + e.Document.Title + "</a>.";

                    AddModelError("file",
                        "The file '" + file.FileName + "' is already known.",
                        htmlMessage
                    );
                }
                catch (TitleAlreadyExistsException e)
                {
                    AddModelError("file",
                        "A document already exists with the same title. See document " + e.ExistingReference + ".",
                        "A document already exists with the same title. See document " + e.ExistingReference + "."
                    );
                }
                catch (InvalidArgumentException e)
                {
                    foreach (var kv in e.Errors)
                    foreach (var errorMessage in kv.Value)
                        AddModelError(kv.Key, errorMessage);
                }
                catch (UnauthorizedOperationException)
                {
                }
                finally
                {
                    // If adding the file failed, remove the empty document we just added.
                    if ((ViewBag.ValidationErrors as List<ValidationError>)?.Any() ?? false && document != null)
                    {
                        // TODO Update to use repository (but Remove won't work since the entity was not added yet to the backend DB)
                        AmbientContext.DatabaseContext.Documents.Attach(document).State = EntityState.Unchanged;
                    }
                }
            }

            TempData["Errors"] = JsonConvert.SerializeObject((List<ValidationError>)ViewBag.ValidationErrors);
            _logger.LogError(TempData["Errors"].ToString());

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Pending), "Document");
        }

        


        [HttpGet("Document/Create/{id}")]
        public async Task<IActionResult> Create(Guid id)
        {
            try
            {
                var currentUser = await GetCurrentUser();
                if (!await _appAuthorizationService.CanCreateDocument(User, null))
                    return Unauthorized();

                var document = await _documentRepository.GetAsync(AmbientContext, id,
                    new[]
                    {
                        nameof(Document.Files),
                        nameof(Document.DocumentTags),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet),
                        nameof(Document.Source),
                        nameof(Document.Comments),
                        nameof(Document.ReleasableTo),
                        nameof(Document.EyesOnly)
                    });
                if (document.Status != DocumentStatus.Analyzed)
                    return NotFound();

                await SetupViewBag(currentUser);
                return View(document);
            }
            catch (UnauthorizedOperationException)
            {
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                return NotFound();
            }
        }

        [HttpPost("Document/Create/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Guid id,
            [Bind("DocumentId", "Title", "DocumentDate", "ExternalReference", "ClassificationId", "ShortDescription",
                "Source", "SourceUrl")]
            Document submittedDocument,
            [Bind(Prefix = "SourceId")] string sourceId,
            [Bind(Prefix = "tags")] string[] tags,
            [Bind(Prefix = "releasableTo")] Guid[] releasableTo,
            [Bind(Prefix = "eyesOnly")] Guid[] eyesOnly,
            [Bind(Prefix = "file")] IFormFile file)
        {
            var currentUser = await GetCurrentUser();
            Document document;

            try
            {
                document = await _documentRepository.GetAsync(AmbientContext, id,
                    new[]
                    {
                        nameof(Document.Files),
                        nameof(Document.DocumentTags),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet),
                        nameof(Document.Source),
                        nameof(Document.Comments)
                    });
            }
            catch (UnauthorizedOperationException)
            {
                // TODO Log error
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                // TODO Log error
                return NotFound();
            }

            try
            {
                if (document.Status != DocumentStatus.Analyzed)
                    return NotFound();

                var observables = await _observableRepository.GetObservables(id);
                var acceptedObservables = false;
                foreach (var i in observables)
                    if (_observablesUtility.RecommendAccepted(i.Type, i.Status, i.History))
                    {
                        acceptedObservables = true;
                        break;
                    }

                document = await SaveDocument(document, submittedDocument, sourceId, tags, releasableTo, eyesOnly, file,
                    acceptedObservables ? DocumentStatus.Analyzed : DocumentStatus.Registered);
                await _documentRepository.SubscribeAsync(AmbientContext, document.DocumentId);
                await AmbientContext.DatabaseContext.SaveChangesAsync();
                
                if (acceptedObservables)
                    return RedirectToAction("Observables", new {id = document.DocumentId});

                return RedirectToAction("Details", new {document.URL});
            }
            catch (FileAlreadyKnownException e)
            {
                var htmlMessage = "The file '" + file.FileName + "' is already uploaded. Register from the list below.";

                if (e.Document.Status == DocumentStatus.Analyzed)
                    htmlMessage = "The file '" + file.FileName + "' is already uploaded. Register <a href=\"" +
                                  Url.Action("Create", "Document", new {id = e.Document.DocumentId}) + "\">"
                                  + e.Document.Title + "</a> from the list below.";
                if (e.Document.Status == DocumentStatus.Registered)
                    htmlMessage = "The file '" + file.FileName + "' is already known. See document <a href=\"" +
                                  Url.Action("Details", "Document", new {id = e.Document.DocumentId}) + "\">"
                                  + e.Document.Title + "</a>.";

                AddModelError("file",
                    "The file '" + file.FileName + "' is already known.",
                    htmlMessage
                );
                
                await SetupViewBag(currentUser);
                return View((document));
            }
            catch (TitleAlreadyExistsException e)
            {
                AddModelError("file",
                    "A document already exists with the same title. See document " + e.ExistingReference + ".",
                    "A document already exists with the same title. See document " + e.ExistingReference + "."
                );
                await SetupViewBag(currentUser);
                return View((document));
            }

            catch (InvalidArgumentException e)
            {
                ModelState.Clear();
                foreach (var kv in e.Errors)
                foreach (var errorMessage in kv.Value)
                    ModelState.AddModelError(kv.Key, errorMessage);

                await SetupViewBag(currentUser);
                return View((document));
            }
        }

        private void AddModelError(string field, string message)
        {
            // TODO Sanitize HTML error message
            AddModelError(field, message, message);
        }

        private void AddModelError(string field, string message, string htmlMessage)
        {
            AddModelError(new ValidationError()
            {
                Field = field,
                Message = message,
                HtmlMessage = htmlMessage
            });
        }

        private void AddModelError(ValidationError error)
        {
            ModelState.AddModelError(error.Field, error.Message);
            
            if ((ViewBag.ValidationErrors as List<ValidationError>) == null)
                ViewBag.ValidationErrors = new List<ValidationError>();
            ((List<ValidationError>)ViewBag.ValidationErrors).Add(error);
        }

        [HttpGet("Document/EditObservables/{id}")]
        public async Task<IActionResult> EditObservables(Guid id)
        {
            try
            {
                var currentUser = await GetCurrentUser();
                if (!await _appAuthorizationService.CanEditDocument(User, null))
                    return Unauthorized();

                var document = await _documentRepository.GetAsync(AmbientContext, id,
                    new[]
                    {
                        nameof(Document.Files),
                        nameof(Document.DocumentTags),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet)
                    });
                if (document.Status != DocumentStatus.Registered)
                    return NotFound();

                var res = await _observableRepository.GetObservables(id);
                var rel = await _observablesUtility.DetectRelations(res, document);

                var divm = new DocumentObservablesViewModel();
                var dfov = new List<ObservableViewModel>();

                //var fileids = res.Select(u => u.FileId).Distinct();
                //foreach (var fileid in fileids)
                {
                    var o = res; // .Where(u => u.FileId == fileid);
                    var x = _mapper.Map<HashSet<ObservableViewModel>>(o);
                    foreach (var i in x)
                    {
                        i.IsAccepted = i.Status == ObservableStatus.Accepted;
                        i.IsWhitelisted = i.Status == ObservableStatus.Whitelisted;
                        var rsl = rel.Where(u => u.SourceRef == i.Id).Select(u => u.TagId);
                        i.Tags = document.DocumentTags.Where(j => rsl.Contains(j.TagId)).Select(u => u.Tag).ToList();
                        dfov.Add(i);
                    }
                }

                divm.Observables = dfov.ToArray();
                divm.Files = document.Files;

                divm.DocumentId = document.DocumentId;
                divm.Title = document.Title;

                await SetupViewBag(currentUser);
                return View(divm);
            }
            catch (UnauthorizedOperationException)
            {
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                return NotFound();
            }
        }

        [HttpPost("Document/EditObservables/{id}")]
        [RequestFormSizeLimit(20000, Order = 1)]
        [ValidateAntiForgeryToken(Order = 2)]
        public async Task<IActionResult> EditObservables(
            Guid id,
            [Bind("DocumentId", "Observables")] DocumentObservablesViewModel viewModel
        )
        {
            var currentUser = await GetCurrentUser();
            if (!await _appAuthorizationService.CanEditDocument(User, null))
                return Unauthorized();

            var document = await _documentRepository.GetAsync(AmbientContext, id,
                new[]
                {
                    nameof(Document.Files),
                    nameof(Document.DocumentTags),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet)
                });
            if (document.Status != DocumentStatus.Registered)
                return NotFound();

            if (ModelState.IsValid)
            {
                await UpdateObservable(id, viewModel);
                return RedirectToAction("Details", new {id = document.DocumentId});
            }

            viewModel.Files = document.Files;
            viewModel.DocumentId = document.DocumentId;
            viewModel.Title = document.Title;

            await SetupViewBag(currentUser);
            return View(viewModel);
        }

        public class Test
        {
            public string Id { get; set; }
        }
        
        [HttpPost("Document/Observables/{id}")]
        [RequestFormSizeLimit(20000, Order = 1)]
        [ValidateAntiForgeryToken(Order = 2)]
        public async Task<IActionResult> Observables(
            Guid id, 
            [Bind("DocumentId", "Observables")] DocumentObservablesViewModel viewModel
        )
        {
            var currentUser = await GetCurrentUser();
            if (!await _appAuthorizationService.CanCreateDocument(User, null))
                return Unauthorized();

            var document = await _documentRepository.GetAsync(AmbientContext, id,
                new[]
                {
                    nameof(Document.Files),
                    nameof(Document.DocumentTags),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet),
                    nameof(Document.Source),
                    nameof(Document.Comments)
                });
            
            if (document.Status != DocumentStatus.Analyzed)
                return NotFound();
            
            if (ModelState.IsValid)
            {
                await UpdateObservable(id, viewModel);
                await _documentRepository.UpdateStatusAsync(AmbientContext, viewModel.DocumentId, DocumentStatus.Registered);
                return RedirectToAction("Details", new {document.URL});
            }

            viewModel.Files = document.Files;
            viewModel.DocumentId = document.DocumentId;
            viewModel.Title = document.Title;

            await SetupViewBag(currentUser);
            return View(viewModel);
        }

        private async Task UpdateObservable(Guid id, DocumentObservablesViewModel viewModel)
        {
            // set accepted - rejected properties
            var storedObservables = (await _observableRepository.GetObservables(id)).ToList();

            foreach (var observableViewModel in viewModel.Observables)
            {
                if (string.IsNullOrEmpty(observableViewModel.Value))
                {
                    storedObservables.RemoveAll(o => o.Id == observableViewModel.Id);
                    await _observableRepository.DeleteObservable(observableViewModel.Id);
                }
                else
                {
                    var storedObservable = storedObservables.First(o => o.Id == observableViewModel.Id);

                    if (storedObservable.History != ObservableStatus.Whitelisted && observableViewModel.IsWhitelisted)
                    {
                        var res = await _observableWhitelistUtility.AddWhitelistedObservable(storedObservable);
                        storedObservable.Status = ObservableStatus.Whitelisted;
                    }
                    else
                    {
                        storedObservable.Status = storedObservable.History == ObservableStatus.Whitelisted
                            ? ObservableStatus.Whitelisted
                            : observableViewModel.IsAccepted
                                ? ObservableStatus.Accepted
                                : ObservableStatus.Rejected;

                        // update value
                        if (observableViewModel.Type == ObservableType.Artefact ||
                            observableViewModel.Type == ObservableType.File)
                            storedObservable.Hashes[0].Value = observableViewModel.Value;
                        else storedObservable.Value = observableViewModel.Value;
                    }
                }
            }

            await _observableRepository.UpdateObservables(storedObservables);
        }

        [HttpGet("Document/Observables/{id}")]
        public async Task<IActionResult> Observables(Guid id)
        {
            var currentUser = await GetCurrentUser();
            if (!await _appAuthorizationService.CanCreateDocument(User, null))
                return Unauthorized();

            var document = await _documentRepository.GetAsync(AmbientContext, id,
                new[]
                {
                    nameof(Document.Files),
                    nameof(Document.DocumentTags),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet),
                    nameof(Document.Source),
                    nameof(Document.Comments)
                });
            if (document.Status != DocumentStatus.Analyzed)
                return NotFound();

            var res = await _observablesUtility.GetDocumentObservables(id);
            var rel = await _observablesUtility.DetectRelations(res, document);

            var divm = new DocumentObservablesViewModel();
            var dfov = new List<ObservableViewModel>();

            //var fileids = res.Select(u => u.FileId).Distinct();
            // foreach (var fileid in fileids)
            {
                var o = res; // Where(u => u.FileId == fileid);
                var x = _mapper.Map<HashSet<ObservableViewModel>>(o);
                foreach (var i in x)
                {
                    i.IsAccepted = _observablesUtility.RecommendAccepted(i.Type, i.Status, i.History);
                    i.IsWhitelisted = i.Status == ObservableStatus.Whitelisted;
                    var rsl = rel.Where(u => u.SourceRef == i.Id).Select(u => u.TagId);
                    i.Tags = document.DocumentTags.Where(j => rsl.Contains(j.TagId)).Select(u => u.Tag).ToList();
                    dfov.Add(i);
                }
            }

            divm.Observables = dfov.ToArray();
            divm.Files = document.Files;

            divm.DocumentId = document.DocumentId;
            divm.Title = document.Title;

            await SetupViewBag(currentUser);
            return View(divm);
        }

        


        [HttpPost("Document/Save")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            [Bind("DocumentId", "Title", "DocumentDate", "ExternalReference", "Classification", "ShortDescription",
                "Source", "SourceUrl")]
            Document submittedDocument,
            [Bind(Prefix = "SourceId")] string sourceId,
            [Bind(Prefix = "tags")] string[] tags,
            [Bind(Prefix = "releasableTo")] Guid[] releasableTo,
            [Bind(Prefix = "eyesOnly")] Guid[] eyesOnly,
            [Bind(Prefix = "file")] IFormFile file)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var document = await _documentRepository.GetAsync(AmbientContext, submittedDocument.DocumentId);
                if (document.Status != DocumentStatus.Analyzed)
                    return NotFound();

                var updatedDocument = await SaveDocument(document, submittedDocument, sourceId, tags, releasableTo,
                    eyesOnly, file, DocumentStatus.Registered);
                return RedirectToAction("Create", new {id = document.DocumentId});
            }
            catch (UnauthorizedOperationException)
            {
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                return NotFound();
            }
            catch (InvalidArgumentException e)
            {
                ModelState.Clear();
                foreach (var kv in e.Errors)
                foreach (var errorMessage in kv.Value)
                    ModelState.AddModelError(kv.Key, errorMessage);

                ViewBag.AvailableClassifications = AmbientContext.DatabaseContext.Classifications.ToList();
                return View("Create", submittedDocument);
            }
        }

        [HttpGet("Document/Discard/{id}")]
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
                await _observableRepository.DeleteAllObservables(document.DocumentId);

                return RedirectToAction(nameof(Pending));
            }
            catch (UnauthorizedOperationException)
            {
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                return NotFound();
            }
        }

        


        [HttpGet("Document/Delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var currentUser = await GetCurrentUser();
                var document = await _documentRepository.GetAsync(AmbientContext, new DocumentQuery {DocumentId = id});
                return View(document);
            }
            catch (UnauthorizedOperationException)
            {
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                return NotFound();
            }
        }

        [HttpPost("Document/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, [Bind("DocumentId")] Document submittedDocument)
        {
            try
            {
                var currentUser = await GetCurrentUser();
                await _documentRepository.RemoveAsync(AmbientContext, submittedDocument.DocumentId);
                await _context.SaveChangesAsync();
                await _observableRepository.DeleteAllObservables(submittedDocument.DocumentId);
                return RedirectToAction(nameof(Index));
            }
            catch (UnauthorizedOperationException)
            {
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> DiscardSubmission(Guid id)
        {
            try
            {
                var currentUser = await GetCurrentUser();
                _documentRepository.DeleteSubmittedDocument(AmbientContext, id, hard: true);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Submitted));
            }
            catch (UnauthorizedOperationException)
            {
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                return NotFound();
            }
        }

        


        [HttpGet("Document/Subscribe/{id}/{notification?}")]
        public async Task<IActionResult> Subscribe(Guid id, bool notification = false, string returnUrl = null)
        {
            try
            {
                var currentUser = await GetCurrentUser();
                var document = await _documentRepository.GetAsync(AmbientContext, new DocumentQuery {DocumentId = id});

                await _documentRepository.SubscribeAsync(AmbientContext, document.DocumentId, notification);
                await _context.SaveChangesAsync();


                if (string.IsNullOrEmpty(returnUrl))
                    return RedirectToAction("Details", new {document.URL});
                return Redirect(returnUrl);
            }
            catch (UnauthorizedOperationException)
            {
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                return NotFound();
            }
        }

        [HttpGet("Document/Unsubscribe/{id}")]
        public async Task<IActionResult> Unsubscribe(Guid id, string returnUrl = null)
        {
            try
            {
                var currentUser = await GetCurrentUser();
                var document = await _documentRepository.GetAsync(AmbientContext, new DocumentQuery {DocumentId = id});

                await _documentRepository.UnsubscribeAsync(AmbientContext, document.DocumentId);
                await _context.SaveChangesAsync();



                if (string.IsNullOrEmpty(returnUrl))
                    return RedirectToAction("Details", new {document.URL});
                return Redirect(returnUrl);
            }
            catch (UnauthorizedOperationException)
            {
                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                return NotFound();
            }
        }

        


        private async Task<Document> SaveDocument(Document document,
            Document submittedDocument,
            string sourceId,
            string[] tagLabels,
            Guid[] releasableTo,
            Guid[] eyesOnly,
            IFormFile file,
            DocumentStatus status)
        {
            var currentUser = await GetCurrentUser();

            var filteredTags = await GetTags(tagLabels, currentUser);
            document.SourceId = await GetSourceId(sourceId, currentUser);

            var filteredRelTo = (ISet<Group>) await _groupRepository
                .GetAllAsync(AmbientContext, new GroupQuery {Id = releasableTo}).ToHashSetAsync();
            var filteredEyes = (ISet<Group>) await _groupRepository
                .GetAllAsync(AmbientContext, new GroupQuery {Id = eyesOnly}).ToHashSetAsync();

            document.Title = submittedDocument.Title;
            document.DocumentDate = submittedDocument.DocumentDate;
            document.ExternalReference = submittedDocument.ExternalReference;
            document.ShortDescription = submittedDocument.ShortDescription;
            document.Note = submittedDocument.Note;
            document.Status = status;
            document.ClassificationId = submittedDocument.ClassificationId;
            document.ThumbnailId = submittedDocument.ThumbnailId;
            document.SourceUrl = submittedDocument.SourceUrl;
            
            if (ModelState.IsValid)
            {
                if (file != null)
                    // TODO
                    // using (var stream = file.OpenReadStream())
                    // {
                    //     return await _documentRepository.UpdateAsync(AmbientContext, document, filteredTags, stream);
                    // }
                    throw new NotImplementedException();
                return await _documentRepository.UpdateAsync(AmbientContext, document, filteredTags, filteredRelTo,
                    filteredEyes);
            }

            throw new InvalidArgumentException(ModelState);
        }

        private async Task<ISet<Tag>> GetTags(string[] tags, AppUser currentUser)
        {
            var filteredTags = new HashSet<Tag>();

            // A cache is needed in order to avoid creating multiple time a new
            // facet with the same prefix when adding multiple tags. E.g. assume
            // that facet "test" is not known, and tags contains { "test:a", 
            // "test:b" }. Without the cache, the code would create a facet 
            // with prefix "test" twice, as it would not get it from database
            // (changes are not yet commited). EF Core caching should handle the
            // case and it could be later investigated.
            var facetCache = new Dictionary<string, TagFacet>();
            foreach (var label in tags.Distinct())
                try
                {
                    filteredTags.Add(await CreateOrAddTag(currentUser, facetCache, label));
                }
                catch (UnauthorizedOperationException)
                {
                }

            // check that there is a tag for all mandatory facets
            var mandatoryFacets = _facetRepository.GetAllAsync(AmbientContext, new FacetQuery
            {
                Mandatory = true
            }).ToEnumerable();
            var mandatoryIds = mandatoryFacets.Select(_ => _.Id).ToHashSet();
            var facetsPresents = filteredTags.Select(_ => _.FacetId).ToHashSet();

            if (mandatoryIds.Except(facetsPresents).Any())
            {
                var values = mandatoryFacets.Where(_ => !facetsPresents.Contains(_.Id))
                    .Select(_ => _.Title);
                var str = (values.Count() > 1 ? "Facets " : "The facet ") + string.Join(", ", values) +
                          (values.Count() > 1 ? " are " : " is ") + " mandatory.";
                ModelState.AddModelError("Tags", str);
            }

            return filteredTags;
        }

        private async Task<Guid?> GetSourceId(string sourceIdOrTitle, AppUser currentUser)
        {
            if (string.IsNullOrEmpty(sourceIdOrTitle))
                return null;

            // If sourceIdOrTitle is a GUID
            if (Guid.TryParse(sourceIdOrTitle, out var sourceGuid))
                try
                {
                    return (await _sourceRepository.GetAsync(AmbientContext, sourceGuid)).SourceId;
                }
                catch (NotFoundEntityException)
                {
                    ModelState.AddModelError("SourceId", "Source could not be found.");
                    return null;
                }

            try
            {
                var source = await _sourceRepository.GetAsync(AmbientContext, new SourceQuery
                {
                    Title = sourceIdOrTitle
                });
                return source.SourceId;
            }
            catch (NotFoundEntityException)
            {
                var source = await _sourceRepository.CreateAsync(AmbientContext, new Source
                {
                    Title = sourceIdOrTitle
                });
                return source.SourceId;
            }
            catch (UnauthorizedOperationException)
            {
                // Do nothing but log FIXME
                return null;
            }
        }

        private async Task<IActionResult> StreamImage(string imagePath)
        {
            var memory = new MemoryStream();
            await using (var stream = new FileStream(
                imagePath,
                FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }

            memory.Position = 0;

            var contentDisposition = new ContentDisposition
            {
                FileName = Path.GetFileName(imagePath),
                Inline = true
            };
            Response.Headers.Add("Content-Disposition", contentDisposition.ToString());
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            return File(memory, "image/png");
        }

        
    }
}
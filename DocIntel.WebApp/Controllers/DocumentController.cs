/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau
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
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Helpers;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils;
using DocIntel.Core.Utils.Observables;
using DocIntel.Core.Utils.Search.Documents;
using DocIntel.WebApp.ViewModels.DocumentViewModel;
using JetBrains.Annotations;
using MassTransit;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Synsharp;
using Synsharp.Telepath.Messages;

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
        private readonly ISynapseRepository _synapseRepository;
        private readonly TagUtility _tagUtility;
        private readonly ISourceRepository _sourceRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IClassificationRepository _classificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IScraperRepository _scraperRepository;

        public DocumentController(
            DocIntelContext context,
            ILogger<DocumentController> logger,
            ApplicationSettings configuration,
            IAppAuthorizationService appAuthorizationService,
            UserManager<AppUser> userManager,
            IAuthorizationService authorizationService,
            ICommentRepository commentRepository,
            IDocumentRepository documentRepository,
            ISourceRepository sourceRepository,
            ITagRepository tagRepository,
            ITagFacetRepository facetRepository,
            IMapper mapper, IPublishEndpoint busClient,
            IGroupRepository groupRepository,
            ApplicationSettings appSettings,
            IClassificationRepository classificationRepository,
            IUserRepository userRepository,
            ISynapseRepository synapseRepository, TagUtility tagUtility, IWebHostEnvironment webHostEnvironment, IScraperRepository scraperRepository)
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
            _appSettings = appSettings;
            _classificationRepository = classificationRepository;
            _userRepository = userRepository;
            _synapseRepository = synapseRepository;
            _tagUtility = tagUtility;
            _webHostEnvironment = webHostEnvironment;
            _scraperRepository = scraperRepository;
        }

        [HttpGet("Document")]
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Search");
        }

        [HttpGet("Document/Pending/{page?}")]
        public async Task<IActionResult> Pending(int page = 1, string username = "")
        {
            var currentUser = await GetCurrentUser();

            AppUser user = null;
            if (!string.IsNullOrEmpty(username))
            {
                try
                {
                    user = await _userRepository.GetByUserName(AmbientContext, username);
                } 
                catch (UnauthorizedOperationException)
                {
                    // TODO: Log silently
                }
                catch (NotFoundEntityException)
                {
                    // TODO: Log silently
                }
            }

            var count = await _documentRepository.CountAsync(AmbientContext, new DocumentQuery
            {
                Statuses = new[] {DocumentStatus.Submitted, DocumentStatus.Analyzed}.ToHashSet()
            });

            var documentQuery = new DocumentQuery
            {
                Statuses = new[] {DocumentStatus.Submitted, DocumentStatus.Analyzed}.ToHashSet(),
                Page = page,
                Limit = 20,
                OrderBy = SortCriteria.RegistrationDate
            };
            
            var documentQueryCount = new DocumentQuery
            {
                Statuses = new[] {DocumentStatus.Submitted, DocumentStatus.Analyzed}.ToHashSet(),
                Limit = -1
            };

            if (user != null)
            {
                documentQuery.RegisteredBy = user.Id;
                documentQueryCount.RegisteredBy = user.Id;
            }

            var pendingDocuments = _documentRepository.GetAllAsync(AmbientContext, documentQuery,
                    new[]
                    {
                        nameof(Document.Source),
                        nameof(Document.RegisteredBy),
                        nameof(Document.LastModifiedBy),
                        nameof(Document.DocumentTags),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                        nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet)
                    })
                .ToEnumerable();

            var pendingCount = await _documentRepository.CountAsync(AmbientContext, documentQueryCount);

            var viewModel = new InboxViewModel
            {
                RegisteredBy = user,
                Documents = pendingDocuments,
                DocumentCount = count,
                Page = page,
                PageCount = pendingCount == 0 ? 1 : (int) Math.Ceiling(pendingCount / 20.0),
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
            var currentUser = await GetCurrentUser();
            try
            {
                var query = new DocumentQuery {URL = url};
                if (Guid.TryParse(url, out var guid))
                {
                    query = new DocumentQuery {DocumentId = guid};
                    var tempDocument = await _documentRepository.GetAsync(
                        AmbientContext,
                        query);
                    return RedirectToAction("Details", new {tempDocument.URL});
                }

                if (url.StartsWith(_configuration.DocumentPrefix))
                {
                    query = new DocumentQuery {Reference = url};
                    var tempDocument = await _documentRepository.GetAsync(
                        AmbientContext,
                        query);
                    return RedirectToAction("Details", new {tempDocument.URL});
                }
                
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

                var viewModel = new DocumentDetailsViewModel
                {
                    AvailableTypes = await _synapseRepository.GetSimpleForms(),
                    Document = document,
                    Observables = await _synapseRepository.GetObservables(document, false, false).ToListAsync(),
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
            string rootPath = _appSettings.StaticFiles;
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = _webHostEnvironment.WebRootPath;
            }
            else if (!rootPath.StartsWith("/"))
            {
                rootPath = Path.Combine(_webHostEnvironment.ContentRootPath, rootPath);
            }

            var placeHolderImage = Path.Combine(rootPath, "images", "thumbnail-placeholder.png");
            
            try
            {
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
                await SetupViewBag(currentUser, document);

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

        private async Task SetupViewBag(AppUser currentUser, Document document)
        {
            ViewBag.ReviewObservables = await _synapseRepository.GetObservables(document, true).AnyAsync();
            
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
            [Bind(Prefix = "tags")] string[] tags,
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
                        tags, releasableTo, eyesOnly,
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
                    await SetupViewBag(currentUser, document);
                    AddFakeTags(tags, document);
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
                    await SetupViewBag(currentUser, document);
                    AddFakeTags(tags, document);
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
                    await SetupViewBag(currentUser, document);
                    AddFakeTags(tags, document);
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

        public IActionResult PreviewURL(string url, string title = "", string description = "")
        {
            return View(new SubmittedDocument
            {
                Title = title,
                Description = description,
                URL = url
            });
        }

        [HttpPost("Document/PreviewURL")]
        public async Task<IActionResult> PreviewURL(SubmittedDocument submittedDocument)
        {
            var currentUser = await GetCurrentUser();
            if (Uri.TryCreate(submittedDocument.URL, UriKind.Absolute, out var uriResult)
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

            return Redirect(submittedDocument.URL);
        }

        [HttpPost("Document/SubmitURL")]
        public async Task<IActionResult> SubmitURL(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) 
                return RedirectToAction("Pending");
            
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

        [HttpGet("Document/Upload")]
        public async Task<IActionResult> Upload()
        {
            if (!await _appAuthorizationService.CanCreateDocument(User, null))
                return Unauthorized();

            ViewBag.ScraperExists = await _scraperRepository.AnyAsync(AmbientContext);

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
                bool error = false;
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
                    error = true;
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
                    error = true;
                    AddModelError("file",
                        "A document already exists with the same title. See document " + e.ExistingReference + ".",
                        "A document already exists with the same title. See document " + e.ExistingReference + "."
                    );
                }
                catch (InvalidArgumentException e)
                {
                    error = true;
                    foreach (var kv in e.Errors)
                    foreach (var errorMessage in kv.Value)
                        AddModelError(kv.Key, errorMessage);
                }
                catch (UnauthorizedOperationException)
                {
                    error = true;
                }
                finally
                {
                    // If adding the file failed, remove the empty document we just added.
                    if (error && document != null)
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

                await SetupViewBag(currentUser, document);
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

                var observables = await _synapseRepository.GetObservables(document, true).ToListAsync();

                document.RegisteredBy = currentUser;
                
                document = await SaveDocument(document, submittedDocument, sourceId, tags, releasableTo, eyesOnly, file,
                    observables.Any() ? DocumentStatus.Analyzed : DocumentStatus.Registered);
                await AmbientContext.DatabaseContext.SaveChangesAsync();
                
                if (observables.Any())
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
                
                await SetupViewBag(currentUser, document);
                AddFakeTags(tags, document);
                return View((document));
            }
            catch (TitleAlreadyExistsException e)
            {
                AddModelError("file",
                    "A document already exists with the same title. See document " + e.ExistingReference + ".",
                    "A document already exists with the same title. See document " + e.ExistingReference + "."
                );
                await SetupViewBag(currentUser, document);
                AddFakeTags(tags, document);
                return View((document));
            }

            catch (InvalidArgumentException e)
            {
                ModelState.Clear();
                foreach (var kv in e.Errors)
                foreach (var errorMessage in kv.Value)
                    ModelState.AddModelError(kv.Key, errorMessage);

                await SetupViewBag(currentUser, document);
                AddFakeTags(tags, document);
                return View((document));
            }
        }

        private void AddFakeTags(string[] tags, Document document)
        {
            foreach (var submittedTag in tags)
            {
                if (document.DocumentTags.All(_ => _.Tag.FriendlyName != submittedTag))
                {
                    var prefix = "";
                    var label = submittedTag;
                    if (submittedTag.Contains(':'))
                    {
                        prefix = submittedTag.Split(':', 2)[0];
                        label = submittedTag.Split(':', 2)[1];
                    }

                    var tag = new DocumentTag()
                    {
                        DocumentId = document.DocumentId,
                        Tag = (_tagRepository.GetAllAsync(AmbientContext, new TagQuery()
                        {
                            FacetPrefix = prefix,
                            Label = label
                        }).FirstOrDefaultAsync().Result) ?? new Tag()
                        {
                            Label = label,
                            Facet = new TagFacet()
                            {
                                Prefix = prefix
                            }
                        }
                    };

                    document.DocumentTags.Add(tag);
                }
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

                var availableTypes = await _synapseRepository.GetSimpleForms();
                
                await SetupViewBag(currentUser, document);
                return View(new DocumentObservablesViewModel
                {
                    AvailableTypes = availableTypes,
                    Observables = await _synapseRepository.GetObservables(document).ToListAsync(),
                    Document = document
                });
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
            [Bind(Prefix = "observables")] string[] observableIden,
            [Bind(Prefix = "status")] string[] observableStatus
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

            for (var index = 0; index < observableIden.Length; index++)
            {
                var iden = observableIden[index];
                var status = observableStatus[index];
                if (status == "remove")
                {
                    _logger.LogDebug($"Remove {iden}");
                    await _synapseRepository.Remove(document, iden, false, true);
                } else if (status == "remove-ignore")
                {
                    _logger.LogDebug($"Add _di.workflow.ignore to {iden}");
                    await _synapseRepository.AddTag(document, iden, "_di.workflow.ignore", false);
                    await _synapseRepository.Remove(document, iden, false, true);
                }
            }

            if (ModelState.IsValid)
            {
                return RedirectToAction("Details", new {document.URL});
            }

            await SetupViewBag(currentUser, document);
            return View(new DocumentObservablesViewModel
            {
                Observables = await _synapseRepository.GetObservables(document, true).ToListAsync(),
                Document = document
            });
        }

        [HttpPost("AddObservable")]
        public async Task<IActionResult> AddObservable(Guid documentId, string obsType, string obsValue, string returnUrl = null)
        {
            var currentUser = await GetCurrentUser();
            if (!await _appAuthorizationService.CanEditDocument(User, null))
                return Unauthorized();

            var document = await _documentRepository.GetAsync(AmbientContext, documentId,
                new[]
                {
                    nameof(Document.Files),
                    nameof(Document.DocumentTags),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag),
                    nameof(Document.DocumentTags) + "." + nameof(DocumentTag.Tag) + "." + nameof(Tag.Facet)
                });
            if (document.Status != DocumentStatus.Registered)
                return NotFound();
            
            if (string.IsNullOrWhiteSpace(obsType))
                return RedirectToAction(nameof(EditObservables), new {id = document.DocumentId});
            
            var availableTypes = await _synapseRepository.GetSimpleForms();
            if (!availableTypes.Contains(obsType))
                return NotFound();

            try
            {
                var ob = new SynapseNode() { Form = obsType, Valu = obsValue };
                
                if (ob != null)
                    await _synapseRepository.Add(new[]{ob}, document, null);
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(EditObservables), new {id = document.DocumentId});
        }
        
        [HttpPost("Document/Observables/{id}")]
        [RequestFormSizeLimit(20000, Order = 1)]
        [ValidateAntiForgeryToken(Order = 2)]
        public async Task<IActionResult> Observables(
            Guid id, 
            [Bind(Prefix = "observables")] string[] observableIden,
            [Bind(Prefix = "status")] string[] observableStatus
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
            
            document.RegisteredBy = currentUser;
            for (var index = 0; index < observableIden.Length; index++)
            {
                try
                {
                    var iden = observableIden[index];
                    var status = observableStatus[index];
                    if (status == "ignore-once")
                    {
                        _logger.LogDebug($"Remove {iden}");
                        await _synapseRepository.Remove(document, iden, true);
                    } else if (status == "ignore-always")
                    {
                        _logger.LogDebug($"Add _di.workflow.ignore to {iden}");
                        await _synapseRepository.AddTag(document, iden, "_di.workflow.ignore", false);
                    } else if (status == "ignore-domain")
                    {
                        _logger.LogDebug($"Ignore-domain {iden}");
                        var url = await _synapseRepository.GetObservableByIden(document, iden, true);
                        if (url != null && url.Form == "inet:url")
                        {
                            // Add the FQDN with the proper tag globally
                            var fqdn = new SynapseNode() { Form = "inet:fqdn", Valu = url.Props[":fqdn"] };
                            fqdn.Tags.Add("_di.workflow.ignore", new long?[] {});
                            await _synapseRepository.Add(fqdn);

                            await _synapseRepository.RemoveRefDataWithProperty(document, "fqdn", url.Props[":fqdn"], true);
                        } else if (url != null && url.Form == "inet:fqdn")
                        {
                            await _synapseRepository.AddTag(document, iden, "_di.workflow.ignore", false);
                            await _synapseRepository.RemoveRefDataWithProperty(document, "fqdn", url.Valu, true).ToListAsync();
                        }
                    }

                    await _synapseRepository.RemoveTag(document, iden, "_di.workflow.review", false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                    _logger.LogError(e.StackTrace);
                }
            }

            if (ModelState.IsValid)
            {
                await _documentRepository.UpdateStatusAsync(AmbientContext, document.DocumentId, DocumentStatus.Registered);
                await _synapseRepository.Merge(document);
                await AmbientContext.DatabaseContext.SaveChangesAsync();
                return RedirectToAction("Details", new {document.URL});
            }

            await SetupViewBag(currentUser, document);
            return View(new DocumentObservablesViewModel
            {
                Observables = await _synapseRepository.GetObservables(document, true).ToListAsync(),
                Document = document
            });
        }
        
        [HttpPost("Document/Observables/Save/{id}")]
        [RequestFormSizeLimit(20000, Order = 1)]
        [ValidateAntiForgeryToken(Order = 2)]
        public async Task<IActionResult> SaveObservables(
            Guid id, 
            [Bind(Prefix = "observables")] string[] observableIden,
            [Bind(Prefix = "status")] string[] observableStatus
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
            
            document.RegisteredBy = currentUser;
            for (var index = 0; index < observableIden.Length; index++)
            {
                try
                {
                    var iden = observableIden[index];
                    var status = observableStatus[index];
                    if (status == "ignore-once")
                    {
                        _logger.LogDebug($"Remove {iden}");
                        await _synapseRepository.Remove(document, iden, true);
                        
                    } else if (status == "ignore-always")
                    {
                        _logger.LogDebug($"Add _di.workflow.ignore to {iden}");
                        await _synapseRepository.AddTag(document, iden, "_di.workflow.ignore", false);
                        
                    } else if (status == "ignore-domain")
                    {
                        _logger.LogDebug($"Ignore-domain {iden}");
                        var url = await _synapseRepository.GetObservableByIden(document, iden, true);
                        if (url != null && url.Form == "inet:url")
                        {
                            // Add the FQDN with the proper tag globally
                            var fqdn = new SynapseNode() { Form = "inet:fqdn", Valu = url.Props[":fqdn"] };
                            fqdn.Tags.Add("_di.workflow.ignore", new long?[] {});
                            await _synapseRepository.Add(fqdn);

                            await _synapseRepository.RemoveRefDataWithProperty(document, "fqdn", url.Props[":fqdn"], true);
                        } else if (url != null && url.Form == "inet:fqdn")
                        {
                            await _synapseRepository.AddTag(document, iden, "_di.workflow.ignore", false);
                            await _synapseRepository.RemoveRefDataWithProperty(document, "fqdn", url.Valu, true).ToListAsync();
                        }
                    }

                    await _synapseRepository.RemoveTag(document, iden, "_di.workflow.review", false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                    _logger.LogError(e.StackTrace);
                }
            }

            return RedirectToAction("Observables", new { id = document.DocumentId });
        }

        private Task UpdateObservable(Guid id, DocumentObservablesViewModel viewModel)
        {
            throw new NotImplementedException();
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

            await SetupViewBag(currentUser, document);
            return View(new DocumentObservablesViewModel
            {
                Observables = await _synapseRepository.GetObservables(document, true).ToListAsync(),
                Document = document
            });
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

            Document document;
            try
            {
                document = await _documentRepository.GetAsync(AmbientContext, submittedDocument.DocumentId,
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
            catch (TitleAlreadyExistsException e)
            {
                AddModelError("file",
                    "A document already exists with the same title. See document " + e.ExistingReference + ".",
                    "A document already exists with the same title. See document " + e.ExistingReference + "."
                );
                await SetupViewBag(currentUser, document);
                AddFakeTags(tags, document);
                return View((document));
            }

            catch (InvalidArgumentException e)
            {
                ModelState.Clear();
                foreach (var kv in e.Errors)
                foreach (var errorMessage in kv.Value)
                    ModelState.AddModelError(kv.Key, errorMessage);

                await SetupViewBag(currentUser, document);
                AddFakeTags(tags, document);
                return View((document));
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
                await _synapseRepository.RemoveView(document);
                
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
                await _synapseRepository.Remove(submittedDocument.DocumentId);
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
            document.DocumentDate = submittedDocument.DocumentDate.ToUniversalTime();
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
                    throw new NotImplementedException();
                return await _documentRepository.UpdateAsync(AmbientContext, document, filteredTags, filteredRelTo,
                    filteredEyes);
            }

            throw new InvalidArgumentException(ModelState);
        }

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
                var values = mandatoryFacets.Where(_ => !facetsPresents.Contains(_.FacetId))
                    .Select(_ => _.Title);
                var str = (values.Count() > 1 ? "Facets " : "The facet ") + string.Join(", ", values) +
                          (values.Count() > 1 ? " are " : " is ") + " mandatory.";
                ModelState.AddModelError("Tags", str);
            }

            return filteredTags.ToHashSet();
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
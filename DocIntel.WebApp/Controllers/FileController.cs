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
using System.Text;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.WebApp.Helpers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Controllers
{
    public class FileController : BaseController
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly IAppAuthorizationService _appAuthorizationService;

        private readonly IDocumentRepository _documentRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly ILogger _logger;
        private readonly ApplicationSettings _settings;

        public FileController(DocIntelContext context,
            ILogger<FileController> logger,
            ApplicationSettings configuration,
            IAppAuthorizationService appAuthorizationService,
            AppUserManager userManager,
            IAuthorizationService authorizationService,
            IDocumentRepository documentRepository,
            IHttpContextAccessor accessor,
            IGroupRepository groupRepository,
            ApplicationSettings settings)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _logger = logger;
            _appAuthorizationService = appAuthorizationService;
            _documentRepository = documentRepository;
            _accessor = accessor;
            _groupRepository = groupRepository;
            _settings = settings;
        }

        [HttpGet("File/Create/{id}")]
        public async Task<IActionResult> Create(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var document = await _documentRepository.GetAsync(AmbientContext, id);

                if (!await _appAuthorizationService.CanAddDocumentFile(User, document))
                {
                    _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to request upload file to document '{id}' without legitimate rights.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext)
                            .AddProperty("document.id", id),
                        null,
                        LogEvent.Formatter);
                    return Unauthorized();
                }

                await AmbientContext.DatabaseContext.Entry(currentUser).Collection(u => u.Memberships).LoadAsync();

                ViewBag.AvailableClassifications = GetAvailableClassifications();

                var allGroups = await _groupRepository.GetAllAsync(AmbientContext).ToListAsync();
                ViewBag.OwnGroups = allGroups.Where(_ => currentUser.Memberships.Any(__ => __.GroupId == _.GroupId));
                ViewBag.DefaultGroups = _groupRepository.GetDefaultGroups(AmbientContext);
                ViewBag.AllGroups = Enumerable.Except(allGroups, ViewBag.DefaultGroups);

                return View(new DocumentFile {Document = document});
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to request upload file to document '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to request upload file to non-existing document '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("File/Create/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid id,
            [Bind(
                "FileId,Title,SourceUrl,Preview,Visible,ClassificationId,OverrideClassification,OverrideEyesOnly,OverrideReleasableTo")]
            DocumentFile submittedFile,
            IFormFile file,
            Guid[] eyesOnly,
            Guid[] releasableTo)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var document = await _documentRepository.GetAsync(AmbientContext, id);
                submittedFile.DocumentId = document.DocumentId;
                submittedFile.Document = document;
                submittedFile.Title = string.IsNullOrEmpty(submittedFile.Title) ? file.FileName : submittedFile.Title;

                await AmbientContext.DatabaseContext.Entry(currentUser).Collection(u => u.Memberships).LoadAsync();

                var filteredRelTo = (ISet<Group>) await _groupRepository
                    .GetAllAsync(AmbientContext, new GroupQuery {Id = releasableTo}).ToHashSetAsync();
                var filteredEyes = (ISet<Group>) await _groupRepository
                    .GetAllAsync(AmbientContext, new GroupQuery {Id = eyesOnly}).ToHashSetAsync();

                try
                {
                    if (file != null)
                    {
                        await using var stream = file.OpenReadStream();
                        var result = await _documentRepository.AddFile(AmbientContext, submittedFile, stream,
                            filteredRelTo, filteredEyes);

                        _logger.Log(LogLevel.Warning, EventIDs.UpdateFileSuccessful,
                            new LogEvent(
                                    $"User '{currentUser.UserName}' successfully edited file '{id}'.")
                                .AddUser(currentUser)
                                .AddHttpContext(_accessor.HttpContext),
                            null,
                            LogEvent.Formatter);

                        await AmbientContext.DatabaseContext.SaveChangesAsync();

                        return RedirectToAction("Details", new {id = result.FileId});
                    }

                    ModelState.AddModelError("file", "You must specify a file.");
                    throw new InvalidArgumentException(ModelState);
                }
                catch (FileAlreadyKnownException e)
                {
                    ModelState.AddModelError("file",
                        "The file '" + file.FileName + "' is already known. See document " + e.Document.Title + ".");

                    _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to add an already existing file.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext),
                        null,
                        LogEvent.Formatter);
                    
                    var allGroups = await _groupRepository.GetAllAsync(AmbientContext).ToListAsync();
                    ViewBag.OwnGroups = allGroups.Where(_ => currentUser.Memberships.Any(__ => __.GroupId == _.GroupId));
                    ViewBag.DefaultGroups = _groupRepository.GetDefaultGroups(AmbientContext);
                    ViewBag.AllGroups = Enumerable.Except(allGroups, ViewBag.DefaultGroups);

                    submittedFile.DocumentId = document.DocumentId;
                    submittedFile.Document = document;

                    return View(submittedFile);
                }
                catch (InvalidArgumentException e)
                {
                    ModelState.Clear();
                    foreach (var kv in e.Errors)
                    foreach (var errorMessage in kv.Value)
                        ModelState.AddModelError(kv.Key, errorMessage);

                    _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to edit file '{id}' with an invalid model.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext),
                        null,
                        LogEvent.Formatter);
                    
                    var allGroups = await _groupRepository.GetAllAsync(AmbientContext).ToListAsync();
                    ViewBag.OwnGroups = allGroups.Where(_ => currentUser.Memberships.Any(__ => __.GroupId == _.GroupId));
                    ViewBag.DefaultGroups = _groupRepository.GetDefaultGroups(AmbientContext);
                    ViewBag.AllGroups = Enumerable.Except(allGroups, ViewBag.DefaultGroups);

                    submittedFile.DocumentId = document.DocumentId;
                    submittedFile.Document = document;

                    return View(submittedFile);
                }
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit file '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to view update a non-existing file '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("file.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("File/Update/{id}")]
        public async Task<IActionResult> Update(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                await AmbientContext.DatabaseContext.Entry(currentUser).Collection(u => u.Memberships).LoadAsync();

                var f = await _documentRepository.GetFileAsync(AmbientContext, id, new[] {"Document"});
                ViewBag.AvailableClassifications = GetAvailableClassifications();

                var allGroups = await _groupRepository.GetAllAsync(AmbientContext).ToListAsync();
                ViewBag.OwnGroups = allGroups.Where(_ => currentUser.Memberships.Any(__ => __.GroupId == _.GroupId));
                ViewBag.DefaultGroups = _groupRepository.GetDefaultGroups(AmbientContext);
                ViewBag.AllGroups = Enumerable.Except(allGroups, ViewBag.DefaultGroups);

                return View(f);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit file '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to view update a non-existing file '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("file.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("File/Update/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Guid id,
            [Bind(
                "FileId,Title,SourceUrl,Preview,Visible,ClassificationId,OverrideClassification,OverrideEyesOnly,OverrideReleasableTo")]
            DocumentFile submittedFile,
            IFormFile file,
            Guid[] eyesOnly,
            Guid[] releasableTo)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                await AmbientContext.DatabaseContext.Entry(currentUser).Collection(u => u.Memberships).LoadAsync();

                var f = await _documentRepository.GetFileAsync(AmbientContext, submittedFile.FileId,
                    new[] {"Document"});

                f.Title = submittedFile.Title;
                f.ClassificationId = submittedFile.ClassificationId;
                f.SourceUrl = submittedFile.SourceUrl;
                f.Visible = submittedFile.Visible;
                f.Preview = submittedFile.Preview;
                f.OverrideClassification = submittedFile.OverrideClassification;
                f.OverrideEyesOnly = submittedFile.OverrideEyesOnly;
                f.OverrideReleasableTo = submittedFile.OverrideReleasableTo;

                var defaultGroup = _groupRepository.GetDefaultGroups(AmbientContext).Select(g => g.GroupId).ToArray();
                var filteredRelTo = (ISet<Group>) await _groupRepository
                    .GetAllAsync(AmbientContext, new GroupQuery {Id = releasableTo})
                    .Where(_ => !defaultGroup.Contains(_.GroupId)).ToHashSetAsync();
                var filteredEyes = (ISet<Group>) await _groupRepository
                    .GetAllAsync(AmbientContext, new GroupQuery {Id = eyesOnly}).ToHashSetAsync();

                try
                {
                    if (file != null)
                    {
                        f.MimeType = file.ContentType;
                        await using var stream = file.OpenReadStream();
                        var result = await _documentRepository.UpdateFile(AmbientContext, f, stream,
                            filteredRelTo, filteredEyes);
                    }
                    else
                    {
                        var result = await _documentRepository.UpdateFile(AmbientContext, f,
                            releasableTo: filteredRelTo,
                            eyesOnly: filteredEyes);
                    }

                    _logger.Log(LogLevel.Warning, EventIDs.UpdateFileSuccessful,
                        new LogEvent(
                                $"User '{currentUser.UserName}' successfully edited file '{id}'.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext),
                        null,
                        LogEvent.Formatter);

                    await AmbientContext.DatabaseContext.SaveChangesAsync();
                }
                catch (InvalidArgumentException e)
                {
                    ModelState.Clear();
                    foreach (var kv in e.Errors)
                    foreach (var errorMessage in kv.Value)
                        ModelState.AddModelError(kv.Key, errorMessage);

                    _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                        new LogEvent(
                                $"User '{currentUser.UserName}' attempted to edit file '{id}' with an invalid model.")
                            .AddUser(currentUser)
                            .AddHttpContext(_accessor.HttpContext),
                        null,
                        LogEvent.Formatter);

                    var allGroups = await _groupRepository.GetAllAsync(AmbientContext).ToListAsync();
                    ViewBag.OwnGroups = allGroups.Where(_ => currentUser.Memberships.Any(__ => __.GroupId == _.GroupId));
                    ViewBag.DefaultGroups = _groupRepository.GetDefaultGroups(AmbientContext);
                    ViewBag.AllGroups = Enumerable.Except(allGroups, ViewBag.DefaultGroups);

                    return View(f);
                }

                return RedirectToAction("Details", new {id = f.FileId});
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit file '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to view update a non-existing file '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("file.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("File/Delete")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                await AmbientContext.DatabaseContext.Entry(currentUser).Collection(u => u.Memberships).LoadAsync();

                var f = await _documentRepository.GetFileAsync(AmbientContext, id, new[] {"Document"});
                if (!await _appAuthorizationService.CanDeleteDocumentFile(User, f))
                    throw new UnauthorizedOperationException();

                return View(f);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to edit file '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to view update a non-existing file '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("file.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpPost("File/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([Bind(Prefix = "FileId")] Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var file = await _documentRepository.DeleteFile(AmbientContext, id);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new {id = file.DocumentId});
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to delete file '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.UpdateFileFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing file '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("file.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        private List<SelectListItem> GetAvailableClassifications()
        {
            var listItems = new List<SelectListItem>();
            listItems.AddRange(
                AmbientContext.DatabaseContext.Classifications.AsQueryable().Select(_ => new SelectListItem
                {
                    Text = _.Title + (string.IsNullOrEmpty(_.Subtitle) ? "" : "(" + _.Subtitle + ")"),
                    Value = _.ClassificationId.ToString()
                })
            );
            return listItems;
        }

        public async Task<IActionResult> Index(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                _logger.Log(LogLevel.Information, EventIDs.ListFilesSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully list the files.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return View(await _documentRepository.GetAsync(AmbientContext, id, new[] {"Files"}));
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.ListFilesFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to list files without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
        }

        [HttpGet("File/Details/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            var currentUser = await GetCurrentUser();

            try
            {
                var file = await _documentRepository.GetFileAsync(AmbientContext,
                    id,
                    new[] {nameof(DocumentFile.Document)});

                _logger.Log(LogLevel.Information, EventIDs.DetailsFileSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully viewed details of file '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddFile(file),
                    null,
                    LogEvent.Formatter);

                return View(file);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.DetailsFileFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of file '{id}' without legitimate rights.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("file.id", id),
                    null,
                    LogEvent.Formatter);

                return Unauthorized();
            }
            catch (NotFoundEntityException)
            {
                _logger.Log(LogLevel.Warning, EventIDs.DetailsFileFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to view details of a non-existing file '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("file.id", id),
                    null,
                    LogEvent.Formatter);

                return NotFound();
            }
        }

        [HttpGet("Document/Download/{id}")]
        public async Task<IActionResult> Download(Guid id)
        {
            var currentUser = await GetCurrentUser();
            try
            {
                var documentFile = await _documentRepository.GetFileAsync(AmbientContext, id);
                var mimetype = documentFile.MimeType;

                var memory = new MemoryStream();
                var filepath = Path.Combine(_configuration.DocFolder, documentFile.Filepath);
                if (System.IO.File.Exists(filepath))
                {
                    if (documentFile.MimeType == "text/plain")
                    {
                        memory.Write(Encoding.Default.GetBytes("<html>\n<head><meta charset=\"UTF-8\" />"));
                        memory.Write(Encoding.Default.GetBytes(
                            "<link rel=\"stylesheet\" media=\"screen, print\" href=\"/dist/main.css\" />\n"));
                        memory.Write(Encoding.Default.GetBytes("<script src=\"/dist/main.js\"></script>\n"));
                        memory.Write(Encoding.Default.GetBytes("</head>\n<body><pre><code>"));
                        await using var stream = new FileStream(filepath, FileMode.Open);
                        await stream.CopyToAsync(memory);
                        memory.Write(Encoding.Default.GetBytes("</code></pre>\n"));
                        memory.Write(Encoding.Default.GetBytes("<script>DocIntel.initApp();</script>\n"));
                        memory.Write(Encoding.Default.GetBytes("</body>\n</html>"));
                        mimetype = "text/html";
                    }
                    else if (documentFile.MimeType == "text/html")
                    {
                        using var stream = new StreamReader(filepath, Encoding.UTF8);
                        var content = await stream.ReadToEndAsync();
                        memory.Write(Encoding.UTF8.GetBytes(content));
                    }
                    else
                    {
                        await using var stream = new FileStream(filepath, FileMode.Open);
                        await stream.CopyToAsync(memory);
                    }
                }
                else
                {
                    _logger.LogDebug($"File not found at '{filepath}'.");
                    return NotFound();
                }

                memory.Position = 0;

                var contentDisposition = new ContentDisposition
                {
                    FileName = Path.GetFileName(documentFile.Filename),
                    Inline = true
                };
                Response.Headers.Add("Content-Disposition", contentDisposition.ToString());
                Response.Headers.Add("X-Content-Type-Options", "nosniff");

                return File(memory, mimetype);
            }
            catch (UnauthorizedOperationException)
            {
                _logger.Log(LogLevel.Warning,
                    EventIDs.DownloadFailed,
                    new LogEvent(
                            $"User '{currentUser.UserName}' attempted to download '{id}' without legitimate rights.")
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
                    EventIDs.DownloadFailed,
                    new LogEvent($"User '{currentUser.UserName}' attempted to download non-existing document '{id}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddProperty("document.id", id),
                    null,
                    LogEvent.Formatter);
                return NotFound();
            }
        }
    }
}
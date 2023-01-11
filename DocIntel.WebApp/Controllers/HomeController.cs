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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Search.Documents;
using DocIntel.WebApp.ViewModels;
using DocIntel.WebApp.ViewModels.HomeViewModel;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DocIntel.WebApp.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly ITagRepository _tagRepository;
        private readonly ISourceRepository _sourceRepository;

        public HomeController(DocIntelContext context,
            ApplicationSettings configuration,
            AppUserManager userManager,
            IAuthorizationService authorizationService,
            IDocumentRepository documentRepository, ITagRepository tagRepository, ISourceRepository sourceRepository)
            : base(context,
                userManager,
                configuration,
                authorizationService)
        {
            _documentRepository = documentRepository;
            _tagRepository = tagRepository;
            _sourceRepository = sourceRepository;
        }

        public async Task<IActionResult> Index()
        {
            var documents = (await GetFromSubscribedTags(AmbientContext, AmbientContext.CurrentUser, DateTime.Now))
                .Union(await GetFromSubscribedSources(AmbientContext, AmbientContext.CurrentUser, DateTime.Now))
                .Distinct()
                .OrderBy(_ => _.RegistrationDate)
                .Take(15);

            return View(new DashboardViewModel
            {
                NewsFeed = documents,
                RecentDocs = _documentRepository.GetAllAsync(AmbientContext, new DocumentQuery() { OrderBy = SortCriteria.DocumentDate, Limit = 10 }, new []{ "DocumentTags", "DocumentTags.Tag", "DocumentTags.Tag.Facet", "Source" }).ToEnumerable()
            });
        }

        // TODO I don't think this works
        // TODO Add structured logging
        public IActionResult Error(int code)
        {
            if (HttpContext.Response.StatusCode == 404)
                return View("Error404");

            var viewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (exceptionFeature != null)
            {
                var routeWhereExceptionOccurred = exceptionFeature.Path;
                var exceptionThatOccurred = exceptionFeature.Error;

                viewModel.ExceptionName = exceptionThatOccurred.Message;
                viewModel.StackTrace = exceptionThatOccurred.StackTrace;
                viewModel.Time = DateTime.Now;
                viewModel.Route = routeWhereExceptionOccurred;
            }

            return View(viewModel);
        }
        
        private async Task<IEnumerable<Document>> GetFromSubscribedTags(AmbientContext ambientContext,
            AppUser user,
            DateTime date)
        {
            var tagSubscriptions = await _tagRepository.GetSubscriptionsAsync(ambientContext, user, 0, -1).ToListAsync();
            
            DocumentQuery query = new DocumentQuery
            {
                TagIds = tagSubscriptions.Select(_ => _.tag.TagId).ToArray(),
                Page = 0,
                Limit = 15,
                ExcludeMuted = true,
                OrderBy = SortCriteria.RegistrationDate
            };

            return await _documentRepository.GetAllAsync(ambientContext, query, new []{ "DocumentTags", "DocumentTags.Tag", "DocumentTags.Tag.Facet", "Source" }).ToListAsync();
        }
        
        
        private async Task<IEnumerable<Document>> GetFromSubscribedSources(AmbientContext ambientContext,
            AppUser user,
            DateTime date)
        {
            var documentSubscriptions =
                _sourceRepository.GetSubscriptionsAsync(ambientContext, user, 0, -1).ToEnumerable();
        
            DocumentQuery query = new DocumentQuery
            {
                SourceIds = documentSubscriptions.Select(_ => _.source.SourceId).ToArray(),
                Page = 0,
                Limit = 15,
                ExcludeMuted = true,
                OrderBy = SortCriteria.RegistrationDate
            };

            return await _documentRepository.GetAllAsync(ambientContext, query, new []{ "DocumentTags", "DocumentTags.Tag", "DocumentTags.Tag.Facet", "Source" }).ToListAsync();
        
        }
    }
}
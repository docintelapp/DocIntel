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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.Query;
using DocIntel.Core.Settings;
using DocIntel.Core.Utils.Indexation;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocIntel.Services.ContinuousIndexing
{
    /// <summary>
    /// Continuous indexing is needed to keep the document count and last document date up to date in the source index
    /// without re-indexing the source every time a new document is registered or a document is updated. The information
    /// is not update real-time for performance reasons.
    /// </summary>
    internal class ContinuousIndexer
    {
        private readonly ISourceRepository _sourceRepository;
        private readonly ISourceIndexingUtility _sourceIndexingUtility;
        private readonly ITagRepository _tagRepository;
        private readonly ITagIndexingUtility _tagIndexingUtility;
        private readonly ApplicationSettings _applicationSettings;
        private readonly IUserClaimsPrincipalFactory<AppUser> _userClaimsPrincipalFactory;
        private readonly IServiceProvider _serviceProvider;

        public ContinuousIndexer(ISourceRepository sourceRepository,
            ISourceIndexingUtility sourceIndexingUtility,
            DocIntelContext context,
            ApplicationSettings applicationSettings,
            IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory, ITagRepository tagRepository, ITagIndexingUtility tagIndexingUtility, IServiceProvider serviceProvider)
        {
            _sourceRepository = sourceRepository;
            _sourceIndexingUtility = sourceIndexingUtility;
            _applicationSettings = applicationSettings;
            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
            _tagRepository = tagRepository;
            _tagIndexingUtility = tagIndexingUtility;
            _serviceProvider = serviceProvider;
        }
        
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                AmbientContext ambientContext = GetAmbientContext();
                await foreach (var source in _sourceRepository.GetAllAsync(ambientContext,
                    new SourceQuery() {Limit = -1}))
                {
                    _sourceIndexingUtility.Update(source);
                }
                await foreach (var tag in _tagRepository.GetAllAsync(ambientContext,
                    new TagQuery() {Limit = -1} ,new []{ "Facet", "Documents" }))
                {
                    _tagIndexingUtility.Update(tag);
                }
                await Task.Delay(TimeSpan.FromDays(1), cancellationToken);
            }
        }

        private AmbientContext GetAmbientContext()
        {
            var dbContextOptions = _serviceProvider.GetRequiredService<DbContextOptions<DocIntelContext>>();
            var dbContextLogger = _serviceProvider.GetRequiredService<ILogger<DocIntelContext>>();
            var _dbContext = new DocIntelContext(dbContextOptions, dbContextLogger);
            var automationUser =
                _dbContext.Users.AsNoTracking().FirstOrDefault(_ => _.UserName == _applicationSettings.AutomationAccount);
            if (automationUser == null)
                throw new ArgumentNullException($"User '{_applicationSettings.AutomationAccount}' does not exists.");

            var claims = _userClaimsPrincipalFactory.CreateAsync(automationUser).Result;
            var ambientContext = new AmbientContext
            {
                DatabaseContext = _dbContext,
                Claims = claims,
                CurrentUser = automationUser
            };
            return ambientContext;
        }
    }
}
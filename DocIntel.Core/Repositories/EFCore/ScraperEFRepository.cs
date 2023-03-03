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
using System.Linq;
using System.Threading.Tasks;

using DocIntel.Core.Authorization;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Messages;
using DocIntel.Core.Models;

using MassTransit;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace DocIntel.Core.Repositories.EFCore
{
    /// <summary>
    ///     Provides an implementation of <see cref="IScraperRepository" />
    ///     based on Entity Framework Core. The operations will
    ///     be announced with their respective message on the specified RabbitMQ bus.
    /// </summary>
    public class ScraperEFRepository : DefaultEFRepository<Scraper>, IScraperRepository
    {
        /// <summary>
        ///     Initialize a new instance of <see cref="ScraperEFRepository" />.
        /// </summary>
        /// <param name="busClient">The bus on which operations shall be announced.</param>
        /// <param name="appAuthorizationService">The service for checking permissions of users.</param>
        public ScraperEFRepository(IPublishEndpoint busClient,
            IAppAuthorizationService appAuthorizationService)
        : base(context => context.DatabaseContext.Scrapers,
            busClient, appAuthorizationService)
        {
        }

        /// <summary>
        ///     Adds the feed to the corresponding table in the database. The
        ///     addition is announced with a <see cref="ScraperCreatedMessage" />
        ///     on the bus.
        /// </summary>
        /// <param name="feed">The feed to add to the database.</param>
        /// <param name="user">The user requesting the addition.</param>
        /// <returns>A task representing the operation</returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right create a new
        ///     scraper in the repository.
        /// </exception>
        /// <exception cref="InvalidArgumentException">
        ///     Thrown when the specified feed is not valid according to its
        ///     data annotations.
        /// </exception>
        public async Task<Scraper> CreateAsync(AmbientContext ambientContext,
            Scraper feed)
        {
            if (!await _appAuthorizationService.CanCreateScraper(ambientContext.Claims, feed))
            {
                throw new UnauthorizedOperationException();
            }

            var modelErrors = new List<ValidationResult>();
            if (IsValid(feed, modelErrors))
            {
                var trackingEntity = await ambientContext.DatabaseContext.AddAsync(feed);
                PublishMessage(ambientContext, new ScraperCreatedMessage
                {
                    ScraperId = trackingEntity.Entity.ScraperId
                });
                return trackingEntity.Entity;
            }

            throw new InvalidArgumentException(modelErrors);
        }

        /// <summary>
        ///     Checks if the specified feed identifier is known in the database.
        /// </summary>
        /// <param name="feedId">The feed identifier to check for.</param>
        /// <param name="user">The user requesting the check.</param>
        /// <returns>
        ///     <c>True</c> if the feed exists and the user has the right
        ///     to view the feed, <c>False</c> otherwise.
        /// </returns>
        public async Task<bool> ExistsAsync(AmbientContext ambientContext, Guid feedId)
        {
            var feed = await ambientContext.DatabaseContext.Scrapers.FindAsync(feedId);
            if (feed != null) return await _appAuthorizationService.CanViewScraper(ambientContext.Claims, feed);

            return false;
        }

        /// <summary>
        ///     Returns the feed corresponding to the specified feed identifier is
        ///     known in the database.
        /// </summary>
        /// <param name="feedId">
        ///     The feed identifier for the feed to retreive.
        /// </param>
        /// <param name="user">The user requesting the feed.</param>
        /// <returns>
        ///     The feed if it exists and the user has the right
        ///     to view the feed.
        /// </returns>
        /// <param name="includeRelatedData">
        ///     An array of string
        ///     representing the related data to be included in the results
        ///     of the query.
        /// </param>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right view the requested
        ///     feed.
        /// </exception>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified feed is not known in the repository.
        /// </exception>
        public async Task<Scraper> GetAsync(AmbientContext ambientContext, Guid feedId,
            Func<IQueryable<Scraper>,IQueryable<Scraper>> includes = null)
        {
            IQueryable<Scraper> enumerable = ambientContext.DatabaseContext.Scrapers;

            if (includes != null)
                enumerable = includes(enumerable);

            var feed = enumerable.SingleOrDefault(_ => _.ScraperId == feedId);

            if (feed == null)
                throw new NotFoundEntityException();

            if (await _appAuthorizationService.CanViewScraper(ambientContext.Claims, feed))
                return feed;
            throw new UnauthorizedOperationException();
        }

        /// <summary>
        ///     Returns all the feeds known in the database that the user is allowed
        ///     to view.
        /// </summary>
        /// ///
        /// <param name="user">The user requesting the listing.</param>
        /// <param name="includeRelatedData">
        ///     An array of string
        ///     representing the related data to be included in the results
        ///     of the query.
        /// </param>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right view the
        ///     scraper in the repository.
        /// </exception>
        /// <returns>
        ///     The feeds in the database that the user is allowed to view.
        /// </returns>
        public async IAsyncEnumerable<Scraper> GetAllAsync(AmbientContext ambientContext,
            Func<IQueryable<Scraper>,IQueryable<Scraper>> query = null,
            Func<IQueryable<Scraper>,IQueryable<Scraper>> includes = null,
            int page = 0, int limit = 25)
        {
            IQueryable<Scraper> enumerable = ambientContext.DatabaseContext.Scrapers;

            if (includes != default)
                enumerable = includes(enumerable);
            
            if (query != default)
                enumerable = query(enumerable);

            var filteredFeeds = BuildQuery(enumerable, page, limit);

            foreach (var feed in filteredFeeds)
                if (await _appAuthorizationService.CanViewScraper(ambientContext.Claims, feed))
                    yield return feed;
        }

        /// <summary>
        ///     Removes an existing scraper from the database. The
        ///     removal is announced with a <see cref="ScraperRemovedMessage" />
        ///     on the bus.
        /// </summary>
        /// <param name="feedId">
        ///     The identifier of the feed to remove from the
        ///     repository.
        /// </param>
        /// <param name="user">The user requesting the removal.</param>
        /// <returns>A task representing the operation.</returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right remove the
        ///     scraper from the database.
        /// </exception>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified feed is not known in the database.
        /// </exception>
        public async Task<Scraper> RemoveAsync(AmbientContext ambientContext,
            Guid feedId)
        {
            var feed = await ambientContext.DatabaseContext.Scrapers.FindAsync(feedId);
            if (feed == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanDeleteScraper(ambientContext.Claims, feed))
                throw new UnauthorizedOperationException();

            var trackingEntity = ambientContext.DatabaseContext.Remove(feed);
            PublishMessage(ambientContext, new ScraperRemovedMessage
            {
                ScraperId = trackingEntity.Entity.ScraperId
            });
            
            return trackingEntity.Entity;
        }

        /// <summary>
        ///     Updates an existing scraper in the database. The
        ///     removal is announced with a <see cref="ScraperUpdatedMessage" />
        ///     on the bus.
        /// </summary>
        /// <param name="feed">The feed to update to the database.</param>
        /// <param name="user">The user requesting the update.</param>
        /// <returns>A task representing the operation.</returns>
        /// <exception cref="UnauthorizedOperationException">
        ///     Thrown when the user does not have the right update the
        ///     scraper in the database.
        /// </exception>
        /// <exception cref="InvalidArgumentException">
        ///     Thrown when the specified feed is not valid according to its
        ///     data annotations.
        /// </exception>
        /// <exception cref="NotFoundEntityException">
        ///     Thrown when the specified feed is not known in the database.
        /// </exception>
        public async Task<Scraper> UpdateAsync(AmbientContext ambientContext,
            Scraper feed)
        {
            var retrievedFeed = await ambientContext.DatabaseContext.Scrapers.FindAsync(feed.ScraperId);
            if (retrievedFeed == null)
                throw new NotFoundEntityException();

            if (!await _appAuthorizationService.CanEditScraper(ambientContext.Claims, retrievedFeed))
                throw new UnauthorizedOperationException();

            var modelErrors = new List<ValidationResult>();
            if (IsValid(feed, modelErrors))
            {
                var trackingEntity = ambientContext.DatabaseContext.Update(feed);
                PublishMessage(ambientContext, new ScraperUpdatedMessage
                {
                    ScraperId = trackingEntity.Entity.ScraperId
                });
                return trackingEntity.Entity;
            }

            throw new InvalidArgumentException(modelErrors);
        }

        private static IQueryable<Scraper> BuildQuery(IQueryable<Scraper> scrapers,
            int page, int limit)
        {
            if ((page > 0) & (limit > 0))
                scrapers = scrapers.Skip((page - 1) * limit).Take(limit);

            return scrapers;
        }
    }
}
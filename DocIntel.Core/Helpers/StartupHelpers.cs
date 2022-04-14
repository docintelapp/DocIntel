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

using DocIntel.Core.Authorization;
using DocIntel.Core.Authorization.Handlers;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.EFCore;
using DocIntel.Core.Repositories.ElasticSearch;
using DocIntel.Core.Utils;
using DocIntel.Core.Utils.ContentExtraction;
using DocIntel.Core.Utils.Indexation;
using DocIntel.Core.Utils.Indexation.SolR;
using DocIntel.Core.Utils.Observables;
using DocIntel.Core.Utils.Search.Documents;
using DocIntel.Core.Utils.Search.Sources;
using DocIntel.Core.Utils.Search.Tags;
using DocIntel.Core.Utils.Thumbnail;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Nest;

using SolrNet;

namespace DocIntel.Core.Helpers
{
    /// <summary>
    ///     Provides helpers for registering the services needed for DocIntel and its components.
    /// </summary>
    public class StartupHelpers
    {
        /// <summary>
        ///     Register all services required for DocIntel.
        /// </summary>
        /// <param name="services">The service collection</param>
        public static void RegisterServices(IServiceCollection services)
        {
            RegisterAuthorizationHandlers(services);
            RegisterSearchServices(services);
            RegisterIndexingServices(services);
            RegisterRepositories(services);

            services.AddTransient<MailKitEmailSender>();
        }

        /// <summary>
        ///     Register the services related to search.
        /// </summary>
        /// <param name="services">The service collection</param>
        public static void RegisterSearchServices(IServiceCollection services)
        {
            services.AddScoped<ITagSearchService, SolRTagSearchEngine>();
            services.AddScoped<ISourceSearchService, SolRSourceSearchEngine>();
            services.AddScoped<IDocumentSearchEngine, SolRDocumentSearchEngine>();
        }

        public static void RegisterSolR(IServiceCollection serviceCollection)
        {
            // TODO Move strings to configuration
            serviceCollection.AddSolrNet("http://localhost:8983/solr");
            serviceCollection.AddSolrNet<IndexedDocument>("http://localhost:8983/solr/document");
            serviceCollection.AddSolrNet<IndexedTag>("http://localhost:8983/solr/tag");
            serviceCollection.AddSolrNet<IndexedTagFacet>("http://localhost:8983/solr/facet");
            serviceCollection.AddSolrNet<IndexedSource>("http://localhost:8983/solr/source");
        }

        public static void RegisterElastic(IServiceCollection serviceCollection)
        {
            var settings = new ConnectionSettings();
            var elasticClient = new ElasticClient(settings);
            serviceCollection.AddSingleton(elasticClient);
        }

        /// <summary>
        ///     Register the services related to indexing.
        /// </summary>
        /// <param name="services">The service collection</param>
        public static void RegisterIndexingServices(IServiceCollection services)
        {
            services.AddScoped<IContentExtractionUtility, SolrContentExtractionUtility>();
            services.AddScoped<ISourceIndexingUtility, SolRSourceIndexingUtility>();
            services.AddScoped<IDocumentIndexingUtility, SolRDocumentIndexingUtility>();
            services.AddScoped<ITagIndexingUtility, SolRTagIndexingUtility>();
            services.AddScoped<ITagFacetIndexingUtility, SolRTagFacetIndexingUtility>();
            services.AddScoped<IThumbnailUtility, PDF2PPMThumbnailUtility>();
            services.AddScoped<TagUtility, TagUtility>();
        }

        /// <summary>
        ///     Register the services related to authorization.
        /// </summary>
        /// <param name="services">The service collection</param>
        public static void RegisterAuthorizationHandlers(IServiceCollection services)
        {
            services.AddScoped<IAppAuthorizationService, AppAuthorizationService>();
            services.AddScoped<IAuthorizationHandler, AppUserAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, AppRoleAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, CommentAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, DocumentAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, TagAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, FacetTagAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, SourceAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, IncomingFeedAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, ImportRuleSetAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, ImportRuleAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, GroupAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, ClassificationAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, DocumentFileAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, ScraperAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, NullAuthorizationHandler>();
        }

        /// <summary>
        ///     Register all repositories.
        /// </summary>
        /// <param name="services">The service collection</param>
        public static void RegisterRepositories(IServiceCollection services)
        {
            services.AddScoped<IRoleRepository, RoleEFRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ISourceRepository, SourceEFRepository>();
            services.AddScoped<ICommentRepository, CommentEFRepository>();
            services.AddScoped<ITagRepository, TagEFRepository>();
            services.AddScoped<ITagFacetRepository, TagFacetEFRepository>();
            services.AddScoped<IIncomingFeedRepository, IncomingFeedEFRepository>();
            services.AddScoped<IDocumentRepository, DocumentEFRepository>();
            services.AddScoped<IGroupRepository, GroupEFRepository>();
            services.AddScoped<IClassificationRepository, ClassificationEFRepository>();
            services.AddScoped<IScraperRepository, ScraperEFRepository>();
            services.AddScoped<IImportRuleRepository, ImportRuleEFRepository>();
            services.AddScoped<IObservableRepository, ObservableESRepository>();
            
            services.AddScoped<IObservablesUtility, ObservablesUtility>();
            services.AddScoped<IObservablesExtractionUtility, RegexObservablesExtractionUtility>();
            services.AddScoped<IObservableWhitelistUtility, ObservableWhitelistUtility>();
        }
    }
}
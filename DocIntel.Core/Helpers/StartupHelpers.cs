/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau, Kevin Menten
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
using System.Net;
using System.Net.Http;
using DocIntel.Core.Authorization;
using DocIntel.Core.Authorization.Handlers;
using DocIntel.Core.Modules;
using DocIntel.Core.Repositories;
using DocIntel.Core.Repositories.EFCore;
using DocIntel.Core.Settings;
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
using SolrNet;
using SolrNet.Impl;
using Synsharp.Telepath;

namespace DocIntel.Core.Helpers
{
    /// <summary>
    ///     Provides helpers for registering the services needed for DocIntel and its components.
    /// </summary>
    public class StartupHelpers
    {
        /// <summary>
        ///     Register all external modules.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="applicationSettings"></param>
        public static void RegisterModules(IServiceCollection services, ApplicationSettings applicationSettings)
        {
            services.AddTransient<ModuleFactory>();
            ModuleFactory.Register(applicationSettings);
        }

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
            services.AddScoped<IFacetSearchService, SolrFacetSearchEngine>();
            services.AddScoped<ISourceSearchService, SolRSourceSearchEngine>();
            services.AddScoped<IDocumentSearchEngine, SolRDocumentSearchEngine>();
        }

        public static void RegisterSolR(IServiceCollection serviceCollection, ApplicationSettings applicationSettings)
        {
            var settings = applicationSettings.Solr;
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = (DecompressionMethods.GZip | DecompressionMethods.Deflate),
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            
            if (!string.IsNullOrEmpty(applicationSettings.Proxy))
                handler.Proxy = new WebProxy()
                {
                    Address = new Uri(applicationSettings.Proxy ?? ""),
                    BypassList = new[] { applicationSettings.NoProxy ?? ""}
                };
            
            var client = new HttpClient(handler);
            
            // TODO Move strings to configuration
            serviceCollection.AddSolrNet($"{settings.Uri}/solr", 
                null, 
                (url) => new AutoSolrConnection(url, client, new InsecureHttpWebRequestFactory()));
            
            serviceCollection.AddSolrNet<IndexedDocument>($"{settings.Uri}/solr/document", null, (url) => new AutoSolrConnection(url, client, new InsecureHttpWebRequestFactory()));
            serviceCollection.AddSolrNet<IndexedTag>($"{settings.Uri}/solr/tag", null, (url) => new AutoSolrConnection(url, client, new InsecureHttpWebRequestFactory()));
            serviceCollection.AddSolrNet<IndexedTagFacet>($"{settings.Uri}/solr/facet", null, (url) => new AutoSolrConnection(url, client, new InsecureHttpWebRequestFactory()));
            serviceCollection.AddSolrNet<IndexedSource>($"{settings.Uri}/solr/source", null, (url) => new AutoSolrConnection(url, client, new InsecureHttpWebRequestFactory()));
        }

        /// <summary>
        ///     Register the services related to indexing.
        /// </summary>
        /// <param name="services">The service collection</param>
        public static void RegisterIndexingServices(IServiceCollection services)
        {
            // Adds all text transforms, extractors and postprocessors for the observable extraction pipeline.
            // I'm lazy and I don't want to add them by hand.
            foreach (Type t in AppDomain.CurrentDomain.GetAssemblies().SelectMany(_ => _.GetTypes())
                         .Where(p => p.IsClass & !p.IsAbstract & (typeof(ITextTransform).IsAssignableFrom(p) |
                                     typeof(IExtractor).IsAssignableFrom(p) |
                                     typeof(IPostProcessor).IsAssignableFrom(p))))
            {
                services.AddSingleton(t);
            }
            services.AddSingleton<IObservablesUtility, DefaultObservableUtility>();
            services.AddScoped<DocumentAnalyzerUtility,DocumentAnalyzerUtility>();
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
            services.AddScoped<IAuthorizationHandler, SavedSearchAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, NullAuthorizationHandler>();
        }

        /// <summary>
        ///     Register all repositories.
        /// </summary>
        /// <param name="services">The service collection</param>
        public static void RegisterRepositories(IServiceCollection services)
        {
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
            services.AddScoped<ISavedSearchRepository, SavedSearchEFRepository>();
        }

        public static void RegisterSynapse(IServiceCollection services, ApplicationSettings applicationSettings)
        {
            services.AddSingleton(applicationSettings.Synapse);
            services.AddScoped<TelepathClient>(provider =>
            {
                var settings = provider.GetRequiredService<SynapseSettings>();
                var uri = new UriBuilder(settings.URL);
                uri.UserName = settings.UserName;
                uri.Password = settings.Password;
                return new TelepathClient(uri.ToString());
            });
            services.AddScoped<ISynapseRepository, SynapseRepository>();
        }
    }
}
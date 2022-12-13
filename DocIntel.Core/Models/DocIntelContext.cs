/*
 * DocIntel
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
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocIntel.Core.Models
{
    public class DocIntelContext : IdentityDbContext<AppUser,
        AppRole,
        string,
        IdentityUserClaim<string>,
        IdentityUserRole<string>,
        IdentityUserLogin<string>,
        IdentityRoleClaim<string>,
        IdentityUserToken<string>>
    {
        private readonly ILogger<DocIntelContext> _logger;

        public DocIntelContext(DbContextOptions<DocIntelContext> options, ILogger<DocIntelContext> logger)
            : base(options)
        {
            OnSaveCompleteTasks = new ConcurrentBag<Func<Task>>();
            _logger = logger;
        }

        public ConcurrentBag<Func<Task>> OnSaveCompleteTasks { get; set; }

        public virtual DbSet<Document> Documents { get; set; }
        public virtual DbSet<DocumentFile> Files { get; set; }

        public virtual DbSet<Tag> Tags { get; set; }
        public virtual DbSet<TagFacet> Facets { get; set; }

        public DbSet<DocumentTag> DocumentTag { get; set; }

        public DbSet<UserTagSubscription> UserTagSubscriptions { get; set; }
        public DbSet<UserFacetSubscription> UserFacetSubscriptions { get; set; }

        public virtual DbSet<Source> Sources { get; set; }
        public DbSet<Comment> Comments { get; set; }

        public DbSet<UserDocumentSubscription> UserDocumentSubscription { get; set; }
        public DbSet<UserSourceSubscription> UserSourceSubscription { get; set; }

        public DbSet<APIKey> APIKeys { get; set; }
        public DbSet<Importer> IncomingFeeds { get; set; }
        public DbSet<Scraper> Scrapers { get; set; }
        public DbSet<ImportRuleSet> ImportRuleSets { get; set; }
        public DbSet<ImportRule> ImportRules { get; set; }

        public DbSet<Group> Groups { get; set; }
        public DbSet<Member> Members { get; set; }
        
        public DbSet<Classification> Classifications { get; set; }
        public DbSet<SubmittedDocument> SubmittedDocuments { get; set; }

        public override int SaveChanges()
        {
            var result = base.SaveChanges();
            _logger.LogTrace("SaveChanges return status: {0}", result);

            foreach (var task in OnSaveCompleteTasks)
            {
                task().Wait();
            }

            _logger.LogTrace("Completed SaveChanges tasks");

            OnSaveCompleteTasks = new ConcurrentBag<Func<Task>>();

            return result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var saveTask = await base.SaveChangesAsync(cancellationToken);
            _logger.LogTrace("SaveChangesAsync return status: {0}", saveTask);

            foreach (var task in OnSaveCompleteTasks)
            {
                await task();
            }
            _logger.LogTrace("Completed SaveChangesAsync tasks");

            OnSaveCompleteTasks = new ConcurrentBag<Func<Task>>();

            return saveTask;
        }

        [ExcludeFromCodeCoverage]
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DocumentTag>()
                .HasKey(t => new {t.DocumentId, t.TagId});

            modelBuilder.Entity<UserTagSubscription>()
                .HasKey(t => new {t.UserId, t.TagId});

            modelBuilder.Entity<UserFacetSubscription>()
                .HasKey(t => new {t.UserId, t.FacetId});

            modelBuilder.Entity<UserDocumentSubscription>()
                .HasKey(t => new {t.UserId, t.DocumentId});

            modelBuilder.Entity<UserSourceSubscription>()
                .HasKey(t => new {t.UserId, t.SourceId});

            modelBuilder.Entity<TagFacet>()
                .HasIndex(_ => _.Prefix).IsUnique();

            modelBuilder.Entity<Tag>()
                .HasIndex(_ => _.FacetId);

            modelBuilder.Entity<Tag>()
                .HasIndex(_ => new {_.FacetId, _.Label}).IsUnique();

            modelBuilder.Entity<Document>().Property(_ => _.URL).IsRequired();
            modelBuilder.Entity<Document>().HasIndex(_ => _.URL).IsUnique();

            modelBuilder.Entity<Tag>().Property(_ => _.URL).IsRequired();
            modelBuilder.Entity<Tag>().HasIndex(_ => _.URL);

            modelBuilder.Entity<Source>().Property(_ => _.URL).IsRequired();
            modelBuilder.Entity<Source>().HasIndex(_ => _.URL).IsUnique();

            modelBuilder.Entity<Document>().Property(_ => _.SequenceId).IsRequired();
            modelBuilder.Entity<Document>().Property(_ => _.Title).IsRequired();
            modelBuilder.Entity<Document>().Property(_ => _.Reference).IsRequired();

            modelBuilder.Entity<Member>().HasAlternateKey(_ => new {_.UserId, _.GroupId});

            modelBuilder.Entity<Document>().HasMany(_ => _.ReleasableTo).WithMany(_ => _.DocumentsReleasableTo).UsingEntity(_ => _.ToTable("DocumentRelToGroup"));
            modelBuilder.Entity<Document>().HasMany(_ => _.EyesOnly).WithMany(_ => _.DocumentsEyesOnly).UsingEntity(_ => _.ToTable("DocumentGroupEyesOnly"));
            modelBuilder.Entity<DocumentFile>().HasMany(_ => _.ReleasableTo).WithMany(_ => _.FilesReleasableTo).UsingEntity(_ => _.ToTable("FileRelToGroup"));
            modelBuilder.Entity<DocumentFile>().HasMany(_ => _.EyesOnly).WithMany(_ => _.FilesEyesOnly).UsingEntity(_ => _.ToTable("FileGroupEyesOnly"));

            modelBuilder.Entity<Scraper>().HasMany(_ => _.ReleasableTo).WithMany(_ => _.ScraperReleasableTo).UsingEntity(_ => _.ToTable("ScraperRelToGroup"));
            modelBuilder.Entity<Scraper>().HasMany(_ => _.EyesOnly).WithMany(_ => _.ScraperEyesOnly).UsingEntity(_ => _.ToTable("ScraperGroupEyesOnly"));

            modelBuilder.Entity<Importer>().HasMany(_ => _.ReleasableTo).WithMany(_ => _.ImporterReleasableTo).UsingEntity(_ => _.ToTable("ImporterRelToGroup"));
            modelBuilder.Entity<Importer>().HasMany(_ => _.EyesOnly).WithMany(_ => _.ImporterEyesOnly).UsingEntity(_ => _.ToTable("ImporterGroupEyesOnly"));

            modelBuilder.Entity<SubmittedDocument>().HasMany(_ => _.ReleasableTo).WithMany(_ => _.SubmittedDocumentReleasableTo).UsingEntity(_ => _.ToTable("SubmissionRelToGroup"));
            modelBuilder.Entity<SubmittedDocument>().HasMany(_ => _.EyesOnly).WithMany(_ => _.SubmittedDocumentEyesOnly).UsingEntity(_ => _.ToTable("SubmissionGroupEyesOnly"));

            modelBuilder.Entity<Document>().HasOne(_ => _.Thumbnail).WithOne(_ => _.DocumentThumbnail).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Document>().HasMany(_ => _.Files).WithOne(_ => _.Document);
            
            modelBuilder
                .Entity<AppRole>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();
            
            modelBuilder
                .Entity<AppUser>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();
            
            modelBuilder.Entity<AppRole>().HasOne<AppUser>(u => u.CreatedBy);
            modelBuilder.Entity<AppRole>().HasOne<AppUser>(u => u.LastModifiedBy);
        }
    }
}
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocIntel.Core.Migrations
{
    /// <inheritdoc />
    public partial class MigratesTimestampDataType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""AspNetUsers"" ALTER COLUMN ""LastLogin"" TYPE timestamp with time zone");        
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""AspNetUsers"" ALTER COLUMN ""RegistrationDate"" TYPE timestamp with time zone"); 
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""AspNetUsers"" ALTER COLUMN ""LastActivity"" TYPE timestamp with time zone"); 
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Groups"" ALTER COLUMN ""CreationDate"" TYPE timestamp with time zone");     
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Groups"" ALTER COLUMN ""ModificationDate"" TYPE timestamp with time zone"); 
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""APIKeys"" ALTER COLUMN ""CreationDate"" TYPE timestamp with time zone");     
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""APIKeys"" ALTER COLUMN ""ModificationDate"" TYPE timestamp with time zone"); 
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""APIKeys"" ALTER COLUMN ""LastUsage"" TYPE timestamp with time zone");        
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""AspNetRoles"" ALTER COLUMN ""CreationDate"" TYPE timestamp with time zone");     
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""AspNetRoles"" ALTER COLUMN ""ModificationDate"" TYPE timestamp with time zone"); 
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Facets"" ALTER COLUMN ""CreationDate"" TYPE timestamp with time zone");     
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Facets"" ALTER COLUMN ""ModificationDate"" TYPE timestamp with time zone"); 
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Sources"" ALTER COLUMN ""CreationDate"" TYPE timestamp with time zone");     
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Sources"" ALTER COLUMN ""ModificationDate"" TYPE timestamp with time zone"); 
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""IncomingFeeds"" ALTER COLUMN ""LastCollection"" TYPE timestamp with time zone");   
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Tags"" ALTER COLUMN ""CreationDate"" TYPE timestamp with time zone");     
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Tags"" ALTER COLUMN ""ModificationDate"" TYPE timestamp with time zone"); 
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Comments"" ALTER COLUMN ""CommentDate"" TYPE timestamp with time zone");      
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Comments"" ALTER COLUMN ""ModificationDate"" TYPE timestamp with time zone"); 
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Documents"" ALTER COLUMN ""DocumentDate"" TYPE timestamp with time zone");     
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Documents"" ALTER COLUMN ""RegistrationDate"" TYPE timestamp with time zone"); 
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Documents"" ALTER COLUMN ""ModificationDate"" TYPE timestamp with time zone"); 
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Files"" ALTER COLUMN ""DocumentDate"" TYPE timestamp with time zone");     
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Files"" ALTER COLUMN ""RegistrationDate"" TYPE timestamp with time zone"); 
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Files"" ALTER COLUMN ""ModificationDate"" TYPE timestamp with time zone"); 
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""SubmittedDocuments"" ALTER COLUMN ""SubmissionDate"" TYPE timestamp with time zone");   
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""SubmittedDocuments"" ALTER COLUMN ""IngestionDate"" TYPE timestamp with time zone");    
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Tags"" ALTER COLUMN ""LastIndexDate"" TYPE timestamp with time zone");    
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Sources"" ALTER COLUMN ""LastIndexDate"" TYPE timestamp with time zone");    
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Facets"" ALTER COLUMN ""LastIndexDate"" TYPE timestamp with time zone");    
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""Documents"" ALTER COLUMN ""LastIndexDate"" TYPE timestamp with time zone");    
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""ExportTemplates"" ALTER COLUMN ""Created"" TYPE timestamp with time zone");          
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""ExportTemplates"" ALTER COLUMN ""Modified"" TYPE timestamp with time zone");         
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""SavedSearches"" ALTER COLUMN ""CreationDate"" TYPE timestamp with time zone");     
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""SavedSearches"" ALTER COLUMN ""ModificationDate"" TYPE timestamp with time zone"); 
            migrationBuilder.Sql(@"SET TimeZone='UTC'; ALTER TABLE ""UserSavedSearches"" ALTER COLUMN ""LastNotification"" TYPE timestamp with time zone"); 
            
            migrationBuilder.Sql(@"ALTER TABLE ""Sources"" ALTER COLUMN ""LastIndexDate"" SET DEFAULT '-infinity'::timestamp with time zone");
            migrationBuilder.Sql(@"ALTER TABLE ""Tags"" ALTER COLUMN ""LastIndexDate"" SET DEFAULT '-infinity'::timestamp with time zone");
            migrationBuilder.Sql(@"ALTER TABLE ""Facets"" ALTER COLUMN ""LastIndexDate"" SET DEFAULT '-infinity'::timestamp with time zone");
            migrationBuilder.Sql(@"ALTER TABLE ""Documents"" ALTER COLUMN ""LastIndexDate"" SET DEFAULT '-infinity'::timestamp with time zone"); 

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetUsers"" ALTER COLUMN ""LastLogin"" TYPE timestamp without time zone");        
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetUsers"" ALTER COLUMN ""RegistrationDate"" TYPE timestamp without time zone"); 
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetUsers"" ALTER COLUMN ""LastActivity"" TYPE timestamp without time zone"); 
            migrationBuilder.Sql(@"ALTER TABLE ""Groups"" ALTER COLUMN ""CreationDate"" TYPE timestamp without time zone");     
            migrationBuilder.Sql(@"ALTER TABLE ""Groups"" ALTER COLUMN ""ModificationDate"" TYPE timestamp without time zone"); 
            migrationBuilder.Sql(@"ALTER TABLE ""APIKeys"" ALTER COLUMN ""CreationDate"" TYPE timestamp without time zone");     
            migrationBuilder.Sql(@"ALTER TABLE ""APIKeys"" ALTER COLUMN ""ModificationDate"" TYPE timestamp without time zone"); 
            migrationBuilder.Sql(@"ALTER TABLE ""APIKeys"" ALTER COLUMN ""LastUsage"" TYPE timestamp without time zone");        
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetRoles"" ALTER COLUMN ""CreationDate"" TYPE timestamp without time zone");     
            migrationBuilder.Sql(@"ALTER TABLE ""AspNetRoles"" ALTER COLUMN ""ModificationDate"" TYPE timestamp without time zone"); 
            migrationBuilder.Sql(@"ALTER TABLE ""Facets"" ALTER COLUMN ""CreationDate"" TYPE timestamp without time zone");     
            migrationBuilder.Sql(@"ALTER TABLE ""Facets"" ALTER COLUMN ""ModificationDate"" TYPE timestamp without time zone"); 
            migrationBuilder.Sql(@"ALTER TABLE ""Sources"" ALTER COLUMN ""CreationDate"" TYPE timestamp without time zone");     
            migrationBuilder.Sql(@"ALTER TABLE ""Sources"" ALTER COLUMN ""ModificationDate"" TYPE timestamp without time zone"); 
            migrationBuilder.Sql(@"ALTER TABLE ""IncomingFeeds"" ALTER COLUMN ""LastCollection"" TYPE timestamp without time zone");   
            migrationBuilder.Sql(@"ALTER TABLE ""Tags"" ALTER COLUMN ""CreationDate"" TYPE timestamp without time zone");     
            migrationBuilder.Sql(@"ALTER TABLE ""Tags"" ALTER COLUMN ""ModificationDate"" TYPE timestamp without time zone"); 
            migrationBuilder.Sql(@"ALTER TABLE ""Comments"" ALTER COLUMN ""CommentDate"" TYPE timestamp without time zone");      
            migrationBuilder.Sql(@"ALTER TABLE ""Comments"" ALTER COLUMN ""ModificationDate"" TYPE timestamp without time zone"); 
            migrationBuilder.Sql(@"ALTER TABLE ""Documents"" ALTER COLUMN ""DocumentDate"" TYPE timestamp without time zone");     
            migrationBuilder.Sql(@"ALTER TABLE ""Documents"" ALTER COLUMN ""RegistrationDate"" TYPE timestamp without time zone"); 
            migrationBuilder.Sql(@"ALTER TABLE ""Documents"" ALTER COLUMN ""ModificationDate"" TYPE timestamp without time zone"); 
            migrationBuilder.Sql(@"ALTER TABLE ""Files"" ALTER COLUMN ""DocumentDate"" TYPE timestamp without time zone");     
            migrationBuilder.Sql(@"ALTER TABLE ""Files"" ALTER COLUMN ""RegistrationDate"" TYPE timestamp without time zone"); 
            migrationBuilder.Sql(@"ALTER TABLE ""Files"" ALTER COLUMN ""ModificationDate"" TYPE timestamp without time zone"); 
            migrationBuilder.Sql(@"ALTER TABLE ""SubmittedDocuments"" ALTER COLUMN ""SubmissionDate"" TYPE timestamp without time zone");   
            migrationBuilder.Sql(@"ALTER TABLE ""SubmittedDocuments"" ALTER COLUMN ""IngestionDate"" TYPE timestamp without time zone");    
            migrationBuilder.Sql(@"ALTER TABLE ""Tags"" ALTER COLUMN ""LastIndexDate"" TYPE timestamp without time zone");    
            migrationBuilder.Sql(@"ALTER TABLE ""Sources"" ALTER COLUMN ""LastIndexDate"" TYPE timestamp without time zone");    
            migrationBuilder.Sql(@"ALTER TABLE ""Facets"" ALTER COLUMN ""LastIndexDate"" TYPE timestamp without time zone");    
            migrationBuilder.Sql(@"ALTER TABLE ""Documents"" ALTER COLUMN ""LastIndexDate"" TYPE timestamp without time zone");    
            migrationBuilder.Sql(@"ALTER TABLE ""ExportTemplates"" ALTER COLUMN ""Created"" TYPE timestamp without time zone");          
            migrationBuilder.Sql(@"ALTER TABLE ""ExportTemplates"" ALTER COLUMN ""Modified"" TYPE timestamp without time zone");         
            migrationBuilder.Sql(@"ALTER TABLE ""SavedSearches"" ALTER COLUMN ""CreationDate"" TYPE timestamp without time zone");     
            migrationBuilder.Sql(@"ALTER TABLE ""SavedSearches"" ALTER COLUMN ""ModificationDate"" TYPE timestamp without time zone"); 
            migrationBuilder.Sql(@"ALTER TABLE ""UserSavedSearches"" ALTER COLUMN ""LastNotification"" TYPE timestamp without time zone"); 
            
            migrationBuilder.Sql(@"ALTER TABLE ""Sources"" ALTER COLUMN ""LastIndexDate"" SET DEFAULT '-infinity'::timestamp without time zone");
            migrationBuilder.Sql(@"ALTER TABLE ""Tags"" ALTER COLUMN ""LastIndexDate"" SET DEFAULT '-infinity'::timestamp without time zone");
            migrationBuilder.Sql(@"ALTER TABLE ""Facets"" ALTER COLUMN ""LastIndexDate"" SET DEFAULT '-infinity'::timestamp without time zone");
            migrationBuilder.Sql(@"ALTER TABLE ""Documents"" ALTER COLUMN ""LastIndexDate"" SET DEFAULT '-infinity'::timestamp without time zone");
        }
    }
}

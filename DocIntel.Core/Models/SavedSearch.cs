using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DocIntel.Core.Utils.Search.Documents;

namespace DocIntel.Core.Models;

public class SavedSearch
{
    public Guid SavedSearchId { get; set; }
    public string Name { get; set; }
    public bool Public { get; set; }
    
    [Display(Name = "Creation Date")]
    [DataType(DataType.Date)]
    public DateTime CreationDate { get; set; }

    [Display(Name = "Modification Date")]
    [DataType(DataType.Date)]
    public DateTime ModificationDate { get; set; }

    public AppUser CreatedBy { get; set; }
    public string CreatedById { get; set; }

    public AppUser LastModifiedBy { get; set; }
    public string LastModifiedById { get; set; }
    
    public string SearchTerm { get; set; }
    
    [Column(TypeName = "jsonb")] 
    public IList<SearchFilter> Filters { get; set; }
    public SortCriteria SortCriteria { get; set; }
    public int PageSize { get; set; }
}
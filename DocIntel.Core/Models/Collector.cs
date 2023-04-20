using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Nodes;

namespace DocIntel.Core.Models;

public class Collector {
    public Guid CollectorId { get; set; }
    
    public string Name { get; set; }
    public string Description { get; set; }
    [Required]
    public string CronExpression { get; set; }
    
    public bool Enabled { get; set; }
    public bool SkipInbox { get; set; }
    public bool ImportStructuredData { get; set; }

    [DefaultValue(-1)]
    public int Limit { get; set; } = -1;

    public DateTime? LastCollection { get; set; }
    
    [Column(TypeName = "jsonb")]
    public JsonObject Settings { get; set; }

    public string Module { get; set; }
    public string CollectorName { get; set; }

    public string UserId { get; set; }
    public AppUser User { get; set; }

    [Required]
    public Guid SourceId { get; set; }
    public Source Source { get; set; }
    
    public Classification Classification { get; set; }
    public Guid ClassificationId { get; set; }
    public ICollection<Group> ReleasableTo { get; set; }
    public ICollection<Group> EyesOnly { get; set; }

    public ICollection<Tag> Tags { get; set; }
}
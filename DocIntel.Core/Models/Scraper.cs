using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json.Linq;

namespace DocIntel.Core.Models;

public class Scraper {
    public Guid ScraperId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Enabled { get; set; }

    [Column(TypeName = "jsonb")]
    public JObject Settings { get; set; }

    public Guid ReferenceClass { get; set; }

    public bool OverrideSource { get; set; }
    public Guid? SourceId { get; set; }
    public Source Source { get; set; }
    public bool SkipInbox { get; set; }
    public int Position { get; set; }

    public bool OverrideClassification { get; set; }
    public Classification Classification { get; set; }
    public Guid? ClassificationId { get; set; }

    public ICollection<Group> ReleasableTo { get; set; }
    public ICollection<Group> EyesOnly { get; set; }
    public bool OverrideReleasableTo { get; set; }
    public bool OverrideEyesOnly { get; set; }

    public ICollection<Tag> Tags { get; set; }
}
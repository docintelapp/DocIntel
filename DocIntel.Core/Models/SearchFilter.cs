using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocIntel.Core.Models;

public class SearchFilter
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool Negate { get; set; }
    public string Field { get; set; }
    public string Operator { get; set; }
    public IList<SearchFilterValue> Values { get; set; }
}
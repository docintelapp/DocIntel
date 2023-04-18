using System;
using System.Collections.Generic;

namespace DocIntel.Core.Collectors;

public class NodeImport
{
    public string Form { get; set; }
    public dynamic Valu { get; set; }
    public Dictionary<string, Tuple<DateTime?, DateTime?>>? Tags { get; set; }
    public Dictionary<string,dynamic>? Props { get; set; }
}
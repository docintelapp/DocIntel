using System;
using System.Collections.Generic;
using DocIntel.Core.Models;

namespace DocIntel.Core.Collectors;

public interface IDocumentCollector
{
    // TODO Object should be replaced by the type of the object itself, use of reflection?
    IAsyncEnumerable<DocumentImport> Collect(Collector lastCollection, object settings = null);
}
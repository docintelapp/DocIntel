using System;
using System.Collections.Generic;

namespace DocIntel.Core.Collectors;

public interface IDocumentCollector
{
    // TODO Object should be replaced by the type of the object itself, use of reflection?
    IAsyncEnumerable<DocumentImport> Collect(DateTime? since, int? limit = null, object settings = null);
}
using System;
using System.Threading.Tasks;
using DocIntel.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace DocIntel.Core.Modules;

public interface IModuleExporter
{}

public interface IModuleExporter<T1, T2> : IModuleExporter
{
    Task<IActionResult> ExportDocumentsAsync(AmbientContext context, T1 parameters = default);
    Task<ActionResult> ExportDocumentAsync(AmbientContext context, Guid documentId, T2 parameters = default);
}
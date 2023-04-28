/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau
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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DocIntel.Core.Authentication;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Models;
using DocIntel.Core.Modules;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocIntel.WebApp.Areas.API.Controllers;

[Area("API")]
[Route("API/Export")]
[ApiController]
public class ExporterController : DocIntelAPIControllerBase
{
    private readonly ILogger _logger;
    private readonly ModuleFactory _moduleFactory;
    private readonly IModelMetadataProvider _modelMetadataProvider;
    private readonly IModelBinderFactory _modelBinderFactory;
    private readonly IServiceProvider _serviceProvider;
    
    public ExporterController(AppUserManager userManager,
        DocIntelContext context,
        ILogger<ExporterController> logger,
        ModuleFactory moduleFactory,
        IModelMetadataProvider modelMetadataProvider,
        IModelBinderFactory modelBinderFactory, IServiceProvider serviceProvider)
        : base(userManager, context)
    {
        _logger = logger;
        _moduleFactory = moduleFactory;
        _modelMetadataProvider = modelMetadataProvider;
        _modelBinderFactory = modelBinderFactory;
        _serviceProvider = serviceProvider;
    }

    [HttpGet("{module}/{exporter}/Document")]
    [Produces("application/json")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IActionResult> ExportDocument(string module, string exporter)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var instance = _moduleFactory.GetExporter(module, exporter);
            if (instance == null) return NotFound();

            var @interface = instance.GetType().GetInterface("IModuleExporter`2");
            if (@interface != null)
            {
                var genericParameters = @interface.GetGenericArguments();
                var exporterParameters = await BindExporterParameters(genericParameters[0]);

                var methodInfo = instance.GetType().GetMethod("ExportDocumentsAsync");
                if (methodInfo != null)
                {
                    var result = (Task<IActionResult>) methodInfo.Invoke(instance, new object[] { AmbientContext, exporterParameters });
                    if (result != null) return await result;
                    else _logger.LogError($"Could not invoke ExportDocumentsAsync for exporter '{exporter}' in module '{module}'.");
                }
                else
                {
                    _logger.LogError($"Could not find method ExportDocumentsAsync for exporter '{exporter}' in module '{module}'.");
                }
            }
            else
            {
                _logger.LogError($"Could not find interface IModuleExporter<,> for exporter '{exporter}' in module '{module}'.");   
            }

            return NotFound();
        }
        catch (UnauthorizedOperationException)
        {
            return Unauthorized();
        }
    }

    [HttpGet("{module}/{exporter}/Document/{id}")]
    [Produces("application/json")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IActionResult> ExportDocument(string module, string exporter, Guid id)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var instance = _moduleFactory.GetExporter(module, exporter);
            Console.WriteLine("Got the exporter");
            if (instance == null) return NotFound();

            var @interface = instance.GetType().GetInterface("IModuleExporter`2");
            if (@interface != null)
            {
                var genericParameters = @interface.GetGenericArguments();
                var exporterParameters = await BindExporterParameters(genericParameters[1]);
            
                var methodInfo = instance.GetType().GetMethod("ExportDocumentAsync");
                if (methodInfo != null)
                {
                    var result = methodInfo.Invoke(instance, new object[] { AmbientContext, id, exporterParameters });
                    
                    if (result != null) return await (Task<ActionResult>) result;
                    else _logger.LogError($"Could not invoke ExportDocumentAsync for exporter '{exporter}' in module '{module}'.");
                }
                else
                {
                    _logger.LogError($"Could not find method ExportDocumentAsync for exporter '{exporter}' in module '{module}'.");
                }
            }
            else
            {
                _logger.LogError($"Could not find interface IModuleExporter<,> for exporter '{exporter}' in module '{module}'.");   
            }

            return NotFound();
        }
        catch (UnauthorizedOperationException)
        {
            return Unauthorized();
        }
    }
    
    /// <summary>
    /// Create a new instance of the specified type and bind its values to the request. It uses the same model
    /// binding methods as the standard model binding for ASP.NET Core MVC.
    /// </summary>
    /// <param name="t">The type for the parameter</param>
    /// <returns>An instance of <c>t</c></returns>
    private async Task<object> BindExporterParameters(Type t)
    {
        var valueProvider = await CompositeValueProvider.CreateAsync(ControllerContext);
        var modelMetadata = _modelMetadataProvider.GetMetadataForType(t);
        var modelBinderFactoryContext = new ModelBinderFactoryContext()
        {
            Metadata = modelMetadata,
            CacheToken = modelMetadata
        };
        var modelBinder = _modelBinderFactory.CreateBinder(modelBinderFactoryContext);
        var modelBindingContext = DefaultModelBindingContext.CreateBindingContext(ControllerContext,
            valueProvider, modelMetadata, new BindingInfo(), string.Empty);
        await modelBinder.BindModelAsync(modelBindingContext);
        var argument = modelBindingContext.Model;
        return argument;
    }
}
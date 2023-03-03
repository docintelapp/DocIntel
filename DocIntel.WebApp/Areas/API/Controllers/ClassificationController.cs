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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AutoMapper;
using Bogus;
using DocIntel.Core.Authentication;
using DocIntel.Core.Exceptions;
using DocIntel.Core.Logging;
using DocIntel.Core.Models;
using DocIntel.Core.Repositories;
using DocIntel.WebApp.Areas.API.Models;
using DocIntel.WebApp.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace DocIntel.WebApp.Areas.API.Controllers;

/// <summary>
/// Classification of information indicates the level of clearance required to read the enclosed information. In
/// DocIntel, classification are only indicative as all users are expected to have the proper security clearance
/// to handle the highest level of classified information stored in the platform.
///
/// Classifications are hierarchical. For example, *Unclassified* information can be used at "Restricted" level in
/// common government classification schemes; so *Restricted* is modeled as a sub-classification of *Unclassified*.
/// 
/// ## Classification Attributes
///
/// | Attribute              | Description                                                                           |
/// |------------------------|---------------------------------------------------------------------------------------|
/// | ClassificationId       | The identifier of the classification                                                  |
/// | Title                  | The title of the classification (e.g. "Beperkte Verspreiding / Diffusion Restreinte") |
/// | Subtitle               | The tag line, often display below the title (e.g. "K.B/A.R 24.03.2000")               |
/// | Abbreviation           | An abbreviated form of the classification (e.g. "BR")                                 |
/// | Color                  | A color used for the background of the classification banner. Valid colors are `(access|warning|danger|info|primary|secondary)-bg-[50-900]`, for example `info-bg-500`. |
/// | Description            | A description of the classification.                                                  |
/// | ParentClassificationId | The identifier of the classification                                                  |
/// | Default                | Whether the classification is the default classification                              |
/// 
/// ## Classification Relationships
///
/// | Relationship         | Description               |
/// |----------------------|---------------------------|
/// | ParentClassification | The parent classification |
/// 
/// </summary>
[Area("API")]
[Route("API/Classification")]
[ApiController]
public class ClassificationController : DocIntelAPIControllerBase
{
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IClassificationRepository _classificationRepository;

    public ClassificationController(AppUserManager userManager,
        DocIntelContext context,
        ILogger<ClassificationController> logger,
        IClassificationRepository classificationRepository,
        IHttpContextAccessor accessor,
        IMapper mapper)
        : base(userManager, context)
    {
        _logger = logger;
        _classificationRepository = classificationRepository;
        _accessor = accessor;
        _mapper = mapper;
    }

    /// <summary>
    /// Get classifications
    /// </summary>
    /// <remarks>
    /// Returns all classifications. 
    ///
    /// For example, with cURL
    /// 
    ///     curl --request GET \
    ///         --url http://localhost:5001/API/Classification \
    ///         --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <returns>The classifications</returns>
    /// <response code="200">Returns the classification</response>
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<APIClassificationDetails>))]
    [SwaggerOperation(
        OperationId = "GetAll"
    )]
    public async Task<IActionResult> Index()
    {
        var currentUser = await GetCurrentUser();

        try
        {
            return Ok(_mapper.Map<IEnumerable<APIClassificationDetails>>(
                await _classificationRepository.GetAllAsync(AmbientContext).ToListAsync()
                ));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.ListClassificationFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to list classification without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
    }
    
    /// <summary>
    /// Get classification details
    /// </summary>
    /// <remarks>
    /// Returns the details of a classification.
    ///
    /// For example, with cURL
    /// 
    ///     curl --request GET \
    ///         --url http://localhost:5001/API/Classification/04573fca-f1b1-48a4-b55b-b26b8c09bb9d \
    ///         --header 'Authorization: Bearer $TOKEN'
    /// 
    /// </remarks>
    /// <param name="classificationId" example="7dd7bdd3-05c3-cc34-c560-8cc94664f810">The identifier of the classification</param>
    /// <returns>The classification</returns>
    /// <response code="201">Returns the classification</response>
    /// <response code="401">Action is not authorized</response>
    [HttpGet("{classificationId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIClassificationDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "Get"
    )]
    public async Task<IActionResult> Details(Guid classificationId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            var classification = await _classificationRepository.GetAsync(AmbientContext, classificationId);
            return Ok(_mapper.Map<APIClassificationDetails>(classification));
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsClassificationFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of classification '{classificationId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("classification.id", classificationId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DetailsClassificationFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to view details of a non-existing classification '{classificationId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("classification.id", classificationId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }

    /// <summary>
    /// Create a classification
    /// </summary>
    /// <remarks>
    /// Creates a new classification
    ///
    /// For example, with cURL
    /// 
    ///     curl --request POST \
    ///         --url http://localhost:5001/API/Classification \
    ///         --header 'Authorization: Bearer $TOKEN' \
    ///         --header 'Content-Type: application/json' \
    ///         --data '{
    ///           "title": "Company Confidential",
    ///           "subtitle": "Do not share outside company",
    ///           "abbreviation": "C",
    ///           "color": "color-warning-500",
    ///           "description": null,
    ///           "parentClassificationId": null,
    ///           "default": false
    ///         }'
    /// 
    /// </remarks>
    /// <param name="submittedClassification">The classification to create</param>
    /// <returns>The created classification</returns>
    /// <response code="200">Returns the newly created classification</response>
    /// <response code="400">The provided data are invalid (e.g. empty title, non-existing parent classification, etc.)</response>
    /// <response code="401">Action is not authorized</response>
    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(APIClassificationDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [SwaggerOperation(
        OperationId = "Create"
    )]
    public async Task<IActionResult> Create([FromBody] APIClassification submittedClassification)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            if (ModelState.IsValid)
            {
                var classification = _mapper.Map<Classification>(submittedClassification);

                classification = await _classificationRepository.AddAsync(AmbientContext, classification);

                await AmbientContext.DatabaseContext.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.CreateClassificationSuccessful,
                    new LogEvent(
                            $"User '{currentUser.UserName}' successfully created a new classification '{classification.Title}' with id '{classification.ClassificationId}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddClassification(classification),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APIClassificationDetails>(classification));
            }

            return BadRequest(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.CreateClassificationFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to create a new classification '{submittedClassification.Title}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (InvalidArgumentException e)
        {
            ModelState.Clear();
            foreach (var kv in e.Errors)
            foreach (var errorMessage in kv.Value)
                ModelState.AddModelError(kv.Key, errorMessage);

            _logger.Log(LogLevel.Information,
                EventIDs.CreateClassificationFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to create a new classification with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Update a classification
    /// </summary>
    /// <remarks>
    /// Updates the classification specified in the route with the provided body.
    ///
    /// For example, with cURL
    ///
    ///     curl --request PATCH \
    ///       --url http://localhost:5001/API/Classification/f0a8ebb6-dcad-45ac-a0c9-1bc0f15b22c3 \
    ///       --header 'Authorization: Bearer $TOKEN' \
    ///       --header 'Content-Type: application/json' \
    ///       --data '{
    ///       "title": "Company Confidential",
    ///       "subtitle": "Do not share outside company.",
    ///       "abbreviation": "C",
    ///       "color": "color-warning-500",
    ///       "description": null,
    ///       "parentClassificationId": null,
    ///       "default": false
    ///     }'
    ///
    /// </remarks>
    /// <param name="classificationId" example="f740b67b-4c2e-4d78-81e2-399f5449412e">The identifier of the classification to update</param>
    /// <param name="submittedClassification">The updated classification</param>
    /// <returns>The updated classification</returns>
    /// <response code="200">Returns the updated classification</response>
    /// <response code="400">The provided data are invalid (e.g. empty title, non-existing parent classification, etc.)</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The classification does not exists</response>
    [HttpPatch("{classificationId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(APIClassificationDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "Update"
    )]
    public async Task<IActionResult> Edit([FromRoute] Guid classificationId, [FromBody] APIClassification submittedClassification)
    {
        var currentUser = await GetCurrentUser();

        try
        {
            var classification = await _classificationRepository.GetAsync(AmbientContext, classificationId);
            
            if (ModelState.IsValid)
            {
                classification = _mapper.Map(submittedClassification, classification);
                    
                classification = await _classificationRepository.UpdateAsync(AmbientContext, classification);
                await _context.SaveChangesAsync();

                _logger.Log(LogLevel.Information,
                    EventIDs.EditClassificationSuccessful,
                    new LogEvent($"User '{currentUser.UserName}' successfully edit classification '{classification.Title}'.")
                        .AddUser(currentUser)
                        .AddHttpContext(_accessor.HttpContext)
                        .AddClassification(classification),
                    null,
                    LogEvent.Formatter);

                return Ok(_mapper.Map<APIClassificationDetails>(classification));
            }

            throw new InvalidArgumentException(ModelState);
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.EditClassificationFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit classification '{classificationId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("classification.id", classificationId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.EditClassificationFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit a non-existing classification '{classificationId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("classification.id", classificationId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
        catch (InvalidArgumentException e)
        {
            ModelState.Clear();
            foreach (var kv in e.Errors)
            foreach (var errorMessage in kv.Value)
                ModelState.AddModelError(kv.Key, errorMessage);

            _logger.Log(LogLevel.Information,
                EventIDs.EditClassificationFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to edit classification '{classificationId}' with an invalid model.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("classification.id", classificationId),
                null,
                LogEvent.Formatter);

            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Delete a classification
    /// </summary>
    /// <remarks>
    /// Deletes the classification specified in the route.
    ///
    ///     curl --request DELETE \
    ///       --url http://localhost:5001/API/Classification/8cdb94c2-f24e-4e04-bfa7-b6f13bdd7fe9 \
    ///       --header 'Authorization: Bearer $TOKEN' \
    /// 
    /// </remarks>
    /// <param name="classificationId" example="f740b67b-4c2e-4d78-81e2-399f5449412e">The classification identifier</param>
    /// <response code="200">Returns the updated classification</response>
    /// <response code="401">Action is not authorized</response>
    /// <response code="404">The classification does not exists</response>
    [HttpDelete("{classificationId}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type=typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        OperationId = "Delete"
    )]
    public async Task<IActionResult> Delete(Guid classificationId)
    {
        var currentUser = await GetCurrentUser();
        try
        {
            await _classificationRepository.RemoveAsync(AmbientContext, classificationId);
            await _context.SaveChangesAsync();

            _logger.Log(LogLevel.Information,
                EventIDs.DeleteClassificationSuccessful,
                new LogEvent($"User '{currentUser.UserName}' successfully deleted classification '{classificationId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("classification.id", classificationId),
                null,
                LogEvent.Formatter);

            return Ok();
        }
        catch (UnauthorizedOperationException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DeleteClassificationFailed,
                new LogEvent(
                        $"User '{currentUser.UserName}' attempted to delete a new classification '{classificationId}' without legitimate rights.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("classification.id", classificationId),
                null,
                LogEvent.Formatter);

            return Unauthorized();
        }
        catch (NotFoundEntityException)
        {
            _logger.Log(LogLevel.Warning,
                EventIDs.DeleteClassificationFailed,
                new LogEvent($"User '{currentUser.UserName}' attempted to delete a non-existing classification '{classificationId}'.")
                    .AddUser(currentUser)
                    .AddHttpContext(_accessor.HttpContext)
                    .AddProperty("classification.id", classificationId),
                null,
                LogEvent.Formatter);

            return NotFound();
        }
    }
    
    public class APIClassificationExample : IExamplesProvider<APIClassification>
    {
        public APIClassification GetExamples()
        {
            var faker = new Faker("en");

            var data = new APIClassification[]
            {
                new APIClassification()
                {
                    Title = "Unclassified",
                    Subtitle = "",
                    Abbreviation = "U",
                    Color = "info-bg-500",
                    Description = "Information is not classified and can be shared according TLP or need to know.",
                    ParentClassificationId = null,
                    Default = true
                },
                new APIClassification()
                {
                    Title = "Beperkte Verspreiding / Diffusion Restreinte",
                    Subtitle = "K.B./A.R. 24.03.2000",
                    Abbreviation = "BR",
                    Color = "info-bg-500",
                    Description = "Information is restricted.",
                    ParentClassificationId = faker.Random.Guid(),
                    Default = faker.Random.Bool()
                },
                new APIClassification()
                {
                    Title = "Company Confidential",
                    Subtitle = "Don't share outside company",
                    Abbreviation = "C",
                    Color = "danger-bg-500",
                    Description = "Information sharing is restricted and cannot be shared outside the company.",
                    ParentClassificationId = faker.Random.Guid(),
                    Default = faker.Random.Bool()
                }
            };

            return faker.PickRandom(data);
        }
    }

    public class APIClassificationDetailsAbstractExample 
    {
        protected readonly Faker faker;
        protected readonly APIClassificationDetails[] data;

        public APIClassificationDetailsAbstractExample()
        {
            faker = new Faker("en");
            var parentGuid = faker.Random.Guid();
            data = new APIClassificationDetails[]
            {
                new APIClassificationDetails()
                {
                    ClassificationId = faker.Random.Guid(),
                    Title = "Company Restricted",
                    Subtitle = "",
                    Abbreviation = "TR",
                    Color = "warning-bg-500",
                    Description = "Information is restricted and can only be shared with a formal approval.",
                    ParentClassificationId = parentGuid,
                    Default = true,
                    ParentClassification = new APIClassification()
                    {
                        Title = "Unclassified",
                        Subtitle = "",
                        Abbreviation = "U",
                        Color = "info-bg-500",
                        Description = "Information is not classified and can be shared according TLP or need to know.",
                        ParentClassificationId = null,
                        Default = true
                    }
                },
                new APIClassificationDetails()
                {
                    ClassificationId = faker.Random.Guid(),
                    Title = "Beperkte Verspreiding / Diffusion Restreinte",
                    Subtitle = "K.B./A.R. 24.03.2000",
                    Abbreviation = "BR",
                    Color = "info-bg-500",
                    Description = "Information is restricted.",
                    ParentClassificationId = parentGuid,
                    Default = faker.Random.Bool(),
                    ParentClassification = new APIClassification()
                    {
                        Title = "Unclassified",
                        Subtitle = "",
                        Abbreviation = "U",
                        Color = "info-bg-500",
                        Description = "Information is not classified and can be shared according TLP or need to know.",
                        ParentClassificationId = null,
                        Default = true
                    }
                },
                new APIClassificationDetails()
                {
                    ClassificationId = faker.Random.Guid(),
                    Title = "Company Confidential",
                    Subtitle = "Don't share outside company",
                    Abbreviation = "C",
                    Color = "danger-bg-500",
                    Description = "Information sharing is restricted and cannot be shared outside the company.",
                    ParentClassificationId = parentGuid,
                    Default = faker.Random.Bool(),
                    ParentClassification = new APIClassification()
                    {
                        Title = "Unclassified",
                        Subtitle = "",
                        Abbreviation = "U",
                        Color = "info-bg-500",
                        Description = "Information is not classified and can be shared according TLP or need to know.",
                        ParentClassificationId = null,
                        Default = true
                    }
                }
            };
        }
    }

    public class APIClassificationDetailsExample : APIClassificationDetailsAbstractExample, IExamplesProvider<APIClassificationDetails>
    {   
        public APIClassificationDetails GetExamples()
        {   
            return faker.PickRandom(data);
        }
    }
    
    public class APIClassificationDetailsExamples : APIClassificationDetailsAbstractExample, IExamplesProvider<IEnumerable<APIClassificationDetails>>
    {
        public IEnumerable<APIClassificationDetails> GetExamples()
        {
            return faker.PickRandom(data, 3);
        }
    }
}
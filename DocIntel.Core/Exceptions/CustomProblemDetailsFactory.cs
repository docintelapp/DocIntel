using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DocIntel.Core.Exceptions
{
    public class CustomProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null, string title = null,
            string type = null, string detail = null, string instance = null)
        {
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance,
            };

            return problemDetails;
        }

        public override ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext,
            ModelStateDictionary modelStateDictionary, int? statusCode = null, string title = null, string type = null,
            string detail = null, string instance = null)
        {
            statusCode ??= 400;
            type ??= "https://tools.ietf.org/html/rfc7231#section-6.5.1";
            instance ??= httpContext.Request.Path;

            var problemDetails = new CustomValidationProblemDetails(modelStateDictionary)
            {
                Status = statusCode,
                Type = type,
                Instance = instance
            };

            if (title != null)
            {
                // For validation problem details, don't overwrite the default title with null.
                problemDetails.Title = title;
            }

            var traceId = Activity.Current?.Id ?? httpContext?.TraceIdentifier;
            if (traceId != null)
            {
                problemDetails.Extensions["traceId"] = traceId;
            }

            return problemDetails;
        }
    }
}
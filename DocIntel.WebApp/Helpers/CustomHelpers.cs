/* DocIntel
 * Copyright (C) 2018-2023 Belgian Defense, Antoine Cailliau, Kevin Menten
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
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using CsvHelper;
using CsvHelper.Configuration;

using DocIntel.Core.Helpers;
using DocIntel.WebApp.ViewModels.Shared;

using Ganss.Xss;
using Json.Schema;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;

using Pluralize.NET.Core;
using TimeZoneConverter;

namespace DocIntel.WebApp.Helpers
{
    public static class CustomHelpers
    {
        // TODO Check and see if necessary or cannot be inlined
        public static IHtmlContent SanitizeAndMap(this IHtmlHelper htmlHelper,
            string text)
        {
            return new HtmlString(new HtmlSanitizer().Sanitize(text));
        }
        

        public static IEnumerable<SelectListItem> GetM49List<TModel>(this IHtmlHelper<TModel> html, string selected)
        {
            Console.WriteLine(selected);
            
            // Get the global
            var unsdCountryRecords = GeoHelpers.GetUNList().ToList();
            
            var global = unsdCountryRecords.Select(_ => new Tuple<string,string>(_.GlobalName, _.GlobalCode)).Distinct();
            
            // Get the region
            var regions = unsdCountryRecords.Select(_ => new Tuple<string,string>(_.RegionName, _.RegionCode)).Distinct();
            
            // Get the sub-region
            var subRegions = unsdCountryRecords.Select(_ => new Tuple<string,string>(_.SubregionName, _.SubregionCode)).Distinct();
            
            // Get the intermediate regions
            var intermediateRegions = unsdCountryRecords.Select(_ => new Tuple<string,string>(_.IntermediateRegionName, _.IntermediateRegionCode)).Distinct();
            
            // Get the countries or areas
            var countriesOrAreas = unsdCountryRecords.Select(_ => new Tuple<string,string>(_.CountryOrArea, _.M49Code)).Distinct();
            
            return global.Union(regions).Union(subRegions).Union(intermediateRegions).Union(countriesOrAreas)
                .Where(_ => !string.IsNullOrEmpty(_.Item1) & !string.IsNullOrEmpty(_.Item2))
                .Select(_ => new SelectListItem(_.Item1, _.Item2) { Selected = selected == _.Item2 } )
                .Union(new SelectListItem[] { new SelectListItem("Unknown", "") { Selected = string.IsNullOrEmpty(selected) } });
        }

        public static IHtmlContent JsonEditor<TModel>(this IHtmlHelper<TModel> html, string key,
            Dictionary<string, JsonObject> metadata, Type tMetaData)
        {
            var schema = JsonSchemaHelpers.ToJsonEditorSchema(tMetaData);
            return JsonEditor<TModel>(html, tMetaData, schema, key, metadata);
        }

        public static IHtmlContent JsonEditor<TModel>(this IHtmlHelper<TModel> html,
            Type tMetaData,
            string schema,
            string key,
            Dictionary<string, JsonObject> metadata)
        {
            var b64Schema = Convert.ToBase64String(Encoding.UTF8.GetBytes(schema));
            
            string b64Value;
            if (metadata != null && metadata.ContainsKey(key))
            {
                JsonSerializerOptions options = new JsonSerializerOptions()
                {
                    Converters = { new JsonStringEnumConverter() }
                };
                
                var metaData = metadata[key].Deserialize(tMetaData, options);
                var serializedMetaData = JsonSerializer.Serialize(metaData, options);
                b64Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(serializedMetaData));
            }
            else
                b64Value = "";

            var htmlString = $@"<input name=""MetaData[{key}]"" id=""metadata_{key}"" type=""hidden""/>
                <div class=""editorJSON"" 
                     data-inputid=""metadata_{key}""
                     data-schema=""{b64Schema}""
                     data-startval=""{b64Value}""></div>";
            
            return new HtmlString(htmlString);
        }

        public static IHtmlContent HelpTextFor<TModel, TValue>(this IHtmlHelper<TModel> html,
            Expression<Func<TModel, TValue>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;

            if (memberExpression == null)
                throw new InvalidOperationException("Expression must be a valid member expression");

            var attributes = memberExpression.Member.GetCustomAttributes(typeof(HelpTextAttribute), true);

            if (attributes.Length == 0) return HtmlString.Empty;

            var firstAttribute = attributes[0] as HelpTextAttribute;
            if ((firstAttribute == null) | string.IsNullOrEmpty(firstAttribute?.Description))
                return HtmlString.Empty;
            return new HtmlString("<p class=\"text-muted mb-2\">" +
                                  html.Encode(firstAttribute?.Description ?? string.Empty) + "</p>");
        }
        
        public static IHtmlContent DisplayAttribute<TModel>(this IHtmlHelper<TModel> html, object o)
        {
            var attributes = o.GetType().GetCustomAttributes(typeof(DisplayAttribute), true);

            if (attributes.Length == 0) return HtmlString.Empty;

            var firstAttribute = attributes[0] as DisplayAttribute;
            if ((firstAttribute == null) | string.IsNullOrEmpty(firstAttribute?.Description))
                return HtmlString.Empty;
            return new HtmlString(html.Encode(firstAttribute?.Description ?? string.Empty));
        }

        public static IHtmlContent Timeago<TModel>(this IHtmlHelper<TModel> html, DateTime t)
        {
            var destinationTimeZone = TimeZoneInfo.Utc;
            var windowsOrIanaTimeZoneId = html.ViewBag.CurrentUser.Preferences?.UI?.TimeZone;
            if (windowsOrIanaTimeZoneId != null)
                destinationTimeZone = TZConvert.GetTimeZoneInfo(windowsOrIanaTimeZoneId) ?? TimeZoneInfo.Utc;
            return new HtmlString("<time datetime=\"" +
                                  TimeZoneInfo.ConvertTimeFromUtc(t, destinationTimeZone).ToString("o") +
                                  "\" class=\"mr-1 timeago\">"
                                  + TimeZoneInfo.ConvertTimeFromUtc(t, destinationTimeZone).ToString("o")
                                  + "</time>");
        }

        public static IHtmlContent Timeago<TModel>(this IHtmlHelper<TModel> html, TimeSpan t)
        {
            var format = "G0";
            return new HtmlString(t.TotalMilliseconds < 1000
                ? t.TotalMilliseconds.ToString(format) + " milliseconds"
                : t.TotalSeconds < 60
                    ? t.TotalSeconds.ToString(format) + " seconds"
                    : t.TotalMinutes < 60
                        ? t.TotalMinutes.ToString(format) + " minutes"
                        : t.TotalHours < 24
                            ? t.TotalHours + " hours"
                            : t.TotalDays.ToString(format) + " days");
        }

        public static IHtmlContent Pluralize(this IHtmlHelper htmlHelper,
            string source, long count)
        {
            if (count > 1)
            {
                var plural = new Pluralizer().Pluralize(source);
                return new HtmlString(plural);
            }

            return new HtmlString(source);
        }

        // Thanks to https://stackoverflow.com/questions/42022311/asp-net-mvc-create-action-link-preserve-query-string
        public static string Page(this IUrlHelper url, long page, string parameter = "page")
        {
            //Reuse existing route values
            var resultRouteValues = new RouteValueDictionary(url.ActionContext.RouteData.Values);

            //Add existing values from query string
            foreach (var queryValue in url.ActionContext.HttpContext.Request.Query)
            {
                if (resultRouteValues.ContainsKey(queryValue.Key))
                    continue;

                resultRouteValues.Add(queryValue.Key, queryValue.Value);
            }

            //Set or add values for PagedList input model
            resultRouteValues[parameter] = page;

            return url.RouteUrl(resultRouteValues);
        }

        // Thanks to https://stackoverflow.com/questions/42022311/asp-net-mvc-create-action-link-preserve-query-string
        public static string DidYouMean(this IUrlHelper url, string newQuery)
        {
            //Reuse existing route values
            var resultRouteValues = new RouteValueDictionary(url.ActionContext.RouteData.Values);

            //Add existing values from query string
            foreach (var queryValue in url.ActionContext.HttpContext.Request.Query)
            {
                if (resultRouteValues.ContainsKey(queryValue.Key))
                    continue;

                resultRouteValues.Add(queryValue.Key, queryValue.Value);
            }

            //Set or add values for PagedList input model
            resultRouteValues["SearchTerm"] = newQuery;
            // resultRouteValues["pageSize"] = pageSize;

            return url.RouteUrl(resultRouteValues);
        }
    }

    public class PaginationTagHelper : TagHelper
    {
        private readonly IHtmlHelper _html;

        public PaginationTagHelper(IHtmlHelper htmlHelper)
        {
            _html = htmlHelper;
        }

        [HtmlAttributeName("page")] public long Page { get; set; }

        [HtmlAttributeName("page-count")] public long PageCount { get; set; }

        [HtmlAttributeName("method")] public string Method { get; set; }

        [HtmlAttributeName("parameter")] public string Parameter { get; set; } = "page";

        [HtmlAttributeNotBound] [ViewContext] public ViewContext ViewContext { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            //Contextualize the html helper
            (_html as IViewContextAware)?.Contextualize(ViewContext);

            IHtmlContent content;
            if (string.IsNullOrEmpty(Method) || Method.ToUpperInvariant() == "GET")
                content = await _html.PartialAsync("~/Views/Shared/_Pagination.cshtml", new PaginationViewModel
                {
                    Page = Page,
                    PageCount = PageCount,
                    Parameter = Parameter
                });
            else
                content = await _html.PartialAsync("~/Views/Shared/_PaginationPost.cshtml", new PaginationViewModel
                {
                    Page = Page,
                    PageCount = PageCount,
                    Parameter = Parameter
                });

            output.Content.SetHtmlContent(content);
        }
    }
}
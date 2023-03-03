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

using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable InconsistentNaming

namespace DocIntel.WebApp.ViewModels
{
    /// <summary>Provides easy-access to building the SmartAdmin Navigation using JSON text data.</summary>
    /// <remarks>These classes are solely created for Demo purposes, please do not use them in Production.</remarks>
    internal static class NavigationBuilder
    {
        private static JsonSerializerSettings DefaultSettings => SerializerSettings();

        private static JsonSerializerSettings SerializerSettings(bool indented = true)
        {
            return new()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Formatting = indented ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter> {new StringEnumConverter()}
            };
        }

        public static SmartNavigation FromJson(string json)
        {
            return JsonConvert.DeserializeObject<SmartNavigation>(json, DefaultSettings);
        }
    }

    public sealed class SmartNavigation
    {
        public SmartNavigation()
        {
        }

        public SmartNavigation(IEnumerable<ListItem> items)
        {
            Lists = new List<ListItem>(items);
        }

        public string Version { get; set; }
        public List<ListItem> Lists { get; set; } = new();
    }

    public class ListItem
    {
        public string Id { get; set; }
        public string Icon { get; set; }
        public bool ShowOnSeed { get; set; } = true;
        public string Title { get; set; }
        public string Text { get; set; }
        public string Href { get; set; }
        public string Area { get; set; } = "";
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Route { get; set; }
        public string Tags { get; set; }
        public string I18n { get; set; }
        public bool Disabled { get; set; }
        public List<ListItem> Items { set; get; } = new();
        public Span Span { get; set; } = new();
        public List<string> Permissions { get; set; }
    }

    public sealed class Span
    {
        public string Position { get; set; }
        public string Class { get; set; }
        public string Text { get; set; }

        public bool HasValue()
        {
            return (Position?.Length ?? 0) + (Class?.Length ?? 0) + (Text?.Length ?? 0) > 0;
        }
    }
}
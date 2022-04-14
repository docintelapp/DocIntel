/* DocIntel
 * Copyright (C) 2018-2021 Belgian Defense, Antoine Cailliau
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

using System.ComponentModel;

using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace DocIntel.Core.Authorization.Operations
{
    public static class ScraperOperations
    {
        public static OperationAuthorizationRequirement Create =
            new() {Name = ScraperOperationsConstants.CreateScraperOperationName};

        public static OperationAuthorizationRequirement List =
            new() {Name = ScraperOperationsConstants.ListScraperOperationName};

        public static OperationAuthorizationRequirement View =
            new() {Name = ScraperOperationsConstants.ViewScraperOperationName};

        public static OperationAuthorizationRequirement Edit =
            new() {Name = ScraperOperationsConstants.EditScraperOperationName};

        public static OperationAuthorizationRequirement Delete =
            new() {Name = ScraperOperationsConstants.DeleteScraperOperationName};
    }

    [DisplayName("Incoming feeds")]
    public class ScraperOperationsConstants : IOperationConstants
    {
        [DisplayName("Create scrapers")] public static string CreateScraperOperationName => "CreateScraper";
        [DisplayName("List scrapers")] public static string ListScraperOperationName => "ListScrapers";
        [DisplayName("View scrapers")] public static string ViewScraperOperationName => "ViewScraper";
        [DisplayName("Edit scrapers")] public static string EditScraperOperationName => "EditScraper";
        [DisplayName("Delete scrapers")] public static string DeleteScraperOperationName => "DeleteScraper";
    }
}
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
    public static class SavedSearchOperations
    {
        public static readonly OperationAuthorizationRequirement View =
            new() {Name = SavedSearchOperationsConstants.ViewRuleOperationName};

        public static readonly OperationAuthorizationRequirement Update =
            new() {Name = SavedSearchOperationsConstants.EditRuleOperationName};

        public static readonly OperationAuthorizationRequirement Add =
            new() {Name = SavedSearchOperationsConstants.CreateRuleOperationName};

        public static readonly OperationAuthorizationRequirement Delete =
            new() {Name = SavedSearchOperationsConstants.DeleteRuleOperationName};
    }

    [DisplayName("Saved Searches")]
    public class SavedSearchOperationsConstants : IOperationConstants
    {
        [DisplayName("View Saved Searches")] public static string ViewRuleOperationName => "ViewSavedSearch";
        [DisplayName("Edit Saved Searches")] public static string EditRuleOperationName => "EditSavedSearch";
        [DisplayName("Create Saved Searches")] public static string CreateRuleOperationName => "CreateSavedSearch";
        [DisplayName("Delete Saved Searches")] public static string DeleteRuleOperationName => "DeleteSavedSearch";
    }
}
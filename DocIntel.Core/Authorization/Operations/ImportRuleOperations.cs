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

using System.ComponentModel;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace DocIntel.Core.Authorization.Operations
{
    public static class ImportRuleOperations
    {
        public static OperationAuthorizationRequirement List =
            new() {Name = ImportRuleOperationsConstants.ListRuleOperationName};

        public static OperationAuthorizationRequirement View =
            new() {Name = ImportRuleOperationsConstants.ViewRuleOperationName};

        public static OperationAuthorizationRequirement Edit =
            new() {Name = ImportRuleOperationsConstants.EditRuleOperationName};

        public static OperationAuthorizationRequirement Create =
            new() {Name = ImportRuleOperationsConstants.CreateRuleOperationName};

        public static OperationAuthorizationRequirement Delete =
            new() {Name = ImportRuleOperationsConstants.DeleteRuleOperationName};
    }

    [DisplayName("Import Rules")]
    public class ImportRuleOperationsConstants : IOperationConstants
    {
        [DisplayName("List import rules")] public static string ListRuleOperationName => "ListImportRule";
        [DisplayName("View import rules")] public static string ViewRuleOperationName => "ViewImportRule";
        [DisplayName("Edit import rules")] public static string EditRuleOperationName => "EditImportRule";
        [DisplayName("Create import rules")] public static string CreateRuleOperationName => "CreateImportRule";
        [DisplayName("Delete import rules")] public static string DeleteRuleOperationName => "DeleteImportRule";
    }
}
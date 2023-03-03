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
    public static class ClassificationOperations
    {
        public static readonly OperationAuthorizationRequirement View =
            new() {Name = ClassificationOperationsConstants.ViewRuleOperationName};

        public static readonly OperationAuthorizationRequirement Update =
            new() {Name = ClassificationOperationsConstants.EditRuleOperationName};

        public static readonly OperationAuthorizationRequirement Add =
            new() {Name = ClassificationOperationsConstants.CreateRuleOperationName};

        public static readonly OperationAuthorizationRequirement Delete =
            new() {Name = ClassificationOperationsConstants.DeleteRuleOperationName};
    }

    [DisplayName("Classifications")]
    public class ClassificationOperationsConstants : IOperationConstants
    {
        [DisplayName("View classifications")] public static string ViewRuleOperationName => "ViewClassification";
        [DisplayName("Edit classifications")] public static string EditRuleOperationName => "EditClassification";
        [DisplayName("Create classifications")] public static string CreateRuleOperationName => "CreateClassification";
        [DisplayName("Delete classifications")] public static string DeleteRuleOperationName => "DeleteClassification";
    }
}
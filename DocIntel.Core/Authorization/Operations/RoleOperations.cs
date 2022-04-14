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
    public static class RoleOperations
    {
        public static OperationAuthorizationRequirement List =
            new() {Name = RoleOperationsConstants.ListOperationName};

        public static OperationAuthorizationRequirement Create =
            new() {Name = RoleOperationsConstants.CreateOperationName};

        public static OperationAuthorizationRequirement Details =
            new() {Name = RoleOperationsConstants.DetailsOperationName};

        public static OperationAuthorizationRequirement Update =
            new() {Name = RoleOperationsConstants.UpdateOperationName};

        public static OperationAuthorizationRequirement Delete =
            new() {Name = RoleOperationsConstants.DeleteOperationName};

        public static OperationAuthorizationRequirement AddRole =
            new() {Name = RoleOperationsConstants.AddRoleOperationName};

        public static OperationAuthorizationRequirement RemoveRole =
            new() {Name = RoleOperationsConstants.RemoveRoleOperationName};

        public static OperationAuthorizationRequirement ViewRole =
            new() {Name = RoleOperationsConstants.ViewRoleOperationName};

        public static OperationAuthorizationRequirement EditPermissions =
            new() {Name = RoleOperationsConstants.EditPermissionsOperationName};
    }

    [DisplayName("Role")]
    public class RoleOperationsConstants : IOperationConstants
    {
        [DisplayName("List user roles")] public static string ListOperationName => "ListRole";
        [DisplayName("Create roles")] public static string CreateOperationName => "CreateRole";
        [DisplayName("View roles")] public static string DetailsOperationName => "ReadRole";
        [DisplayName("Edit roles")] public static string UpdateOperationName => "UpdateRole";
        [DisplayName("Delete roles")] public static string DeleteOperationName => "DeleteRole";
        [DisplayName("Add users to roles")] public static string AddRoleOperationName => "AddUserToRole";
        [DisplayName("Remove users from roles")] public static string RemoveRoleOperationName => "RemoveUserFromRole";
        [DisplayName("List users in roles")] public static string ViewRoleOperationName => "ViewUserFromRole";
        [DisplayName("Edit permissions")] public static string EditPermissionsOperationName => "EditPermissions";
    }
}